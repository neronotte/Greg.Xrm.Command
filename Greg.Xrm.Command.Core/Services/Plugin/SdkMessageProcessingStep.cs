using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

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
	}
}
