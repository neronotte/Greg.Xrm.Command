using System.ServiceModel;
using Greg.Xrm.Command.Commands.CustomApi.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class BindCustomApiCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<BindCustomApiCommand>
	{
		public async Task<CommandResult> ExecuteAsync(BindCustomApiCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Resolve the Custom API
				output.Write($"Resolving Custom API '{command.ApiUniqueName}'...");
				var apiQ = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
				apiQ.ColumnSet.AddColumn("customapiid");
				apiQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.ApiUniqueName);
				var apiResult = await crm.RetrieveMultipleAsync(apiQ);
				if (apiResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.ApiUniqueName}' not found.");
				}
				var apiId = apiResult.Entities[0].Id;
				output.WriteLine("Done", ConsoleColor.Green);

				// Resolve the plugin type
				output.Write($"Resolving plugin type '{command.PluginTypeName}'...");
				var typeQ = new QueryExpression("plugintype") { NoLock = true };
				typeQ.ColumnSet.AddColumns("plugintypeid", "typename", "name");
				typeQ.Criteria.AddCondition("typename", ConditionOperator.Equal, command.PluginTypeName);
				if (!string.IsNullOrWhiteSpace(command.AssemblyName))
				{
					var asmLink = typeQ.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid");
					asmLink.LinkCriteria.AddCondition("name", ConditionOperator.Equal, command.AssemblyName);
				}
				var typeResult = await crm.RetrieveMultipleAsync(typeQ);
				if (typeResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					var assemblyHint = command.AssemblyName != null ? $" in assembly '{command.AssemblyName}'" : "";
					return CommandResult.Fail($"Plugin type '{command.PluginTypeName}'{assemblyHint} not found.");
				}
				if (typeResult.Entities.Count > 1)
				{
					output.WriteLine("Ambiguous", ConsoleColor.Red);
					return CommandResult.Fail($"Multiple plugin types found with name '{command.PluginTypeName}'. Use --assembly to disambiguate.");
				}
				var pluginTypeId = typeResult.Entities[0].Id;
				var pluginTypeName = typeResult.Entities[0].GetAttributeValue<string>("typename");
				output.WriteLine("Done", ConsoleColor.Green);

				// Patch the customapi record with the plugin type reference
				output.Write($"Binding '{command.ApiUniqueName}' to plugin '{pluginTypeName}'...");
				var api = new Model.CustomApi(apiId)
				{
					plugintypeid = new EntityReference("plugintype", pluginTypeId)
				};
				await api.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

				output.WriteLine($"Custom API '{command.ApiUniqueName}' is now bound to plugin '{pluginTypeName}'.", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["Custom API"] = command.ApiUniqueName!;
				result["Plugin Type"] = pluginTypeName ?? string.Empty;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
