using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Parsing.Attributes;

namespace Greg.Xrm.Command.Commands.Config
{
    public class Help : NamespaceHelperBase
	{
		public Help() : base(true, "PACX configurations", "!config")
		{
		}
	}
}
