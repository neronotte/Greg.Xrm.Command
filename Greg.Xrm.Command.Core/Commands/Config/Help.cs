using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Config
{
	public class Help : NamespaceHelperBase
	{
		public Help() : base(true, "PACX configurations", "!config")
		{
		}
	}
}
