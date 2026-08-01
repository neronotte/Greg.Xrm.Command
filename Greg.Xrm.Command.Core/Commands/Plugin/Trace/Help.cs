using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Plugin.Trace
{
	public class Help : NamespaceHelperBase
	{
		public Help() : base("Read plugin trace logs", "plugin", "trace")
		{
		}
	}
}
