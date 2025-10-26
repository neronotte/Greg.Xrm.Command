using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Plugin;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[Command("plugin", "update-framework-path", HelpText = "Updates the path to the .NET Framework 4.6.2 reference assemblies used for plugin assembly validation.")]
	[Alias("plugin", "ufp")]
	public class UpdateFrameworkPathCommand : IValidatableObject, ICanProvideUsageExample
	{

		[Option("path", "p", HelpText = $"The full path to the .NET Framework 4.6.2 reference assemblies. Type DEFAULT if you want to restore the setting to its original value ({Constants.DefaultFrameworkPath}).")]
		[Required]
		public string NewPath { get; set; } = string.Empty;

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (string.IsNullOrWhiteSpace(NewPath))
			{
				yield return new ValidationResult("The path cannot be empty.", [nameof(NewPath)]);
			}
			else if (!NewPath.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) && !Directory.Exists(NewPath))
			{
				yield return new ValidationResult($"The specified path '{NewPath}' does not exist.", [nameof(NewPath)]);
			}
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("The `pacx plugin push` command requires the **.NET Framework 4.6.2 reference assemblies** to validate plugin assemblies before they are pushed to Dataverse.");
			writer.WriteLine();
			writer.WriteLine($"By default, **.NET Framework 4.6.2 reference assemblies** are registered at `{Constants.DefaultFrameworkPath}`.")
				.WriteLine("If you have a custom installation of the .NET Framework, you can use this command to set the correct path.");
		}
	}
}
