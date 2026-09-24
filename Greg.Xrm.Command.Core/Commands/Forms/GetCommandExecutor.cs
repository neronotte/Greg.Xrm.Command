using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Forms
{
	public class GetCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IFormRepository formRepository,
		IOutput output) : ICommandExecutor<GetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetCommand command, CancellationToken cancellationToken)
		{
			if (!string.IsNullOrWhiteSpace(command.OutputFile) && !FormXmlFileHelper.TryValidateOutputPath(command.OutputFile, out var error))
				return CommandResult.Fail(error!);

			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			var forms = await formRepository.GetMainFormByTableNameAsync(crm, command.TableName);
			if (!FormCommandHelpers.TryGetForm(output, command.TableName, command.FormName, forms, out var form, out var result, announce: false))
				return result ?? CommandResult.Fail("Error retrieving the form");

			if (string.IsNullOrWhiteSpace(form?.formxml))
				return CommandResult.Fail("No formxml found!");

			if (string.IsNullOrWhiteSpace(command.OutputFile))
			{
				output.WriteLine(form.formxml);
				return CommandResult.Success();
			}

			try
			{
				await File.WriteAllTextAsync(command.OutputFile, form.formxml, cancellationToken);
				output.WriteLine($"Form XML written to {command.OutputFile}", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				return CommandResult.Fail($"Unable to write <{command.OutputFile}>: {ex.Message}", ex);
			}
		}
	}
}
