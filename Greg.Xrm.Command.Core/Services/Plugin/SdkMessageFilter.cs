using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Services.Plugin
{
	public class SdkMessageFilter : EntityWrapper
	{
		public SdkMessageFilter(Entity entity) : base(entity)
		{
		}

		public string primaryobjecttypecode
		{
			get => Get<string>();
			set => SetValue(value);
		}
	}
}
