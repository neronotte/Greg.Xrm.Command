using System.Xml;
using System.Xml.Linq;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Forms
{
	public class LayoutCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IFormRepository formRepository,
		IOutput output) : ICommandExecutor<LayoutCommand>
	{
		public async Task<CommandResult> ExecuteAsync(LayoutCommand command, CancellationToken cancellationToken)
		{
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			var forms = await formRepository.GetMainFormByTableNameAsync(crm, command.TableName);
			if (!FormCommandHelpers.TryGetForm(output, command.TableName, command.FormName, forms, out var form, out var result))
				return result ?? CommandResult.Fail("Error retrieving the form");

			if (string.IsNullOrWhiteSpace(form?.formxml))
				return CommandResult.Fail("No formxml found!");

			try
			{
				var xml = XDocument.Parse(form.formxml, LoadOptions.None);
				if (xml.Root?.Name.LocalName != "form")
					return CommandResult.Fail("The formxml does not contain a form element.");

				foreach (var line in FormLayoutRenderer.Render(xml.Root, command.Display, form.name))
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
