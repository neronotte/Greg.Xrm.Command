using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Commands.Script.Service;

namespace Greg.Xrm.Command.Commands.Script
{
    public class ScriptTableCommandExecutor : ICommandExecutor<ScriptTableCommand>
    {
        private readonly IScriptExtractionService extractionService;
        public ScriptTableCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository, IScriptExtractionService extractionService)
        {
            this.extractionService = extractionService;
        }
        public Task<CommandResult> ExecuteAsync(ScriptTableCommand command, CancellationToken cancellationToken)
        {
            return extractionService.ExtractTableAsync(command, cancellationToken);
        }
    }
}
