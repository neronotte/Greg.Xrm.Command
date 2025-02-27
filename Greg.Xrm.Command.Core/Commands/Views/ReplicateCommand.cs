using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "replicate", HelpText = "Replicates the structure (layout and sort order) of a given view.")]
	public class ReplicateCommand : ICanProvideUsageExample
	{
		[Option("name", "n", HelpText = "The name of the view to replicate")]
		[Required]
		public string ViewName { get; set; } = string.Empty;

		[Option("table", "t", HelpText = "The name of the table that contains the view. Required only if the view name is not unique in the system.")]
		public string? TableName { get; set; }

		[Option("onto", "o", HelpText = "The name of the views that should be updated with the new layout, separated by comma (,). If not specified, all saved queries except for lookup views will be updated. If * is provided as value, all views will be updated (lookup views included).")]
		public string? Onto { get; set; }



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("The current command replicates the behavior of the famous **ViewLayoutReplicator** tool from **XrmToolbox** (kudos to _Tanguy Touzard_).");

			writer.WriteParagraph("You can simply type: ");

			writer.WriteCodeBlock("pacx view replicate -n \"My View\" -t greg_table", "Powershell");

			writer.WriteLine("This will replicate the view named `My View` from the table `greg_table` to all other saved queries belonging to the same table, except for the lookup views.");
			writer.WriteLine("This default behavior is driven by the fact that changing the structure of the lookup views may generate unexpected behaviors on the lookup control.");
			writer.WriteLine();

			writer.WriteParagraph("If you want to override this default behavior you can type: ");

			writer.WriteCodeBlock("pacx view replicate -n \"My View\" -t greg_table -o *", "Powershell");

			writer.WriteLine("This will replicate the view named `My View` from the table `greg_table` to all other saved queries, including lookup views.");
			writer.WriteLine();

			writer.WriteParagraph("Seamlessly, if you want to replicate the view only onto a _specific_ view or set of views, you can type: ");

			writer.WriteCodeBlock("pacx view replicate -n \"My View\" -t greg_table -o \"View 1,View 2,View 3\"", "Powershell");

			writer.WriteLine("Separating the view names via comma (,).");
		}
	}
}
