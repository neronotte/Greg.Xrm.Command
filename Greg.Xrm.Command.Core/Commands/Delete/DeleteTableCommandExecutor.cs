using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Delete
{
	public class DeleteTableCommandExecutor : ICommandExecutor<DeleteTableCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public DeleteTableCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}
		public async Task ExecuteAsync(DeleteTableCommand command, CancellationToken cancellationToken)
		{
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();

			try
			{
				this.output.Write("Deleting table ").Write(command.SchemaName, ConsoleColor.Yellow).Write("...");

				var request = new DeleteEntityRequest
				{
					LogicalName = command.SchemaName
				};

				await crm.ExecuteAsync(request);

				this.output.WriteLine(" Done", ConsoleColor.Green);
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine()
					.Write("Error: ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);

				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
			}
		}
	}
}
