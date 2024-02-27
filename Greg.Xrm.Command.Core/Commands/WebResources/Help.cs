using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.WebResources
{
    public class Help : NamespaceHelperBase
	{
		public Help() : base("Commands to work with webresources", "webresources")
		{
		}
	}


	public class Help2 : NamespaceHelperBase
	{
		public Help2() : base("(Preview) Commands that can be used to create webresource files starting from templates", "webresources", "create")
		{
		}
	}
}
