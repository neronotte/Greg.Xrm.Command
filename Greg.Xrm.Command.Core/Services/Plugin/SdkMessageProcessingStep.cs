using Greg.Xrm.Command.Model;
using Microsoft.Identity.Client;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public class SdkMessageProcessingStep : EntityWrapper
	{
		protected SdkMessageProcessingStep(Entity entity) : base(entity)
		{
		}

		public SdkMessageProcessingStep() : base("sdkmessageprocessingstep")
		{
		}


		public string name
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public OptionSetValue? mode
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}
		public OptionSetValue? stage
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}
		public bool? asyncautodelete
		{
			get => Get<bool>();
			set => SetValue(value);
		}

		public EntityReference? eventhandler
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
		public EntityReference? plugintypeid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
		public int? rank
		{
			get => Get<int>();
			set => SetValue(value);
		}
		public EntityReference? sdkmessageid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
		public OptionSetValue? supporteddeployment
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}
		public EntityReference? sdkmessagefilterid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
		public OptionSetValue? invocationsource
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}
		public string? filteringattributes
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? description
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? configuration
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public EntityReference? sdkmessageprocessingstepsecureconfigid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
		public OptionSetValue? statecode
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}
		public OptionSetValue? statuscode
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}

		public string primaryobjecttypecode => GetAliased<string>("filter.primaryobjecttypecode") ?? "";
		public string plugintypename => GetAliased<string>("pt.name") ?? "";
		public string messagename => GetAliased<string>("msg.name") ?? "";
		public string assemblyname => GetAliased<string>("pa.name") ?? "";
		public Guid plugintypeidaliased => plugintypeid?.Id ?? Guid.Empty;
		public Guid assemblyidaliased => GetAliased<Guid?>("pa.pluginassemblyid") ?? Guid.Empty;
		public bool isstepinsolution => GetAliased<Guid?>("stepsc.objectid") != null;

		public override string ToString()
		{
			return name;
		}


		public class Repository : ISdkMessageProcessingStepRepository
		{
			private readonly string[] columns = [
				"name",
				"mode",
				"stage",
				"asyncautodelete",
				"eventhandler",
				"plugintypeid",
				"rank",
				"sdkmessageid",
				"supporteddeployment",
				"sdkmessagefilterid",
				"invocationsource",
				"filteringattributes",
				"description",
				"configuration",
				"sdkmessageprocessingstepsecureconfigid" ,
				"statecode",
				"statuscode"
			];

			private static void AddStageFilter(QueryExpression query, bool includeInternalStages)
			{
				if (!includeInternalStages)
				{
					// Only include user-manageable stages: PreValidation (10), PreOperation (20), PostOperation (40)
					var stageFilter = query.Criteria.AddFilter(LogicalOperator.Or);
					stageFilter.AddCondition("stage", ConditionOperator.Equal, 10); // PreValidation
					stageFilter.AddCondition("stage", ConditionOperator.Equal, 20); // PreOperation
					stageFilter.AddCondition("stage", ConditionOperator.Equal, 40); // PostOperation
				}
			}

			public async Task<SdkMessageProcessingStep?> GetByIdAsync(IOrganizationServiceAsync2 crm, Guid id)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, id);
				query.NoLock = true;
				query.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(query);
				return result.Entities.Select(x => new SdkMessageProcessingStep(x)).FirstOrDefault();
			}

			public async Task<SdkMessageProcessingStep[]> GetByKeyAsync(IOrganizationServiceAsync2 crm, PluginType pluginType, string messageName, string? primaryEntityName, PluginRegistrationToolkit.Stage? stage)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, pluginType.Id);
				
				if (stage.HasValue)
				{
					query.Criteria.AddCondition("stage", ConditionOperator.Equal, (int)stage.Value);
				}

				var linkMessage = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid");
				linkMessage.LinkCriteria.AddCondition("name", ConditionOperator.Equal, messageName);

				if (!string.IsNullOrWhiteSpace(primaryEntityName))
				{
					var linkFilter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid");
					linkFilter.LinkCriteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, primaryEntityName.ToLowerInvariant());
				}

				var sdkMessageProcessingStepList = await crm.RetrieveMultipleAsync(query);
				return sdkMessageProcessingStepList.Entities.Select(x => new SdkMessageProcessingStep(x)).ToArray();
			}

			public async Task<SdkMessageProcessingStep[]> GetByAssemblyNameAsync(IOrganizationServiceAsync2 crm, string assemblyName, bool includeInternalStages, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				AddStageFilter(query, includeInternalStages);

				// Join with plugintype
				var linkPluginType = query.AddLink("plugintype", "plugintypeid", "plugintypeid");
				linkPluginType.Columns.AddColumns("name");
				linkPluginType.EntityAlias = "pt";

				// Join with pluginassembly
				var linkAssembly = linkPluginType.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid");
				linkAssembly.Columns.AddColumns("name", "pluginassemblyid");
				linkAssembly.EntityAlias = "pa";
				linkAssembly.LinkCriteria.AddCondition("name", ConditionOperator.Equal, assemblyName);

				// Join with sdkmessage for message name
				var linkMessage = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.LeftOuter);
				linkMessage.Columns.AddColumns("name");
				linkMessage.EntityAlias = "msg";

				// Join with sdkmessagefilter for table name
				var linkFilter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter);
				linkFilter.Columns.AddColumns("primaryobjecttypecode");
				linkFilter.EntityAlias = "filter";

				query.NoLock = true;
				query.AddOrder("name", OrderType.Ascending);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(x => new SdkMessageProcessingStep(x)).ToArray();
			}

			public async Task<SdkMessageProcessingStep[]> GetByAssemblyIdAsync(IOrganizationServiceAsync2 crm, Guid assemblyId, bool includeInternalStages, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				AddStageFilter(query, includeInternalStages);

				// Join with plugintype
				var linkPluginType = query.AddLink("plugintype", "plugintypeid", "plugintypeid");
				linkPluginType.Columns.AddColumns("name");
				linkPluginType.EntityAlias = "pt";

				// Join with pluginassembly
				var linkAssembly = linkPluginType.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid");
				linkAssembly.Columns.AddColumns("name", "pluginassemblyid");
				linkAssembly.EntityAlias = "pa";
				linkAssembly.LinkCriteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assemblyId);

				// Join with sdkmessage for message name
				var linkMessage = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.LeftOuter);
				linkMessage.Columns.AddColumns("name");
				linkMessage.EntityAlias = "msg";

				// Join with sdkmessagefilter for table name
				var linkFilter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter);
				linkFilter.Columns.AddColumns("primaryobjecttypecode");
				linkFilter.EntityAlias = "filter";

				query.NoLock = true;
				query.AddOrder("name", OrderType.Ascending);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(x => new SdkMessageProcessingStep(x)).ToArray();
			}

			public async Task<SdkMessageProcessingStep[]> GetByPluginTypeIdAsync(IOrganizationServiceAsync2 crm, Guid pluginTypeId, bool includeInternalStages, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, pluginTypeId);
				AddStageFilter(query, includeInternalStages);

				// Join with plugintype for name
				var linkPluginType = query.AddLink("plugintype", "plugintypeid", "plugintypeid");
				linkPluginType.Columns.AddColumns("name");
				linkPluginType.EntityAlias = "pt";

				// Join with pluginassembly for assembly name
				var linkAssembly = linkPluginType.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid");
				linkAssembly.Columns.AddColumns("name", "pluginassemblyid");
				linkAssembly.EntityAlias = "pa";

				// Join with sdkmessage for message name
				var linkMessage = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.LeftOuter);
				linkMessage.Columns.AddColumns("name");
				linkMessage.EntityAlias = "msg";

				// Join with sdkmessagefilter for table name
				var linkFilter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter);
				linkFilter.Columns.AddColumns("primaryobjecttypecode");
				linkFilter.EntityAlias = "filter";

				query.NoLock = true;
				query.AddOrder("name", OrderType.Ascending);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(x => new SdkMessageProcessingStep(x)).ToArray();
			}

			public async Task<SdkMessageProcessingStep[]> GetByTableNameAsync(IOrganizationServiceAsync2 crm, string tableName, bool includeInternalStages, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				AddStageFilter(query, includeInternalStages);

				// Join with plugintype for name
				var linkPluginType = query.AddLink("plugintype", "plugintypeid", "plugintypeid");
				linkPluginType.Columns.AddColumns("name");
				linkPluginType.EntityAlias = "pt";

				// Join with pluginassembly for assembly name
				var linkAssembly = linkPluginType.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid");
				linkAssembly.Columns.AddColumns("name", "pluginassemblyid");
				linkAssembly.EntityAlias = "pa";

				// Join with sdkmessage for message name
				var linkMessage = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid");
				linkMessage.Columns.AddColumns("name");
				linkMessage.EntityAlias = "msg";

				// Join with sdkmessagefilter for table name (required join since we're filtering by it)
				var linkFilter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid");
				linkFilter.Columns.AddColumns("primaryobjecttypecode");
				linkFilter.EntityAlias = "filter";
				linkFilter.LinkCriteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, tableName.ToLowerInvariant());

				query.NoLock = true;
				
				// Note: Cannot sort by aliased columns in QueryExpression, so we'll sort in memory after retrieval
				// Sort by: Mode (Sync first), then Stage, then Rank
				query.AddOrder("mode", OrderType.Ascending); // 0 = Sync, 1 = Async
				query.AddOrder("stage", OrderType.Ascending); // 10, 20, 40
				query.AddOrder("rank", OrderType.Ascending); // Execution order

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var steps = result.Entities.Select(x => new SdkMessageProcessingStep(x)).ToArray();

				// Sort in memory by: Message, Mode (already sorted), Stage (already sorted), Rank (already sorted), Plugin Type Name
				return steps
					.OrderBy(s => s.messagename) // Message name (from aliased column)
					.ThenBy(s => s.mode?.Value ?? 0) // Mode: Sync first (0), then Async (1)
					.ThenBy(s => s.stage?.Value ?? 0) // Stage: 10, 20, 40
					.ThenBy(s => s.rank ?? 0) // Rank: execution order
					.ThenBy(s => s.plugintypename) // Plugin type name (from aliased column)
					.ToArray();
			}

			public async Task<SdkMessageProcessingStep[]> GetBySolutionAsync(IOrganizationServiceAsync2 crm, Guid solutionId, bool includeInternalStages, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				AddStageFilter(query, includeInternalStages);

				// Join with plugintype for name
				var linkPluginType = query.AddLink("plugintype", "plugintypeid", "plugintypeid");
				linkPluginType.Columns.AddColumns("name");
				linkPluginType.EntityAlias = "pt";

				// Join with pluginassembly for assembly name
				var linkAssembly = linkPluginType.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid");
				linkAssembly.Columns.AddColumns("name", "pluginassemblyid");
				linkAssembly.EntityAlias = "pa";

				// Join with solution components to get only assemblies in the specified solution
				var linkSolutionComponent = linkAssembly.AddLink("solutioncomponent", "pluginassemblyid", "objectid");
				linkSolutionComponent.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
				linkSolutionComponent.LinkCriteria.AddCondition("componenttype", ConditionOperator.Equal, 91); // 91 = Plugin Assembly

				// Join with sdkmessage for message name
				var linkMessage = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.LeftOuter);
				linkMessage.Columns.AddColumns("name");
				linkMessage.EntityAlias = "msg";

				// Join with sdkmessagefilter for table name
				var linkFilter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter);
				linkFilter.Columns.AddColumns("primaryobjecttypecode");
				linkFilter.EntityAlias = "filter";

				// Left join to check if the step itself is in the solution
				var linkStepSolutionComponent = query.AddLink("solutioncomponent", "sdkmessageprocessingstepid", "objectid", JoinOperator.LeftOuter);
				linkStepSolutionComponent.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
				linkStepSolutionComponent.LinkCriteria.AddCondition("componenttype", ConditionOperator.Equal, 92); // 92 = Plugin Step
				linkStepSolutionComponent.Columns.AddColumns("objectid");
				linkStepSolutionComponent.EntityAlias = "stepsc";

				query.NoLock = true;
				query.AddOrder("name", OrderType.Ascending);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(x => new SdkMessageProcessingStep(x)).ToArray();
			}
		}
	}
}
