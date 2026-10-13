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
			if (!string.IsNullOrWhiteSpace(command.OutputFile) && !command.OutputFile.TryValidateOutputPath(out var error))
				return CommandResult.Fail(error!);

			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var (result, form) = await formRepository.GetByNameAsync(crm, command.TableName, command.FormName, announce: false);
			if (!result.IsSuccess)
				return result;

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
