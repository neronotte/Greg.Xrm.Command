using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Settings
{
	[Command("settings", "export", HelpText = "List settings defined for the current environment")]
	[Alias("settings", "list")]
	[Alias("settings", "ls")]
	public class ExportCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("origin", "o", "Indicates if the list of settings to retrieve is the whole list of settings, or just the settings in the specified solution.", DefaultValue = Origin.Solution)]
		public Origin Origin { get; set; } = Origin.Solution;

		[Option("filter", "f", "Indicates if the list of settings to retrieve should include all settings, or only visible settings.", DefaultValue = Which.Visible)]
		public Which Filter { get; set; } = Which.Visible;


		[Option("format", "fmt", "The format of the output. Default is Text. Use Json to get the output in JSON format.")]
		public Format Format { get; set; } = Format.Text;


		[Option("output", "out", "If the format specified is Json or Excel, this is the name of the file where the output will be saved. For Excel files is mandatory. For JSON, if not specified, the output will be written only to the console.")]
		public string? OutputFileName { get; set; }

		[Option("run", "r", HelpText = "Allows to specify whether the output file should be automatically opened or not.", DefaultValue = false)]
		public bool AutoRun { get; set; } = false;


		[Option("solution", "s", HelpText = "The solution to get the settings from. If not specified, the default solution is considered.")]
		public string? SolutionName { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (Format == Format.Excel && string.IsNullOrWhiteSpace(OutputFileName))
			{
				yield return new ValidationResult("When the format is Excel, the output file name must be specified.", new[] { nameof(OutputFileName) });
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("The current command will list all the settings in a given solution, or in the current environment.");
			writer.WriteLine("It can output the settings value to console in a tabular format (`-fmt Text`), to console and/or file in JSON format (`-fmt Json`) or in an excel file (`-fmt Excel`).");
			writer.WriteLine();
			writer.WriteLine("**Easter egg**: if you put the {version} token in the output file name, it will be replaced by the current timestamp. E.g.");
			writer.WriteLine();
			writer.WriteCodeBlock("pacx settings export -fmt Excel -out settings_{version}.xlsx");
			writer.WriteLine();
			writer.WriteLine("Will generate an Excel file named `settings_2024.05.05.12.34.56.xlsx`.");
			writer.WriteLine();
			writer.WriteParagraph("In both Excel and Json format, the output is deterministic: settings are sorted by name, and the values_per_app are sorted by app name. This means that the output structure will remain fixed, to improve versioning and simplify comparison between different environments.");
		}
	}


	public enum Origin
	{
		Solution,
		All
	}

	public enum Which
	{
		Visible,
		All
	}

	public enum Format
	{
		Text,
		Json,
		Excel
	}
}
