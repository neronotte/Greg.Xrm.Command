using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.CustomApi.Model
{
	public class CustomApiResponseProperty : EntityWrapper
	{
		protected CustomApiResponseProperty(Entity entity) : base(entity) { }

		public CustomApiResponseProperty() : base("customapiresponseproperty") { }

		public string? uniquename
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string? displayname
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string? description
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public OptionSetValue? type
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}

		public EntityReference? customapiid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
	}
}
