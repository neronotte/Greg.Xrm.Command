using Greg.Xrm.Command.Commands.Script.Service;

namespace Greg.Xrm.Command.Commands.Script
{
    public class ScriptSolutionCommandExecutor : ICommandExecutor<ScriptSolutionCommand>
    {
        private readonly IScriptExtractionService extractionService;
        public ScriptSolutionCommandExecutor(IScriptExtractionService extractionService)
        {
            this.extractionService = extractionService;
        }
        public Task<CommandResult> ExecuteAsync(ScriptSolutionCommand command, CancellationToken cancellationToken)
        {
            return extractionService.ExtractSolutionAsync(command, cancellationToken);
        }
    }
}
