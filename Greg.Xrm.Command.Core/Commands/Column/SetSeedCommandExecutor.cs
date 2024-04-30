using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column
{
	public class SetSeedCommandExecutor : ICommandExecutor<SetSeedCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public SetSeedCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}


		public async Task<CommandResult> ExecuteAsync(SetSeedCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				this.output.Write($"Setting the seed for column {command.TableName}.{command.ColumnName} to {command.Seed}...");

				var request = new OrganizationRequest("SetAutoNumberSeed");
				request["EntityName"] = command.TableName;
				request["AttributeName"] = command.ColumnName;
				request["Value"] = command.Seed;

				await crm.ExecuteAsync(request, cancellationToken);

				this.output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				this.output.WriteLine(ex.Message, ConsoleColor.Red);

				return CommandResult.Fail(ex.Message);
			}
		}
	}
}
