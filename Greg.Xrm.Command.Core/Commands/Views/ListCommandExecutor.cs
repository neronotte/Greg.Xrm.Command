using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Views
{
	public class ListCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IOutput output,
		ISavedQueryRepository savedQueryRepository,
		IUserQueryRepository userQueryRepository

	) : ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			var viewList = new List<TableView>();
			if (command.QueryType == QueryType.SavedQuery || command.QueryType == QueryType.Both)
			{
				output.Write($"Retrieving saved queries for table '{command.TableName}'...");

				var savedQueries = await savedQueryRepository.GetByTableNameAsync(crm, command.TableName);
				viewList.AddRange(savedQueries);

				output.WriteLine("Done", ConsoleColor.Green);
			}

			if (command.QueryType == QueryType.UserQuery || command.QueryType == QueryType.Both)
			{
				output.Write($"Retrieving user queries for table '{command.TableName}'...");

				var userQueries = await userQueryRepository.GetByTableNameAsync(crm, command.TableName);
				viewList.AddRange(userQueries);

				output.WriteLine("Done", ConsoleColor.Green);
			}

			output.WriteLine($"Found {viewList.Count} views for table '{command.TableName}'");
			if (viewList.Count == 0)
			{
				return CommandResult.Success();
			}

			viewList = [.. viewList
				.OrderBy(x => x.GetType().Name)
				.ThenBy(x => x.querytype)
				.ThenBy(x => x.name)];

			output.WriteLine();
			output.WriteTable(viewList, () => ["Ownership", "Name", "View Type"],
			row => {

				var ownership = row.GetType() == typeof(SavedQuery) ? "Saved" : "User";
				var name = row.name ?? string.Empty;
				var type = row.GetQueryTypeLabel();

				return [ownership, name, type];
			});


			return CommandResult.Success();
		}
	}
}
