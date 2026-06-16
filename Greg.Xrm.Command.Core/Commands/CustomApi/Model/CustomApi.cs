using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.CustomApi.Model
{
	public class CustomApi : EntityWrapper
	{
		protected CustomApi(Entity entity) : base(entity) { }

		public CustomApi() : base("customapi") { }

		public CustomApi(Guid id) : base(new Entity("customapi", id)) { }

		public string? name
		{
			get => Get<string>();
			set => SetValue(value);
		}

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

		public OptionSetValue? bindingtype
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}

		public bool isfunction
		{
			get => Get<bool>();
			set => SetValue(value);
		}

		public bool isprivate
		{
			get => Get<bool>();
			set => SetValue(value);
		}

		public OptionSetValue? allowedcustomprocessingsteptype
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}

		public string? executeprivilegename
		{
			get => Get<string>();
			set => SetValue(value);
		}

			public string? boundentitylogicalname
			{
				get => Get<string>();
				set => SetValue(value);
			}

			public EntityReference? plugintypeid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
	}
}
