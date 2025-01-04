using Greg.Xrm.Command.Commands.WebResources.Templates;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class JsResetTemplateCommandExecutor : ICommandExecutor<JsResetTemplateCommand>
	{
		private readonly IOutput output;
		private readonly IJsTemplateManager jsTemplateManager;

		public JsResetTemplateCommandExecutor(
			IOutput output,
			IJsTemplateManager jsTemplateManager)
		{
			this.output = output;
			this.jsTemplateManager = jsTemplateManager;
		}

		public async Task<CommandResult> ExecuteAsync(JsResetTemplateCommand command, CancellationToken cancellationToken)
		{
			this.output.Write("Resetting default template...");
			var global = command.Type == JavascriptWebResourceType.Ribbon && !command.ForTable;
			await this.jsTemplateManager.ResetTemplateForAsync(command.Type, global);
			this.output.WriteLine("Done", ConsoleColor.Green);

			return CommandResult.Success();
		}
	}
}
