using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public class SdkMessageProcessingStepImage : EntityWrapper
	{
		protected SdkMessageProcessingStepImage(Entity entity) : base(entity)
		{
		}

		public SdkMessageProcessingStepImage() : base("sdkmessageprocessingstepimage")
		{
			this.messagepropertyname = "Target";
		}

		public EntityReference? sdkmessageprocessingstepid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
		public string? messagepropertyname
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? name
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? entityalias
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public OptionSetValue? imagetype
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}

		public class Repository : ISdkMessageProcessingStepImageRepository
		{
			public async Task<SdkMessageProcessingStepImage[]> GetByStepIdAsync(IOrganizationServiceAsync2 crm, Guid stepId)
			{
				var query = new QueryExpression("sdkmessageprocessingstepimage");
				query.ColumnSet.AddColumns("sdkmessageprocessingstepid", "messagepropertyname", "name", "entityalias", "imagetype");
				query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, stepId);
				query.NoLock = true;
				var result = await crm.RetrieveMultipleAsync(query);
				return result.Entities.Select(x => new SdkMessageProcessingStepImage(x)).ToArray();
			}
		}
	}
}
