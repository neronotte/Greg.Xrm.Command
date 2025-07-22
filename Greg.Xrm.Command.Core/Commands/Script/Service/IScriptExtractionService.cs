
namespace Greg.Xrm.Command.Commands.Script.Service
{
    public interface IScriptExtractionService
    {
        Task<CommandResult> ExtractAllAsync(ScriptAllCommand command, CancellationToken cancellationToken);
        Task<CommandResult> ExtractSolutionAsync(ScriptSolutionCommand command, CancellationToken cancellationToken);
        Task<CommandResult> ExtractTableAsync(ScriptTableCommand command, CancellationToken cancellationToken);
    }
}