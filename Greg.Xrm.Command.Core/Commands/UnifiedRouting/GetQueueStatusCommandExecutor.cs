using Greg.Xrm.Command.Commands.UnifiedRouting.Model;
using Greg.Xrm.Command.Commands.UnifiedRouting.Repository;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
    public class GetQueueStatusCommandExecutor : ICommandExecutor<GetQueueStatusCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;

        public GetQueueStatusCommandExecutor(
            IOutput output,
            IOrganizationServiceRepository organizationServiceFactory)
        {
            this.output = output;
            organizationServiceRepository = organizationServiceFactory;
        }

        public async Task ExecuteAsync(GetQueueStatusCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();

            if (crm == null)
            {
                output.WriteLine("No connection selected.");
                return;
            }

            this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{
                var repo = new AgentStatusHistoryRepository(crm);

                output.WriteLine($"Checking queue status {command.Queue}");
                DateTime parsedTime;

                var isDateTimeParsed = DateTime.TryParse(command.DateTimeStatus, out parsedTime);

                // Set Condition Values
                var timeQuery = isDateTimeParsed ? parsedTime : DateTime.UtcNow;

                var results = await repo.GetAgentStatusHistoryByQueue(command.Queue ?? string.Empty, timeQuery);

                if (results.Count==0)
                {
                    output.WriteLine("No records found for: ", ConsoleColor.Yellow).WriteLine(command.Queue, ConsoleColor.Yellow);
                    return;
                }

                this.output.Write("The agents status in ").Write(command.Queue).Write(" at ").Write(timeQuery.ToLocalTime().ToString()).WriteLine(" is:");


                this.output.WriteTable(results, 
                    () => new[] { "User", "Status", "Since" },
                    user => new [] {
                        user.GetAliasedValue<string>(systemuser.internalemailaddress, nameof(systemuser)) ?? string.Empty,
                        user.GetAliasedValue<string>(msdyn_presence.msdyn_presencestatustext, nameof(msdyn_presence)) ?? string.Empty,
                        user.GetAttributeValue<DateTime?>(msdyn_agentstatushistory.msdyn_starttime).GetValueOrDefault().ToLocalTime().ToString()
                    },
                    (index, row) =>
                    {
                        if (index == 1)
                            return repo.GetAgentStatusColor(row.GetAliasedValue<OptionSetValue?>(msdyn_presence.msdyn_basepresencestatus));

                        return null;
                    }
                );
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