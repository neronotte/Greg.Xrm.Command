using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Greg.Xrm.Command.Plugin.Automation.Model
{
	[EntityLogicalName("flowrun")]
	public class FlowRun : Entity
	{
		public FlowRun() : base("flowrun") { }

		[AttributeLogicalName("flowrunid")]
		public override Guid Id { get => base.Id; set => base.Id = value; }

		[AttributeLogicalName("name")]
		public string? Name { get => GetAttributeValue<string>("name"); set => SetAttributeValue("name", value); }

		[AttributeLogicalName("status")]
		public string? Status { get => GetAttributeValue<string>("status"); set => SetAttributeValue("status", value); }

		[AttributeLogicalName("starttime")]
		public DateTime? StartTime { get => GetAttributeValue<DateTime?>("starttime"); set => SetAttributeValue("starttime", value); }

		[AttributeLogicalName("endtime")]
		public DateTime? EndTime { get => GetAttributeValue<DateTime?>("endtime"); set => SetAttributeValue("endtime", value); }
	}
}
