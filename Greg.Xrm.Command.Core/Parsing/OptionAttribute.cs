namespace Greg.Xrm.Command
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class OptionAttribute : Attribute
	{
		public OptionAttribute(string longName)
		{
			this.LongName = longName;
			this.ShortName = null;
			this.IsRequired = false;
			this.HelpText = null;
			this.DefaultValue = null;
		}
		public OptionAttribute(string longName, string shortName)
		{
			this.LongName = longName;
			this.ShortName = shortName;
			this.IsRequired = false;
			this.HelpText = null;
			this.DefaultValue = null;
		}

		public OptionAttribute(string longName, string shortName, bool isRequired, string helpText, object? defaultValue = null)
        {
			this.LongName = longName;
			this.ShortName = shortName;
			this.IsRequired = isRequired;
			this.HelpText = helpText;
			this.DefaultValue = defaultValue;
		}

		public string LongName { get; }
		public string? ShortName { get; set; }
		public bool IsRequired { get; set; }
		public string? HelpText { get; set; }
		public object? DefaultValue { get; set; }
	}
}
