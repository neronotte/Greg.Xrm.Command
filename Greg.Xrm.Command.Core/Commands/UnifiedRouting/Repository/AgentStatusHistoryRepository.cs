﻿using Greg.Xrm.Command.Commands.UnifiedRouting.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.UnifiedRouting.Repository
{
	public class AgentStatusHistoryRepository
	{
		private readonly IOrganizationServiceAsync2 crm;


		public AgentStatusHistoryRepository(IOrganizationServiceAsync2 crm) {
			this.crm = crm;        
		}


		public async Task<DataCollection<Entity>> GetAgentStatusHistoryByQueue(string queueName, DateTime timeQuery)
		{
			var query = new QueryExpression(nameof(msdyn_agentstatushistory));
			query.NoLock = true;
			// Add columns to query.ColumnSet
			query.ColumnSet.AddColumns(
				msdyn_agentstatushistory.msdyn_agentstatushistoryid,
				msdyn_agentstatushistory.createdon,
				msdyn_agentstatushistory.msdyn_starttime,
				msdyn_agentstatushistory.msdyn_presenceid,
				msdyn_agentstatushistory.msdyn_endtime,
				msdyn_agentstatushistory.msdyn_availablecapacity,
				msdyn_agentstatushistory.msdyn_agentid);

			// Add conditions to query.Criteria
			query.Criteria.AddCondition(msdyn_agentstatushistory.msdyn_starttime, ConditionOperator.LessEqual, timeQuery);
			var endTimeFiltered = new FilterExpression(LogicalOperator.Or);
			query.Criteria.AddFilter(endTimeFiltered);
			endTimeFiltered.AddCondition(msdyn_agentstatushistory.msdyn_endtime, ConditionOperator.GreaterEqual, timeQuery);
			endTimeFiltered.AddCondition(msdyn_agentstatushistory.msdyn_endtime, ConditionOperator.Null);

			query.AddOrder(msdyn_agentstatushistory.createdon, OrderType.Descending);

			var systemuserJoin = query.AddLink(nameof(systemuser), msdyn_agentstatushistory.msdyn_agentid, systemuser.systemuserid);
			systemuserJoin.Columns.AddColumns(systemuser.fullname, systemuser.internalemailaddress);
			systemuserJoin.EntityAlias = nameof(systemuser);

			var queueMembershipJoin = systemuserJoin.AddLink(nameof(queuemembership), systemuser.systemuserid, queuemembership.systemuserid);
			var queueJoin = queueMembershipJoin.AddLink(nameof(queue), queue.queueid, queuemembership.queueid);
			queueJoin.LinkCriteria.AddCondition(queue.name, ConditionOperator.Equal, queueName);

			var presenceJoin = query.AddLink(nameof(msdyn_presence), msdyn_presence.msdyn_presenceid, msdyn_presence.msdyn_presenceid, JoinOperator.Inner);
			presenceJoin.EntityAlias = nameof(msdyn_presence);

			// Add columns to presence.Columns
			presenceJoin.Columns.AddColumns(msdyn_presence.msdyn_presencestatustext, msdyn_presence.msdyn_basepresencestatus);

			return (await crm.RetrieveMultipleAsync(query)).Entities;
		}

		public async Task<Entity?> GetAgentStatusHistoryByAgentMail(string agentEmail, DateTime timeQuery)
		{
			var query = new QueryExpression(nameof(msdyn_agentstatushistory));
			query.NoLock = true;
			// Add columns to query.ColumnSet
			query.ColumnSet.AddColumns(
				msdyn_agentstatushistory.msdyn_agentstatushistoryid,
				msdyn_agentstatushistory.createdon,
				msdyn_agentstatushistory.msdyn_starttime,
				msdyn_agentstatushistory.msdyn_presenceid,
				msdyn_agentstatushistory.msdyn_endtime,
				msdyn_agentstatushistory.msdyn_availablecapacity,
				msdyn_agentstatushistory.msdyn_agentid);

			// Add conditions to query.Criteria
			query.Criteria.AddCondition(msdyn_agentstatushistory.msdyn_starttime, ConditionOperator.LessEqual, timeQuery);
			var endTimeFiltered = new FilterExpression(LogicalOperator.Or);
			query.Criteria.AddFilter(endTimeFiltered);
			endTimeFiltered.AddCondition(msdyn_agentstatushistory.msdyn_endtime, ConditionOperator.GreaterEqual, timeQuery);
			endTimeFiltered.AddCondition(msdyn_agentstatushistory.msdyn_endtime, ConditionOperator.Null);

			query.AddOrder(msdyn_agentstatushistory.createdon, OrderType.Descending);

			var systemuserJoin = query.AddLink(nameof(systemuser), msdyn_agentstatushistory.msdyn_agentid, systemuser.systemuserid);
			systemuserJoin.Columns.AddColumns(systemuser.fullname, systemuser.internalemailaddress);
			systemuserJoin.EntityAlias = nameof(systemuser);

			var queryAgentAddress = new FilterExpression(LogicalOperator.Or);
			systemuserJoin.LinkCriteria.AddFilter(queryAgentAddress);

			// Add conditions to aa.LinkCriteria
			queryAgentAddress.AddCondition(systemuser.internalemailaddress, ConditionOperator.Equal, agentEmail);
			queryAgentAddress.AddCondition(systemuser.domainname, ConditionOperator.Equal, agentEmail);

			var presenceJoin = query.AddLink(nameof(msdyn_presence), msdyn_agentstatushistory.msdyn_presenceid, msdyn_presence.msdyn_presenceid, JoinOperator.Inner);
			presenceJoin.EntityAlias = nameof(msdyn_presence);
			// Add columns to presence.Columns
			presenceJoin.Columns.AddColumns(msdyn_presence.msdyn_presencestatustext, msdyn_presence.msdyn_basepresencestatus);

			return (await crm.RetrieveMultipleAsync(query)).Entities.FirstOrDefault();
		}

		public ConsoleColor GetAgentStatusColor(OptionSetValue? presenceOptions)
		{
			return (presenceOptions?.Value) switch
			{
				(int)msdyn_presence.AgentStatuses.Available => ConsoleColor.Green,
				(int)msdyn_presence.AgentStatuses.Busy or (int)msdyn_presence.AgentStatuses.BusyDND => ConsoleColor.Red,
				(int)msdyn_presence.AgentStatuses.Away => ConsoleColor.DarkYellow,
				_ => ConsoleColor.DarkGray,
			};
		}

	}
}
