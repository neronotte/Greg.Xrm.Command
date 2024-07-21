using Greg.Xrm.Command.Commands.WebResources.Templates;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.WebResources
{
    public class JsSetTemplateCommandExecutor : ICommandExecutor<JsSetTemplateCommand>
	{
		private readonly IOutput output;
		private readonly IJsTemplateManager jsTemplateManager;

		public JsSetTemplateCommandExecutor(
			IOutput output,
			IJsTemplateManager jsTemplateManager)
        {
			this.output = output;
			this.jsTemplateManager = jsTemplateManager;
		}


        public async Task<CommandResult> ExecuteAsync(JsSetTemplateCommand command, CancellationToken cancellationToken)
		{
			string templateContent;
			try
			{
				this.output.Write("Reading template contents...");
				templateContent = await File.ReadAllTextAsync(command.FileName, cancellationToken);
				this.output.WriteLine("DONE", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail("Error while trying to read the template file contents: " + ex.Message, ex);
			}

			this.output.Write("Updating default template...");
			var global = command.Type == JavascriptWebResourceType.Ribbon && !command.ForTable;
			await this.jsTemplateManager.SetTemplateForAsync(command.Type, global, templateContent);
			this.output.WriteLine("DONE", ConsoleColor.Green);

			return CommandResult.Success();
		}
	}
}
