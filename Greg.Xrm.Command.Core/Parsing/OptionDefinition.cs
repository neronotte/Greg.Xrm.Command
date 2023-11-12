using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
	public class OptionDefinition
	{
        public OptionDefinition(PropertyInfo property, OptionAttribute option, bool isRequired)
        {
			this.Property = property;
			this.Option = option;
			this.IsRequired = isRequired;
		}

        public PropertyInfo Property { get; }
		public OptionAttribute Option { get; }

		public bool IsRequired { get; }
	}
}
