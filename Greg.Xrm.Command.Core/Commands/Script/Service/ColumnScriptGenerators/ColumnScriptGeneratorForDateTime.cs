using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForDateTime(DateTimeAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateBase(script, "datetime");


			if (field.DateTimeBehavior != null)
			{
				script.Append(" --dateTimeBehavior ").Append(field.DateTimeBehavior.Value);
			}

			if (field.Format != null)
			{
				script.Append(" --dateTimeFormat ").Append(field.Format);
			}

			script.Append(" --imeMode ").Append(field.ImeMode);
		}
	}
}
