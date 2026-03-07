namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "optionset", HelpText = "Creates an optionset (picklist) column.")]
	[Alias("column", "add", "picklist")]
	public class CreatePicklistCommand : BaseCreateCommand
	{
		[Option("globalOptionSetName", "gon",
			HelpText = "For Picklist type columns that must be tied to a global option set,\nprovides the name of the global option set.",
			Order = 10)]
		public string? GlobalOptionSetName { get; set; }


		[Option("options", "o", 
			HelpText = "The list of options for the attribute, as a single string separated by comma (,) or semicolon (;) or pipe.\nYou can pass also values separating using syntax \"label1:value1,label2:value2\"\nIf not provided, values will be automatically generated",
			Order =20
		)]
		public string? Options { get; internal set; }


		[Option("defaultValue", "dv", HelpText = "For Picklist type columns indicates the default value for the column. You can provide the name or the value. If not provided, is automatically evaluated by the system.",
			Order = 30)]
		public string? DefaultFormValue { get; set; }

		[Option("multiselect", "m", 
			HelpText = "Indicates whether the attribute is a multi-select picklist (default: false).", 
			DefaultValue = false, 
			Order = 40)]
		public bool Multiselect { get; set; } = false;
	}
}
