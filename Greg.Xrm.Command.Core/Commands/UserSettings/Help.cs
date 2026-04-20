using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public class Help : NamespaceHelperBase
	{
		public Help() : base("Sets one or more user setting properties for the specified or currently logged-in user.", "usersettings")
		{
		}
	}
}
