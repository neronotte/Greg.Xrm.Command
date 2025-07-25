using Greg.Xrm.Command.Commands.Script.Service;

namespace Greg.Xrm.Command.Commands.Script
{
    public class ScriptAllCommandExecutor : ICommandExecutor<ScriptAllCommand>
    {
        private readonly IScriptExtractionService extractionService;
        public ScriptAllCommandExecutor(IScriptExtractionService extractionService)
        {
            this.extractionService = extractionService;
        }
        public Task<CommandResult> ExecuteAsync(ScriptAllCommand command, CancellationToken cancellationToken)
        {
            return extractionService.ExtractAllAsync(command, cancellationToken);
        }
    }
}
