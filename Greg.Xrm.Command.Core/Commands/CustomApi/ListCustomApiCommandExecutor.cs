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
				q.ColumnSet.AddColumns("customapiid", "uniquename", "displayname", "isfunction", "bindingtype", "plugintypeid");

				if (!string.IsNullOrWhiteSpace(command.Publisher))
					q.Criteria.AddCondition("uniquename", ConditionOperator.BeginsWith, command.Publisher + "_");

				if (command.Type.HasValue)
					q.Criteria.AddCondition("isfunction", ConditionOperator.Equal, command.Type.Value == CustomApiType.Function);

				q.AddOrder("uniquename", OrderType.Ascending);

				var results = await crm.RetrieveMultipleAsync(q);
				output.WriteLine("Done", ConsoleColor.Green);

				var rows = results.Entities.AsEnumerable();

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

				if (!command.Full)
				{
					output.WriteTable(
						list,
						() => ["Unique Name", "Display Name", "Type", "Binding", "Plugin Bound"],
						row => [
							row.GetAttributeValue<string>("uniquename") ?? "",
							row.GetAttributeValue<string>("displayname") ?? "",
							row.GetAttributeValue<bool>("isfunction") ? "Function" : "Action",
							BindingTypeLabel(row.GetAttributeValue<OptionSetValue>("bindingtype")),
							row.GetAttributeValue<EntityReference>("plugintypeid") != null ? "Yes" : "No"
						],
						(col, _) => col switch
						{
							0 => ConsoleColor.White,
							2 => ConsoleColor.Cyan,
							_ => (ConsoleColor?)null
						});
				}
				else
				{
					// ── Full mode: batch-load params and responses, then show signature per API ──
					var apiIds = list.Select(e => e.Id).ToList();

					var paramQ = new QueryExpression("customapirequestparameter") { NoLock = true };
					paramQ.ColumnSet.AddColumns("customapiid", "uniquename", "type", "isoptional");
					paramQ.Criteria.AddCondition("customapiid", ConditionOperator.In, apiIds.Cast<object>().ToArray());
					var allParams = (await crm.RetrieveMultipleAsync(paramQ)).Entities;

					var respQ = new QueryExpression("customapiresponseproperty") { NoLock = true };
					respQ.ColumnSet.AddColumns("customapiid", "uniquename", "type");
					respQ.Criteria.AddCondition("customapiid", ConditionOperator.In, apiIds.Cast<object>().ToArray());
					var allResps = (await crm.RetrieveMultipleAsync(respQ)).Entities;

					var paramsByApi = allParams.GroupBy(p => p.GetAttributeValue<EntityReference>("customapiid")?.Id ?? Guid.Empty)
						.ToDictionary(g => g.Key, g => g.ToList());
					var respsByApi  = allResps.GroupBy(r => r.GetAttributeValue<EntityReference>("customapiid")?.Id ?? Guid.Empty)
						.ToDictionary(g => g.Key, g => g.ToList());

					foreach (var api in list)
					{
						var uniqueName  = api.GetAttributeValue<string>("uniquename") ?? "";
						var displayName = api.GetAttributeValue<string>("displayname") ?? "";
						var isFunction  = api.GetAttributeValue<bool>("isfunction");
						var binding     = BindingTypeLabel(api.GetAttributeValue<OptionSetValue>("bindingtype"));
						var bound       = api.GetAttributeValue<EntityReference>("plugintypeid") != null;

						var apiParams = paramsByApi.GetValueOrDefault(api.Id) ?? [];
						var apiResps  = respsByApi.GetValueOrDefault(api.Id) ?? [];

						var inputParams = apiParams
								.OrderBy(p => p.GetAttributeValue<bool>("isoptional"))
								.Select(p => (
									name: p.GetAttributeValue<string>("uniquename") ?? "",
									type: TypeLabel(p.GetAttributeValue<OptionSetValue>("type")),
									opt:  p.GetAttributeValue<bool>("isoptional")));
							var outputParams = apiResps.Select(r => (
								name: r.GetAttributeValue<string>("uniquename") ?? "",
								type: TypeLabel(r.GetAttributeValue<OptionSetValue>("type"))));

							output.Write(uniqueName, ConsoleColor.White);
							output.Write($"  [{(isFunction ? "Function" : "Action")}/{binding}]", ConsoleColor.DarkGray);
							if (!bound) output.Write("  (unbound)", ConsoleColor.Yellow);
							output.WriteLine();
							output.Write("  ");
							CustomApiSignatureWriter.WriteSignature(output, uniqueName, inputParams, outputParams);
							output.WriteLine();
						if (!string.Equals(uniqueName, displayName, StringComparison.OrdinalIgnoreCase))
						{
							output.Write("  Display Name: ");
							output.WriteLine(displayName, ConsoleColor.DarkGray);
						}
						output.WriteLine();
					}
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static string BindingTypeLabel(OptionSetValue? value) => value?.Value switch
		{
			0 => "Global",
			1 => "Entity",
			2 => "EntityCollection",
			_ => "Unknown"
		};

		private static string TypeLabel(OptionSetValue? value) => value?.Value switch
		{
			0  => "Boolean",
			1  => "DateTime",
			2  => "Decimal",
			3  => "Entity",
			4  => "EntityCollection",
			5  => "EntityReference",
			6  => "Float",
			7  => "Integer",
			8  => "Money",
			9  => "Picklist",
			10 => "String",
			11 => "StringArray",
			12 => "Guid",
			_  => "Unknown"
		};
	}
}
