using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
    [Command("solution", "setDefault", HelpText = "Sets the default solution for the current environment")]
	public class SetDefaultCommand : ICanProvideUsageExample
	{
		[Option("name", "un", HelpText = "The unique name of the solution to set as default")]
		[Required]
        public string? SolutionUniqueName { get; set; }



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.Write("The default solution is the solution that will be used by commands such as ");
			writer.WriteCode("pacx table create");
			writer.Write(" or ");
			writer.WriteCode("pacx column create");
			writer.Write(" to store the applied customizations, when a solution uniquename is not been indicated in the proper command arguments.");
			writer.WriteLine();
			writer.WriteLine();
			writer.WriteParagraph("Example:");
			writer.WriteCodeBlock("pacx solution setDefault -n my_solution_uniquename", "Command");
		}
	}
}
