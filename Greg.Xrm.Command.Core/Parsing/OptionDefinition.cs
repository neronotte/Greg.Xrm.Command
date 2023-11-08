using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
	public class OptionDefinition
	{
        public OptionDefinition(PropertyInfo property, OptionAttribute option)
        {
			this.Property = property;
			this.Option = option;
		}

        public PropertyInfo Property { get; }
		public OptionAttribute Option { get; }
	}
}
