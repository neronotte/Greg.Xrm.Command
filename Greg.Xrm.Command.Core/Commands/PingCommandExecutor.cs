using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands
{
	public class PingCommandExecutor : ICommandExecutor<PingCommand>
	{
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IOutput output;

		public PingCommandExecutor(IOrganizationServiceRepository organizationServiceRepository, IOutput output)
		{
			this.organizationServiceRepository = organizationServiceRepository;
			this.output = output;
		}

		public async Task ExecuteAsync(PingCommand command, CancellationToken cancellationToken)
		{
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();

			if (crm == null)
			{
				this.output.WriteLine("No connection selected.");
				return;
			}

			try
			{
				var request = new WhoAmIRequest();
				var response = (WhoAmIResponse) await crm.ExecuteAsync(request);

				output
					.Write("Connection successful. User: ")
					.Write(response.UserId.ToString())
					.WriteLine();
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				output
					.Write("Connection failed. ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);
				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
			}
			catch(Exception ex)
			{
				output
					.Write("Connection exception '", ConsoleColor.Red)
					.Write(ex.GetType()?.FullName ?? string.Empty, ConsoleColor.Red)
					.Write("'. ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);
				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
			}

		}
	}
}
