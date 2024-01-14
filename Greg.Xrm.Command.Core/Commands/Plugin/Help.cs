using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Plugin
{
    public class Help : NamespaceHelperBase
	{
		public Help() : base("Allows adding, listing, updating and removing PACX plugins", "plugin")
		{
		}
	}
}
