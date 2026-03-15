using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin.Type
{
	[Command("plugin", "type", "unregister", HelpText = "Removes a plugin type registration. If the type has registered steps, the command will list them and stop unless --force is specified.")]
	[Alias("plugin", "type", "remove")]
	[Alias("plugin", "type", "del")]
	public class UnregisterCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("id", "id", Order = 0, HelpText = "The unique identifier of the plugin type to be removed.")]
		public Guid? TypeId { get; set; }

		[Option("name", "n", Order = 1, HelpText = "Name (or partial name) of the plugin type to be removed. Resolved via fuzzy search.")]
		public string? PluginTypeName { get; set; }

		[Option("force", "f", Order = 2, HelpText = "When specified, all registered steps (and their images) belonging to this plugin type are deleted before removing the type itself.", DefaultValue = false)]
		public bool Force { get; set; } = false;


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (TypeId == null && string.IsNullOrWhiteSpace(PluginTypeName))
			{
				yield return new ValidationResult(
					"Either --id or --name must be provided.",
					[nameof(TypeId), nameof(PluginTypeName)]);
			}

			if (TypeId != null && !string.IsNullOrWhiteSpace(PluginTypeName))
			{
				yield return new ValidationResult(
					"Cannot specify both --id and --name. Please provide only one of these parameters.",
					[nameof(TypeId), nameof(PluginTypeName)]);
			}
		}


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("To remove a plugin type, you can identify it by its unique identifier or by name:");

			writer.WriteCodeBlock(@"# By unique identifier
pacx plugin type unregister --id 3fa85f64-5717-4562-b3fc-2c963f66afa6", "Powershell");
			writer.WriteLine();

			writer.WriteCodeBlock(@"# By name (fuzzy match)
pacx plugin type unregister --name MyProject.Plugins.AccountPlugin", "Powershell");
			writer.WriteLine();

			writer.WriteParagraph("If the plugin type has registered steps the command will list them and stop. Use `--force` to delete the steps automatically before removing the type:");

			writer.WriteCodeBlock(@"# Force removal including all registered steps
pacx plugin type unregister --name MyProject.Plugins.AccountPlugin --force", "Powershell");
		}
	}
}
