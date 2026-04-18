using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	public class Help : NamespaceHelperBase
	{
		public Help() : base("Register, unregister, enable and disable plugin steps", "plugin", "step")
		{
		}
	}
}
