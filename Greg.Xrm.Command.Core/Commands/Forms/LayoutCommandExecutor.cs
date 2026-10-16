using System.Xml;
using System.Xml.Linq;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Forms;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Forms
{
	public class LayoutCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IFormRepository formRepository,
		IFormLayoutRenderer formLayoutRenderer,
		IOutput output) : ICommandExecutor<LayoutCommand>
	{
		public async Task<CommandResult> ExecuteAsync(LayoutCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var (result, form) = await formRepository.GetByNameAsync(crm, command.TableName, command.FormName);
			if (!result.IsSuccess)
				return result;

			if (string.IsNullOrWhiteSpace(form?.formxml))
				return CommandResult.Fail("No formxml found!");

			try
			{
				var xml = XDocument.Parse(form.formxml, LoadOptions.None);
				if (xml.Root?.Name.LocalName != "form")
					return CommandResult.Fail("The formxml does not contain a form element.");

				foreach (var line in formLayoutRenderer.Render(xml.Root, command.Display, form.name))
					output.WriteLine(line);
				return CommandResult.Success();
			}
			catch (XmlException ex)
			{
				return CommandResult.Fail($"Invalid formxml: {ex.Message}");
			}
		}
	}
}
