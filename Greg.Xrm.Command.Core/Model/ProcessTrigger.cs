using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class ProcessTrigger : EntityWrapper
	{
		public ProcessTrigger(Entity entity) : base(entity)
		{
		}

		public string? controlname => Get<string>();


		public class Repository : IProcessTriggerRepository
		{
			public async Task<IReadOnlyList<ProcessTrigger>> GetByWorkflowIdAsync(IOrganizationServiceAsync2 crm, Guid workflowId)
			{
				var query = new QueryExpression("processtrigger");
				query.ColumnSet.AddColumns(nameof(controlname));
				query.Criteria.AddCondition("processid", ConditionOperator.Equal, workflowId);
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities.Select(x => new ProcessTrigger(x)).ToArray();
			}
		}
	}
}
