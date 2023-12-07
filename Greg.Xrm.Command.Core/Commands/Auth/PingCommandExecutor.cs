using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Auth
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

        public async Task<CommandResult> ExecuteAsync(PingCommand command, CancellationToken cancellationToken)
        {
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();

            if (crm == null)
            {
                return CommandResult.Fail("No connection selected.");
            }

            try
            {
                var request = new WhoAmIRequest();
                var response = (WhoAmIResponse)await crm.ExecuteAsync(request);

                output
                    .Write("Connection successful. User: ")
                    .Write(response.UserId.ToString())
                    .WriteLine();

                return CommandResult.Success();
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                return CommandResult.Fail("Connection failed. " + ex.Message, ex);
            }
            catch (Exception ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
    }
}
