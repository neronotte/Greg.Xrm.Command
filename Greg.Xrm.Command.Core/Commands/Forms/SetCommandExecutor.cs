using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	public class SetCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IFormRepository formRepository,
		ISolutionRepository solutionRepository,
		IOutput output) : ICommandExecutor<SetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SetCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FileName))
				return CommandResult.Fail($"The input file <{command.FileName}> does not exist.");

			if (!string.IsNullOrWhiteSpace(command.BackupFile))
			{
				if (!FormXmlFileHelper.TryValidateOutputPath(command.BackupFile, out var pathError))
					return CommandResult.Fail(pathError!);

				if (Path.GetFullPath(command.FileName).Equals(Path.GetFullPath(command.BackupFile), StringComparison.OrdinalIgnoreCase))
					return CommandResult.Fail("The --backup file must differ from the --file input.");
			}

			string replacementXml;
			XElement replacement;
			try
			{
				replacementXml = await File.ReadAllTextAsync(command.FileName, cancellationToken);
				var document = XDocument.Parse(replacementXml, LoadOptions.None);
				replacement = document.Root ?? throw new XmlException("The XML document is empty.");
				if (replacement.Name != "form")
					return CommandResult.Fail("The input XML must contain one <form> root element.");
				// Solution exports of forms layered over a managed base carry solutionaction diff markers instead of the complete form.
				if (replacement.DescendantsAndSelf().Any(e => e.Attribute("solutionaction") != null))
					return CommandResult.Fail("The input XML is a solution diff (it contains solutionaction attributes), not a complete form. Use the output of 'pacx forms get' instead.");
				replacementXml = replacement.ToString(SaveOptions.DisableFormatting);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
			{
				return CommandResult.Fail($"Unable to read form XML from <{command.FileName}>: {ex.Message}", ex);
			}

			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			var forms = await formRepository.GetMainFormByTableNameAsync(crm, command.TableName);
			if (!FormCommandHelpers.TryGetForm(output, command.TableName, command.FormName, forms, out var form, out var result))
				return result ?? CommandResult.Fail("Error retrieving the form");

			if (string.IsNullOrWhiteSpace(form?.formxml))
				return CommandResult.Fail("No formxml found!");

			if (!string.IsNullOrWhiteSpace(command.BackupFile))
			{
				try
				{
					await File.WriteAllTextAsync(command.BackupFile, form.formxml, cancellationToken);
					output.WriteLine($"Original form XML saved to {command.BackupFile}");
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
				{
					return CommandResult.Fail($"Unable to write backup <{command.BackupFile}>: {ex.Message}", ex);
				}
			}

			if (XNode.DeepEquals(XElement.Parse(form.formxml), replacement))
			{
				output.WriteLine("The form XML is unchanged. No update applied.");
				return CommandResult.Success();
			}

			try
			{
				if (command.Fast)
				{
					output.WriteLine("WARNING: --fast updates formxml directly. formjson may remain out of sync. Before publishing in production, apply the final XML without --fast.", ConsoleColor.Yellow);
					var update = new Entity("systemform", form.Id);
					update["formxml"] = replacementXml;
					await crm.UpdateAsync(update, cancellationToken);

					if (command.Publish)
					{
						var request = new PublishXmlRequest
						{
							ParameterXml = new XElement("importexportxml",
								new XElement("entities", new XElement("entity", command.TableName))).ToString(SaveOptions.DisableFormatting)
						};
						await crm.ExecuteAsync(request, cancellationToken);
					}
				}
				else
				{
					var (success, failure, solution) = await FormCommandHelpers.CreateHoldingSolutionAsync(
						organizationServiceRepository, solutionRepository, output, crm, command.SolutionName);
					if (!success || solution == null)
						return failure ?? CommandResult.Fail("Error creating the holding solution");

					using (solution)
					{
						await solution.AddComponentAsync(form.Id, ComponentType.SystemForm);
						using var content = await solution.DownloadAsync();
						content.UpdateEntryXml("customizations.xml", document =>
						{
							var targets = document.XPathSelectElements("./ImportExportXml/Entities/Entity/FormXml/forms/systemform/form").ToArray();
							if (targets.Length != 1)
								throw new InvalidOperationException($"Expected one form in the temporary solution, found {targets.Length}.");
							targets[0].ReplaceWith(new XElement(replacement));
							return true;
						});
						if (command.Publish)
							await solution.UploadAndPublishAsync(content.ToArray(), command.TableName);
						else
							await solution.UploadAsync(content.ToArray());
					}
				}

				output.WriteLine(command.Publish ? "Form XML updated and table published." : "Form XML updated; table not published.", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Unable to update the form: {ex.Message}", ex);
			}
		}
	}
}
