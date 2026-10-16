using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Forms;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Forms
{
	public class SetCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IFormRepository formRepository,
		ISolutionRepository solutionRepository,
		IFormXmlValidator formXmlValidator,
		IOutput output) : ICommandExecutor<SetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SetCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FileName))
				return CommandResult.Fail($"The input file <{command.FileName}> does not exist.");

			if (!string.IsNullOrWhiteSpace(command.BackupFile))
			{
				if (!command.BackupFile.TryValidateOutputPath(out var pathError))
					return CommandResult.Fail(pathError!);

				var inputPath = Path.GetFullPath(command.FileName);
				var backupPath = Path.GetFullPath(command.BackupFile);
				var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				if (inputPath.Equals(backupPath, pathComparison))
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
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
			{
				return CommandResult.Fail($"Unable to read form XML from <{command.FileName}>: {ex.Message}", ex);
			}

			output.Write("Validating form XML...");
			if (!formXmlValidator.TryValidate(replacementXml, out var validationErrors))
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Form XML validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, validationErrors));
			}
			output.WriteLine("Done", ConsoleColor.Green);
			replacementXml = replacement.ToString(SaveOptions.DisableFormatting);

			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var (result, form) = await formRepository.GetByNameAsync(crm, command.TableName, command.FormName);
			if (!result.IsSuccess)
				return result;

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
					output.Write("Updating form XML...");
					await crm.UpdateAsync(update, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);

					if (command.Publish)
					{
						var request = new PublishXmlRequest
						{
							ParameterXml = new XElement("importexportxml",
								new XElement("entities", new XElement("entity", command.TableName))).ToString(SaveOptions.DisableFormatting)
						};
						output.Write($"Publishing table {command.TableName}...");
						await crm.ExecuteAsync(request, cancellationToken);
						output.WriteLine("Done", ConsoleColor.Green);
					}
				}
				else
				{
					var (solutionResult, solution) = await solutionRepository.CreateHoldingSolutionAsync(
						crm, command.SolutionName);
					if (!solutionResult.IsSuccess || solution == null)
						return solutionResult;

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
