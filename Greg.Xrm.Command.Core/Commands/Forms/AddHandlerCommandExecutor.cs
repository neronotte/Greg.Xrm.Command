using System.Xml.Linq;
using System.Xml.XPath;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Forms;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Forms
{
	public class AddHandlerCommandExecutor
	(
			IOrganizationServiceRepository organizationServiceRepository,
			IOutput output,
			IFormRepository formRepository,
			ISolutionRepository solutionRepository,
			IFormWrapperFactory formWrapperFactory) : ICommandExecutor<AddHandlerCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AddHandlerCommand command, CancellationToken cancellationToken)
		{
			if (!string.IsNullOrWhiteSpace(command.TempDir) && !Directory.Exists(command.TempDir))
			{
				return CommandResult.Fail($"The --output directory <{command.TempDir}> does not exist. No changes have been applied.");
			}

			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			if (!await CheckWebResourceExistsAsync(crm, command.Library))
			{
				return CommandResult.Fail($"Webresource <{command.Library}> not found in the current environment, or it is not a javascript webresource. The name must match exactly, check it via the maker portal.");
			}

			output.Write($"Retrieving main form of table {command.TableName}...");
			var formList = await formRepository.GetMainFormByTableNameAsync(crm, command.TableName);
			output.WriteLine("Done", ConsoleColor.Green);

			if (!FormCommandHelpers.TryGetForm(output, command.TableName, command.FormName, formList, out var form, out var result))
			{
				return result ?? CommandResult.Fail("Error retrieving the form to update");
			}

			if (form == null || string.IsNullOrWhiteSpace(form.formxml))
			{
				return CommandResult.Fail("No formxml found!");
			}

			var eventName = GetEventName(command.Event);
			var field = command.Event == FormEvent.OnChange ? command.Field : null;

			if (command.Fast)
			{
				return await ExecuteFastAsync(crm, command, form, eventName, field, cancellationToken);
			}

			var (success, result1, solution) = await FormCommandHelpers.CreateHoldingSolutionAsync(organizationServiceRepository, solutionRepository, output, crm, command.SolutionName);
			if (!success) return result1 ?? CommandResult.Fail("Error creating the holding solution");
			if (solution == null) return CommandResult.Fail("Error creating the holding solution");

			using (solution)
			{
				try
				{
					await solution.AddComponentAsync(form.Id, ComponentType.SystemForm);

					using var solutionContent = await solution.DownloadAsync();

					if (!string.IsNullOrWhiteSpace(command.TempDir))
					{
						var fileName = Path.Combine(command.TempDir, $"{solution}_original.zip");
						await solutionContent.SaveToAsync(fileName);
						output.WriteLine($"Original form backup saved to {fileName}", ConsoleColor.DarkGray);
					}

					var hasChanges = solutionContent.UpdateEntryXml("customizations.xml", doc =>
					{
						var element = doc.XPathSelectElement("./ImportExportXml/Entities/Entity/FormXml/forms/systemform/form");
						if (element == null)
						{
							throw new InvalidOperationException("The form element was not found in the customizations.xml of the temporary solution.");
						}

						output.Write($"Registering {command.Function} on the {eventName} event...");

						var wrapper = formWrapperFactory.CreateWrapper(element);
						var changed = wrapper.EnsureLibrary(command.Library);
						changed = wrapper.EnsureHandler(eventName, field, command.Library, command.Function, command.PassExecutionContext) || changed;

						output.WriteLine("Done", ConsoleColor.Green);
						return changed;
					});

					if (hasChanges)
					{
						var newZipBytes = solutionContent.ToArray();
						await solution.UploadAndPublishAsync(newZipBytes, command.TableName);
					}
					else
					{
						output.WriteLine("The handler is already registered on the form. No changes applied.", ConsoleColor.DarkGray);
					}
				}
				catch (Exception ex)
				{
					output.WriteLine($"ERROR: {ex.Message}", ConsoleColor.Red);
					return CommandResult.Fail(ex.Message, ex);
				}
			}

			return CommandResult.Success();
		}

		private async Task<CommandResult> ExecuteFastAsync(IOrganizationServiceAsync2 crm, AddHandlerCommand command, Form form, string eventName, string? field, CancellationToken cancellationToken)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(command.TempDir))
				{
					var fileName = Path.Combine(command.TempDir, $"{command.TableName}_{form.name.OnlyLowercaseLettersNumbersOrUnderscore()}_original_formxml.xml");
					await File.WriteAllTextAsync(fileName, form.formxml, cancellationToken);
					output.WriteLine($"Original formxml backup saved to {fileName}", ConsoleColor.DarkGray);
				}

				var formElement = XElement.Parse(form.formxml);

				output.Write($"Registering {command.Function} on the {eventName} event...");
				var wrapper = formWrapperFactory.CreateWrapper(formElement);
				var changed = wrapper.EnsureLibrary(command.Library);
				changed = wrapper.EnsureHandler(eventName, field, command.Library, command.Function, command.PassExecutionContext) || changed;
				formElement = wrapper.ToXElement();
				output.WriteLine("Done", ConsoleColor.Green);

				if (!changed)
				{
					output.WriteLine("The handler is already registered on the form. No changes applied.", ConsoleColor.DarkGray);
					return CommandResult.Success();
				}

				output.Write("Updating the form...");
				var update = new Entity("systemform", form.Id);
				update["formxml"] = formElement.ToString(SaveOptions.DisableFormatting);
				await crm.UpdateAsync(update, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write("Publishing customizations...");
				var publishRequest = new PublishXmlRequest
				{
					ParameterXml = $"<importexportxml><entities><entity>{command.TableName}</entity></entities></importexportxml>"
				};
				await crm.ExecuteAsync(publishRequest, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine($"ERROR: {ex.Message}", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static string GetEventName(FormEvent formEvent) => formEvent switch
		{
			FormEvent.OnSave => "onsave",
			FormEvent.OnChange => "onchange",
			_ => "onload"
		};

		private async Task<bool> CheckWebResourceExistsAsync(IOrganizationServiceAsync2 crm, string libraryName)
		{
			output.Write($"Checking webresource <{libraryName}>...");

			var query = new QueryExpression("webresource")
			{
				NoLock = true,
				TopCount = 1
			};
			query.ColumnSet.AddColumns("name");
			query.Criteria.AddCondition("name", ConditionOperator.Equal, libraryName);
			query.Criteria.AddCondition("webresourcetype", ConditionOperator.Equal, (int)WebResourceType.Script);

			var response = await crm.RetrieveMultipleAsync(query);
			if (response.Entities.Count == 0)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return false;
			}

			output.WriteLine("Done", ConsoleColor.Green);
			return true;
		}

	}
}
