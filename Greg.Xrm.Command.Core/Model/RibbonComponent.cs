using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	/// <summary>A solution component projected for ribbon solution validation.</summary>
	public class RibbonComponent(Entity entity) : EntityWrapper(entity)
	{
		public int? Type => GetChoice("componenttype");
		public Guid ObjectId => Get<Guid>("objectid");
		public int? RootBehavior => GetChoice("rootcomponentbehavior");
		public Guid? RootSolutionComponentId => Get<object>("rootsolutioncomponentid") switch
		{
			EntityReference reference => reference.Id,
			Guid id => id,
			_ => null
		};

		private int? GetChoice(string name) => Get<object>(name) switch
		{
			OptionSetValue option => option.Value,
			int number => number,
			_ => null
		};

		public class Repository : IRibbonComponentRepository
		{
			// Dataverse built-in Active solution ID used to locate the application ribbon.
			// https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.addsolutioncomponentrequest#adding-application-ribbons-to-a-solution
			private static readonly Guid ActiveSolutionId = new("FD140AAE-4DF4-11DD-BD17-0019B9312238");

			public async Task<IReadOnlyList<RibbonComponent>> GetSolutionComponentsAsync(
				IOrganizationServiceAsync2 crm, Guid solutionId, int maximumCount, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("solutioncomponent") { TopCount = maximumCount };
				query.ColumnSet.AddColumns("componenttype", "objectid", "rootcomponentbehavior", "rootsolutioncomponentid");
				query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(entity => new RibbonComponent(entity)).ToList();
			}

			public async Task<IReadOnlyList<Guid>> GetApplicationRibbonIdsAsync(
				IOrganizationServiceAsync2 crm, bool activeOnly, Guid? ribbonId, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("ribboncustomization") { TopCount = 2 };
				query.ColumnSet.AddColumn("ribboncustomizationid");
				query.Criteria.AddCondition("entity", ConditionOperator.Null);
				if (activeOnly)
					query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, ActiveSolutionId);
				if (ribbonId.HasValue)
					query.Criteria.AddCondition("ribboncustomizationid", ConditionOperator.Equal, ribbonId.Value);
				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(entity => entity.Id).ToList();
			}

			public async Task<(string? TableName, string? Name)> GetComponentNamesAsync(
				IOrganizationServiceAsync2 crm, ComponentType componentType, Guid objectId, CancellationToken cancellationToken)
			{
				var (entityName, idColumn, tableColumn) = componentType switch
				{
					ComponentType.SystemForm => ("systemform", "formid", "objecttypecode"),
					ComponentType.SavedQuery => ("savedquery", "savedqueryid", "returnedtypecode"),
					ComponentType.SavedQueryVisualization => ("savedqueryvisualization", "savedqueryvisualizationid", "primaryentitytypecode"),
					_ => throw new ArgumentOutOfRangeException(nameof(componentType))
				};
				var query = new QueryExpression(entityName) { TopCount = 1 };
				query.ColumnSet.AddColumns(tableColumn, "name");
				query.Criteria.AddCondition(idColumn, ConditionOperator.Equal, objectId);
				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var record = result.Entities.FirstOrDefault();
				return (record?.GetAttributeValue<string>(tableColumn), record?.GetAttributeValue<string>("name"));
			}
		}
	}
}
