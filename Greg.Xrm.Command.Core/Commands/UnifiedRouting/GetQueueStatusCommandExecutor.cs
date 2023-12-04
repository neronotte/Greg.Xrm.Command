﻿using Greg.Xrm.Command.Commands.UnifiedRouting.Model;
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

                output.WriteLine($"Checking queue status {command.Queue}");
                DateTime parsedTime;

                var isDateTimeParsed = DateTime.TryParse(command.DateTimeStatus, out parsedTime);

                // Set Condition Values
                var timeQuery = isDateTimeParsed ? parsedTime : DateTime.UtcNow;

                // Instantiate QueryExpression query
                var query = new QueryExpression("msdyn_agentstatushistory");
                query.NoLock = true;
                // Add columns to query.ColumnSet
                query.ColumnSet.AddColumns(
                    "msdyn_agentstatushistoryid",
                    "createdon",
                    "msdyn_starttime",
                    "msdyn_presenceid",
                    "msdyn_endtime",
                    "msdyn_availablecapacity",
                    "msdyn_agentid");

                // Add conditions to query.Criteria
                query.Criteria.AddCondition("msdyn_starttime", ConditionOperator.LessEqual, timeQuery);
                var endTimeFiltered = new FilterExpression(LogicalOperator.Or);
                query.Criteria.AddFilter(endTimeFiltered);
                endTimeFiltered.AddCondition("msdyn_endtime", ConditionOperator.GreaterEqual, timeQuery);
                endTimeFiltered.AddCondition("msdyn_endtime", ConditionOperator.Null);

                query.AddOrder("createdon", OrderType.Descending);

                var systemuserJoin = query.AddLink("systemuser", "msdyn_agentid", "systemuserid");
                systemuserJoin.EntityAlias = "systemuserJoin";
                systemuserJoin.Columns.AddColumns("fullname", "internalemailaddress");

                var queueMembershipJoin = systemuserJoin.AddLink("queuemembership", "systemuserid", "systemuserid");
                var queueJoin = queueMembershipJoin.AddLink("queue", "queueid", "queueid");
                queueJoin.LinkCriteria.AddCondition("name", ConditionOperator.Equal, command.Queue);

                var presenceJoin = query.AddLink("msdyn_presence", "msdyn_presenceid", "msdyn_presenceid", JoinOperator.Inner);
                presenceJoin.EntityAlias = "presenceJoin";

                // Add columns to presence.Columns
                presenceJoin.Columns.AddColumns("msdyn_presencestatustext", "msdyn_basepresencestatus");

                var results = (await crm.RetrieveMultipleAsync(query)).Entities;
                if (results.Count==0)
                {
                    output.WriteLine("No records found for: ", ConsoleColor.Yellow).WriteLine(command.Queue, ConsoleColor.Yellow);
                    return;
                }

                this.output.Write("The agents status in ").Write(command.Queue).Write(" at ").Write(timeQuery.ToLocalTime().ToString()).WriteLine(" is:");

                var tableResult = new List<AgentStatus>();
                foreach (Entity result in results)
                {
                    tableResult.Add(new AgentStatus
                    {
                        UserEmail = result.GetAliasedValue<string>("systemuserJoin.internalemailaddress"),
                        Status = result.GetAliasedValue<string>("presenceJoin.msdyn_presencestatustext"),
                        StatusCode = result.GetAliasedValue<OptionSetValue>("presenceJoin.msdyn_basepresencestatus")?.Value,
                        DateStart = result.GetAttributeValue<DateTime>("msdyn_starttime")
                    });
                }

                this.output.WriteTable(tableResult, 
                    () => new[] { "User", "Status", "Since" },
                    user => new[] {
                        user.UserEmail ?? string.Empty,
                        user.Status ?? string.Empty,
                        user.DateStart.GetValueOrDefault().ToLocalTime().ToString()
                    },
                    (index, row) =>
                    {
                        if (index == 1)
                            return GetAgentStatusColor(row.StatusCode.GetValueOrDefault());

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


        private static ConsoleColor GetAgentStatusColor(int value)
        {
            switch (value)
            {
                case 192360000:
                    return ConsoleColor.Green;
                case 192360001:
                case 192360002:
                    return ConsoleColor.Red;
                case 192360003:
                    return ConsoleColor.DarkYellow;
                default:
                    return ConsoleColor.DarkGray;
            }
        }
    }
}