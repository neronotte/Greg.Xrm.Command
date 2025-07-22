using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Script
{
    public class Help : NamespaceHelperBase
    {
        public Help() : base("Commands to generate PACX scripts and OptionSet CSV from Dataverse tables.", "script")
        {
        }
    }
}
