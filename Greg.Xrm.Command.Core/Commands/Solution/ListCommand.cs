
namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "list", HelpText = "Lists all solutions in the current environment.")]
	public class ListCommand
	{
		[Option("type", "t", HelpText = "Type of solutions to list (Managed, Unmanaged, Both).", DefaultValue = SolutionType.Both)]
		public SolutionType Type { get; set; } = SolutionType.Both;

		[Option("hidden", "hid", HelpText = "Shows all solutions, including the ones that are not visible via make.powerapps.com UI.")]
		public bool Hidden { get; set; } = false;

		[Option("format", "f", HelpText = "Chooses how to generate the output.", DefaultValue = OutputFormat.TableCompact)]
		public OutputFormat Format { get; set; } = OutputFormat.TableCompact;


		[Option("orderby", "o", HelpText = "Order of the output.", DefaultValue = OutputOrder.Name)]
		public OutputOrder OrderBy { get; set; } = OutputOrder.Name;



		public enum SolutionType
		{
			Managed,
			Unmanaged,
			Both
		}

		public enum OutputFormat
		{
			Table,
			TableCompact,
			Json
		}

		public enum OutputOrder 
		{
			Name,
			CreatedOn,
			ModifiedOn,
			Type,
		}
	}
}
