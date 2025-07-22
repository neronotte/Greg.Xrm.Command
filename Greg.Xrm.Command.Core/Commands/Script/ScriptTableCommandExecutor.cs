using Greg.Xrm.Command.Commands.Script.Service;

namespace Greg.Xrm.Command.Commands.Script
{
    public class ScriptTableCommandExecutor : ICommandExecutor<ScriptTableCommand>
    {
        private readonly IScriptExtractionService extractionService;
        public ScriptTableCommandExecutor(IScriptExtractionService extractionService)
        {
            this.extractionService = extractionService;
        }
        public Task<CommandResult> ExecuteAsync(ScriptTableCommand command, CancellationToken cancellationToken)
        {
            return extractionService.ExtractTableAsync(command, cancellationToken);
        }
    }
}
