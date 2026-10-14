using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	public class Help : NamespaceHelperBase
	{
		public Help() : base("List Dataverse teams and inspect their security roles", "security", "teams") { }
	}
}