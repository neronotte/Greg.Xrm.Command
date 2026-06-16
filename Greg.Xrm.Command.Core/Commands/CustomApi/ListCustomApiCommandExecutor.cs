using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class ListCustomApiCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<ListCustomApiCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCustomApiCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.Write("Retrieving Custom APIs...");

				var q = new QueryExpression("customapi") { NoLock = true };
				q.ColumnSet.AddColumns("uniquename", "displayname", "isfunction", "bindingtype", "plugintypeid");

				if (!string.IsNullOrWhiteSpace(command.Publisher))
					q.Criteria.AddCondition("uniquename", ConditionOperator.BeginsWith, command.Publisher + "_");

				if (command.Type.HasValue)
					q.Criteria.AddCondition("isfunction", ConditionOperator.Equal, command.Type.Value == CustomApiType.Function);

				q.AddOrder("uniquename", OrderType.Ascending);

				var results = await crm.RetrieveMultipleAsync(q);
				output.WriteLine("Done", ConsoleColor.Green);

				var rows = results.Entities.AsEnumerable();

				// Client-side filter substring (OData doesn't support case-insensitive contains natively via QueryExpression)
				if (!string.IsNullOrWhiteSpace(command.Filter))
				{
					var filter = command.Filter.ToLowerInvariant();
					rows = rows.Where(e =>
						(e.GetAttributeValue<string>("uniquename") ?? "").ToLowerInvariant().Contains(filter) ||
						(e.GetAttributeValue<string>("displayname") ?? "").ToLowerInvariant().Contains(filter));
				}

				var list = rows.ToList();
				if (list.Count == 0)
				{
					output.WriteLine("No Custom APIs found matching the specified criteria.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.WriteLine();
				output.WriteTable(
					list,
					() => ["Unique Name", "Display Name", "Type", "Binding Type", "Plugin Bound"],
					row => [
						row.GetAttributeValue<string>("uniquename") ?? "",
						row.GetAttributeValue<string>("displayname") ?? "",
						row.GetAttributeValue<bool>("isfunction") ? "Function" : "Action",
						BindingTypeLabel(row.GetAttributeValue<Microsoft.Xrm.Sdk.OptionSetValue>("bindingtype")),
						row.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("plugintypeid") != null ? "Yes" : "No"
					],
					(col, _) => col switch
					{
						0 => ConsoleColor.White,
						2 => ConsoleColor.Cyan,
						_ => (ConsoleColor?)null
					});

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static string BindingTypeLabel(Microsoft.Xrm.Sdk.OptionSetValue? value) => value?.Value switch
		{
			0 => "Global",
			1 => "Entity",
			2 => "EntityCollection",
			_ => "Unknown"
		};
	}
}
