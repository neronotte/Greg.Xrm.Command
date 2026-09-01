using System.Text.Encodings.Web;
using System.Text.Json;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Workflows
{
	public class GetCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWorkflowRepository workflowRepository)

	: ICommandExecutor<GetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetCommand command, CancellationToken cancellationToken)
		{
			if (!string.IsNullOrWhiteSpace(command.OutputFile))
			{
				string? outputFolder;
				try
				{
					outputFolder = Path.GetDirectoryName(Path.GetFullPath(command.OutputFile));
				}
				catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException)
				{
					return CommandResult.Fail($"The output file path <{command.OutputFile}> is not valid: {ex.Message}");
				}

				if (!string.IsNullOrWhiteSpace(outputFolder) && !Directory.Exists(outputFolder))
				{
					return CommandResult.Fail($"The folder <{outputFolder}> does not exist.");
				}
			}

			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var searchedName = command.Name.Trim();

			IReadOnlyList<Workflow> found;
			try
			{
				output.Write($"Retrieving workflow {(command.Id.HasValue ? command.Id.ToString() : searchedName)}...");

				if (command.Id.HasValue)
				{
					var byId = await workflowRepository.GetDefinitionByIdAsync(crm, command.Id.Value);
					found = byId == null ? [] : [byId];
				}
				else
				{
					found = await workflowRepository.GetDefinitionByNameAsync(crm, searchedName, command.SolutionName?.Trim());
				}

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}

			if (found.Count == 0)
			{
				var searched = command.Id.HasValue ? $"id <{command.Id}>" : $"name <{searchedName}>";
				return CommandResult.Fail($"No workflow found with {searched}. You can use 'pacx workflow list' to see the available ones.");
			}

			// names typed into the maker portal can carry leading or trailing spaces,
			// so an exact match wins but a name that only differs by those is still found
			var matches = found;
			if (!command.Id.HasValue)
			{
				var exactMatches = found
					.Where(w => string.Equals(w.name?.Trim(), searchedName, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (exactMatches.Count > 0)
				{
					matches = exactMatches;
				}
			}

			if (matches.Count > 1)
			{
				output.WriteLine();
				output.WriteLine($"More than one workflow matches <{searchedName}>:", ConsoleColor.Yellow);
				foreach (var candidate in matches)
				{
					output.WriteLine($"  {candidate.name}", ConsoleColor.Yellow);
				}
				return CommandResult.Fail("Please provide the full name, or use the --solution option to identify the one you need.");
			}

			var workflow = matches[0];

			// modern flows keep their definition in clientdata, classic workflows in xaml
			var definition = !string.IsNullOrWhiteSpace(workflow.clientdata)
				? Prettify(workflow.clientdata)
				: workflow.xaml;

			if (string.IsNullOrWhiteSpace(definition))
			{
				return CommandResult.Fail($"The workflow <{workflow.name}> ({workflow.CategoryFormatted}) has no definition to return.");
			}

			if (string.IsNullOrWhiteSpace(command.OutputFile))
			{
				output.WriteLine();
				output.WriteLine(definition);
				return CommandResult.Success();
			}

			try
			{
				await File.WriteAllTextAsync(command.OutputFile, definition, cancellationToken);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				return CommandResult.Fail($"Unable to write the file <{command.OutputFile}>: {ex.Message}");
			}

			output.WriteLine($"Definition written to {command.OutputFile}", ConsoleColor.Green);
			return CommandResult.Success();
		}

		/// <summary>
		/// The clientdata of a flow is stored as a single line of json.
		/// Indenting it makes it readable and comparable. The relaxed encoder keeps
		/// apostrophes and non ascii characters as they are, the output is meant to
		/// be read and diffed, not to be embedded in html.
		/// </summary>
		private static readonly JsonSerializerOptions prettyOptions = new()
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		private static string Prettify(string json)
		{
			try
			{
				using var document = JsonDocument.Parse(json);
				return JsonSerializer.Serialize(document, prettyOptions);
			}
			catch (JsonException)
			{
				return json;
			}
		}
	}
}
