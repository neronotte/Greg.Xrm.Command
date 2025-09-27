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

			public async Task<SdkMessageProcessingStep[]> GetByKeyAsync(IOrganizationServiceAsync2 crm, PluginType pluginType, string messageName, string? primaryEntityName, PluginRegistrationToolkit.Stage stage)
			{
				var query = new QueryExpression("sdkmessageprocessingstep");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, pluginType.Id);
				query.Criteria.AddCondition("stage", ConditionOperator.Equal, (int)stage);

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
		}
	}
}
