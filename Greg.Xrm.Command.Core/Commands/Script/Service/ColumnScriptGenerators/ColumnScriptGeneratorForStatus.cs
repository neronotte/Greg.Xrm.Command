using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForStatus(StatusAttributeMetadata status, StateAttributeMetadata state) : IColumnScriptGenerator
	{
		public void GenerateScript(StringBuilder script)
		{
			var stateOptions = state.OptionSet.Options.OfType<StateOptionMetadata>().OrderBy(x => x.Value).ToList();

			var statusOptions = status.OptionSet.Options.OfType<StatusOptionMetadata>().OrderBy(x => x.Value).ToList();


			foreach (var stateOption in stateOptions)
			{
				script.Append("## STATE: ");
				script.Append(stateOption.Label.UserLocalizedLabel.Label);
				script.Append(" (");
				script.Append(stateOption.Value);
				script.Append(")");
				script.AppendLine();

				foreach (var child in statusOptions.Where(x => x.State == stateOption.Value))
				{
					var operation = (child.Value == stateOption.DefaultStatus) ? "update" : "add";
					
					script.Append($"pacx optionset {operation} --table ");
					script.Append(state.EntityLogicalName);
					script.Append(" --column ");
					script.Append(status.LogicalName);
					script.Append(" --stateCode ");
					script.Append(child.State);
					script.Append(" --value ");
					script.Append(child.Value);
					script.Append(" --label ");
					script.Append(child.Label.UserLocalizedLabel.Label);

					if (!string.IsNullOrWhiteSpace(child.Color))
					{
						script.Append(" --color ");
						script.Append(child.Color);
					}
					script.AppendLine();
				}

			}
		}
	}
}
