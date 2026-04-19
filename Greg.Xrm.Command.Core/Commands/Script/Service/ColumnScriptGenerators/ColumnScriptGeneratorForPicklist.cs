using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForPicklist(EnumAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateBase(script, "optionset");

			if (field.OptionSet.IsGlobal ?? false)
			{
				script.Append($" --globalOptionSetName \"{field.OptionSet.Name}\"");
			}
			else
			{
				var options = field.OptionSet.Options.Select(o => $"{o.Label.UserLocalizedLabel.Label}:{o.Value}");
				script.Append($" --options \"{string.Join(",", options)}\"");
			}

			if (field.DefaultFormValue != null)
			{
				script.Append($" --defaultValue {field.DefaultFormValue}");
			}
		}
	}
}
