using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Commands.Script.Service;

namespace Greg.Xrm.Command.Commands.Script
{
    public class ScriptAllCommandExecutor : ICommandExecutor<ScriptAllCommand>
    {
        private readonly IScriptExtractionService extractionService;
        public ScriptAllCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository, IScriptExtractionService extractionService)
        {
            this.extractionService = extractionService;
        }
        public Task<CommandResult> ExecuteAsync(ScriptAllCommand command, CancellationToken cancellationToken)
        {
            return extractionService.ExtractAllAsync(command, cancellationToken);
        }
    }
}
