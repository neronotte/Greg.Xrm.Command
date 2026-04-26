using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Column.Conventions
{
	public class ShowCommandExecutor(
		IOutput output,
		ISettingsRepository settingsRepository
	) : ICommandExecutor<ShowCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ShowCommand command, CancellationToken cancellationToken)
		{
			output.Write("Retrieving column naming conventions...");
			ColumnConventions conventions;
			try
			{
				conventions = await settingsRepository.GetAsync<ColumnConventions>(ColumnConventions.StorageKey) ?? new ColumnConventions();
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			var convetionsList = new List<Convention>
			{
				new(nameof(conventions.SimpleOptionSetSuffix).SplitNameInPartsByCapitalLetters(), conventions.SimpleOptionSetSuffix),
				new(nameof(conventions.MultiselectOptionSetSuffix).SplitNameInPartsByCapitalLetters(), conventions.MultiselectOptionSetSuffix),
				new(nameof(conventions.Casing).SplitNameInPartsByCapitalLetters(), conventions.Casing.ToString())
			};

			output.WriteTable(convetionsList, () => ["Convention", "Value"], c => [c.key, c.value]);

			return CommandResult.Success();
		}


		record Convention(string key, string value);
	}
}
