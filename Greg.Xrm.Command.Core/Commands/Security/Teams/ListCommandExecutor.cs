using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	public class ListCommandExecutor(IOutput output, IOrganizationServiceRepository connections, ITeamRepository teams) : ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);
				var matches = await teams.SearchAsync(crm, command.Type, command.Name, cancellationToken);
				output.WriteLine($"Found {matches.Count} teams.");
				if (matches.Count > 0) output.WriteTable(matches, () => ["Id", "Name", "Type", "Business Unit"], team =>
					[team.Id.ToString(), team.name, team.TypeName, team.BusinessUnitName]);
				var result = CommandResult.Success();
				result["Count"] = matches.Count;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
			catch (Exception exception) { return CommandResult.Fail(exception.Message, exception); }
		}
	}
}