using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class Help : NamespaceHelperBase
	{
		public Help() : base("List and inspect Dataverse security roles", "security", "roles")
		{
		}
	}
}
