using System.ServiceModel;
using Greg.Xrm.Command.Commands.CustomApi.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class AddCustomApiResponseCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<AddCustomApiResponseCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AddCustomApiResponseCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Resolve the Custom API
				output.Write($"Resolving Custom API '{command.ApiUniqueName}'...");
				var q = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
					q.ColumnSet.AddColumns("customapiid", "displayname");
				q.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.ApiUniqueName);
				var apiResult = await crm.RetrieveMultipleAsync(q);
				if (apiResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.ApiUniqueName}' not found.");
				}
					var apiId          = apiResult.Entities[0].Id;
					var apiDisplayName = apiResult.Entities[0].GetAttributeValue<string>("displayname") ?? command.ApiUniqueName;
					output.WriteLine("Done", ConsoleColor.Green);

					CustomApiParamSpec.TryParse(command.Response!, out var spec, out _);
					var respUniqueName = spec!.UniqueName;  // already cleaned by TryParse
					var respName       = CustomApiDisplayNameHelper.BuildResponseName(apiDisplayName, respUniqueName);
					var displayName    = command.DisplayName ?? respName;

					// Idempotency check
					var existingQ = new QueryExpression("customapiresponseproperty") { NoLock = true, TopCount = 1 };
					existingQ.ColumnSet.AddColumn("customapiresponsepropertyid");
					existingQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, respUniqueName);
					existingQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
					var existingResult = await crm.RetrieveMultipleAsync(existingQ);
					if (existingResult.Entities.Count > 0)
						return CommandResult.Fail($"Response property '{respUniqueName}' already exists on Custom API '{command.ApiUniqueName}'.");

					output.Write($"Adding response property '{respUniqueName}'...");
					var resp = new CustomApiResponseProperty
					{
							name        = respName,
						uniquename  = respUniqueName,
					displayname = displayName,
					description = command.Description ?? string.Empty,
					type = new OptionSetValue(spec.TypeCode),
					customapiid = new EntityReference("customapi", apiId)
				};
				await resp.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["Response Property Id"] = resp.Id;
				result["Response Property Name"] = respUniqueName;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
