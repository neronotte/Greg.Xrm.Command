using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	public class Help : NamespaceHelperBase
	{
		public Help() : base("List and inspect Dataverse system users", "security", "users")
		{
		}
	}
}
