using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Messages;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	public class GetCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
			: ICommandExecutor<GetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				output.Write($"Retrieving global option set '{command.Name}'...");
				var request = new RetrieveOptionSetRequest
				{
					Name = command.Name
				};

				var response = (RetrieveOptionSetResponse)await crm.ExecuteAsync(request, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);


				var metadata = response.OptionSetMetadata;


				var metadataString = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				});

				var result = CommandResult.Success();
				result["OptionSet"] = metadataString;

				return result;
			}
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
