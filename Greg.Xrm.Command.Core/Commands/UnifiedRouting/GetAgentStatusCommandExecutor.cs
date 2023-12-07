using Greg.Xrm.Command.Commands.UnifiedRouting.Model;
using Greg.Xrm.Command.Commands.UnifiedRouting.Repository;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.Globalization;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
    public class GetAgentStatusCommandExecutor : ICommandExecutor<GetAgentStatusCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;

        public GetAgentStatusCommandExecutor(
            IOutput output,
            IOrganizationServiceRepository organizationServiceFactory)
        {
            this.output = output;
            organizationServiceRepository = organizationServiceFactory;
        }

        public async Task ExecuteAsync(GetAgentStatusCommand command, CancellationToken cancellationToken)
        {
            try
            {
                DateTime timeQuery = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(command.DateTimeFilter) && !DateTime.TryParseExact(command.DateTimeFilter, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out timeQuery))
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, "Invalid format date provided. Expected dd/MM/yyyy.");


                this.output.Write($"Connecting to the current dataverse environment...");
                var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();

                if (crm == null)
                {
                    output.WriteLine("No connection selected.");
                    return;
                }

                this.output.WriteLine("Done", ConsoleColor.Green);

                output.WriteLine($"Checking agent status {command.AgentPrimaryEmail}");

                var repo = new AgentStatusHistoryRepository(crm);
                var result = await repo.GetAgentStatusHistoryByAgentMail(command.AgentPrimaryEmail ?? string.Empty, timeQuery);

                if (result == null)
                {
                    output.WriteLine("No records found for: ", ConsoleColor.Yellow).WriteLine(command.AgentPrimaryEmail, ConsoleColor.Yellow);
                    return;
                }

                var status = result.GetAliasedValue<string>(msdyn_presence.msdyn_presencestatustext, nameof(msdyn_presence));
                var statusOpt = result.GetAliasedValue<OptionSetValue>(msdyn_presence.msdyn_basepresencestatus, nameof(msdyn_presence));
                var dateStart = result.GetAttributeValue<DateTime>(msdyn_agentstatushistory.msdyn_starttime);

                if (string.IsNullOrEmpty(command.DateTimeFilter))
                    this.output.WriteLine(command.AgentPrimaryEmail)
                    .Write("is ").WriteLine(status, repo.GetAgentStatusColor(statusOpt))
                    .Write("since ").WriteLine(dateStart.ToLocalTime().ToString());
                else
                    this.output.WriteLine(command.AgentPrimaryEmail)
                    .Write("at ").WriteLine(timeQuery.ToLocalTime().ToString())
                    .Write("was ").WriteLine(status, repo.GetAgentStatusColor(statusOpt))
                    .Write("since ").WriteLine(dateStart.ToLocalTime().ToString());
            }
            catch (FaultException<OrganizationServiceFault> ex)
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