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

		public string LongName { get; }
		public string? ShortName { get; set; }
		public string? HelpText { get; set; }
		public object? DefaultValue { get; set; }

		public bool SuppressValuesHelp { get; set; }
	}
}
