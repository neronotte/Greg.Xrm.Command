namespace Greg.Xrm.Command
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class OptionAttribute : Attribute
	{
		public OptionAttribute(string longName)
		{
			this.LongName = longName;
			this.ShortName = null;
			this.HelpText = null;
			this.DefaultValue = null;
			this.SuppressValuesHelp = false;
		}
		public OptionAttribute(string longName, string shortName)
		{
			this.LongName = longName;
			this.ShortName = shortName;
			this.HelpText = null;
			this.DefaultValue = null;
			this.SuppressValuesHelp = false;
		}

		public OptionAttribute(string longName, string shortName, string helpText, object? defaultValue = null)
        {
			this.LongName = longName;
			this.ShortName = shortName;
			this.HelpText = helpText;
			this.DefaultValue = defaultValue;
			this.SuppressValuesHelp = false;
		}

		/// <summary>
		/// Gets or sets the name of the argument in the --long-name format. 
		/// This is the primary identifier for the option and 
		/// is used in the command-line interface to specify the option.
		/// </summary>
		public string LongName { get; }

		/// <summary>
		/// Gets or sets the short name associated with the option.
		/// </summary>
		/// <remarks>This property can be null, indicating that no short name has been assigned.</remarks>
		public string? ShortName { get; set; }

		/// <summary>
		/// Gets or sets the help text associated with the option.
		/// </summary>
		public string? HelpText { get; set; }

		/// <summary>
		/// Gets or sets the default value for the option. 
		/// This value will be used if the option is not specified in the command-line arguments.
		/// </summary>
		public object? DefaultValue { get; set; }

		/// <summary>
		/// For options associated to arguments of type Enum, gets or sets a value indicating 
		/// whether the possible values of the enum should be suppressed in the help text.
		/// </summary>
		public bool SuppressValuesHelp { get; set; }

		/// <summary>
		/// Order in which the options should be displayed in both interactive experience and help text. 
		/// Options with lower order values will be displayed before those with higher values. 
		/// The default order is 0, and options with the same order value will be displayed in the order they are defined in the class.
		/// </summary>
		public int Order { get; set; } = 0;
	}
}
