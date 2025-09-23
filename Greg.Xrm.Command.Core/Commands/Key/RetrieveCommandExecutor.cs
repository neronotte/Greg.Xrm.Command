using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Newtonsoft.Json;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Key
{
	public class RetrieveCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
	)
	: ICommandExecutor<RetrieveCommand>
	{
		public async Task<CommandResult> ExecuteAsync(RetrieveCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				output.Write($"Retrieving key '{command.SchemaName}' on table '{command.Table}'...");
				var request = new RetrieveEntityKeyRequest
				{
					EntityLogicalName = command.Table,
					LogicalName = command.SchemaName
				};

				var response = (RetrieveEntityKeyResponse)await crm.ExecuteAsync(request);
				if (response.EntityKeyMetadata == null)
				{
					return CommandResult.Fail("No key found with the provided schema name.");
				}

				var dto = new
				{
					Table= response.EntityKeyMetadata.EntityLogicalName,
					response.EntityKeyMetadata.SchemaName,
					DisplayName = response.EntityKeyMetadata.DisplayName?.UserLocalizedLabel?.Label,
					Columns = response.EntityKeyMetadata.KeyAttributes,
					IndexStatus = response.EntityKeyMetadata.EntityKeyIndexStatus.ToString(),
				};

				var json = JsonConvert.SerializeObject(dto, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["Key"] = json;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Error: " + ex.Message);
			}
		}
	}
}
