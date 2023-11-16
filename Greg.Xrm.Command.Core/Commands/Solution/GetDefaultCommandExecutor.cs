using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class GetDefaultCommandExecutor : ICommandExecutor<GetDefaultCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public GetDefaultCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}



		public async Task ExecuteAsync(GetDefaultCommand command, CancellationToken cancellationToken)
		{

			try
			{
				var connections = await this.organizationServiceRepository.GetAllConnectionDefinitionsAsync();

				var currentConnection = connections.CurrentConnectionStringKey;
				if (string.IsNullOrWhiteSpace(currentConnection))
				{
					this.output.WriteLine("No default connection selected. Please use 'pacx auth select' to select a default connection.", ConsoleColor.Red);
					return;
				}

				if (connections.DefaultSolutions.TryGetValue(currentConnection, out var defaultSolutionName))
				{
					this.output.Write("Default solution is: '").Write(defaultSolutionName, ConsoleColor.Yellow).Write("'").WriteLine();
					return;
				}

				this.output.WriteLine("No default solution set for the current connection. Current connection is " + currentConnection, ConsoleColor.Yellow);
			}
			catch (Exception ex)
			{
				this.output.WriteLine("Error while getting default solution: " + ex.Message, ConsoleColor.Red);
			}
		}
	}
}
