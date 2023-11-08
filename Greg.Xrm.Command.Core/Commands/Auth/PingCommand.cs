using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Auth
{
    [Command("auth", "ping", HelpText = "Tests the connection to the Dataverse environment currently selected")]
    public class PingCommand
    {
    }
}
