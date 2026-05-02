using Greg.Xrm.Command.Commands.Solution.Model;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Solution.Writers
{
	public class WriteConstantsToFileJs : WriteConstantsToFileBase
	{
		public override string FilePath { get; set; }
		public override List<EntityMetadataManager> EntitiesData { get; set; }

		private string NameSpaceJsName { get; }
		private string[] JsHeaderLines { get; }
		private List<GlobalOptionSetsMetadataManager> GlobalOptionSetsMetadata { get; }
		private List<GlobalOptionSetsMetadataManager> GlobalBooleanOptionSetsMetadata { get; }

		public WriteConstantsToFileJs(
			string filePath,
			string nameSpaceJs,
			string jsHeaderLines,
			List<EntityMetadataManager> entitiesData,
			List<GlobalOptionSetsMetadataManager> globalOptionSetsMetadata,
			List<GlobalOptionSetsMetadataManager> globalBooleanOptionSetsMetadata,
			IOutput? output = null)
		{
			FilePath = filePath;
			NameSpaceJsName = nameSpaceJs;
			JsHeaderLines = jsHeaderLines.Replace("\\n", "\n").Split('\n');
			EntitiesData = entitiesData;
			GlobalOptionSetsMetadata = globalOptionSetsMetadata;
			GlobalBooleanOptionSetsMetadata = globalBooleanOptionSetsMetadata;
			Output = output;
		}

		public int WriteConstantsToFile()
		{
			Output?.WriteLine("  Writing JS entity constants...", ConsoleColor.Gray);
			var globalFiles = WriteGlobalOptionSetConstants();
			return globalFiles + WriteEntityConstants("js", "\t");
		}

		private int WriteGlobalOptionSetConstants()
		{
			Output?.WriteLine("  Writing JS global option set constants...", ConsoleColor.Gray);
			JsHeaderLines.ToList().ForEach(row => WriteLine(row));
			WriteLine(Environment.NewLine);
			WriteLine(NameSpaceJsName + ".GlobalPickListConstants = new function () {" + Environment.NewLine + "\tvar self = this;");
			WriteLine("\tself.GlobalOptionSets = {");
			WriteGlobalElements(GlobalOptionSetsMetadata, "\t\t");
			WriteLine("\t};");
			WriteLine(Environment.NewLine + "\tself.GlobalBooleans = {");
			WriteGlobalElements(GlobalBooleanOptionSetsMetadata, "\t\t");
			WriteLine("\t};" + Environment.NewLine + "};");
			CommitToFileAndRestart(FilePath + "/GlobalOptionSetsConstants.js");
			return 1;
		}

		public override void WriteGlobalOptionSetConstantClassHeader(GlobalOptionSetsMetadataManager optionSetMetadata, string tabulation)
		{
			WriteLine(Environment.NewLine + "\t\t/// " + optionSetMetadata.DisplayName + " constants.");
			WriteLine("\t\t" + optionSetMetadata.LogicalName + "Values: {");
		}

		public override void WriteEntityConstantClassHeader(EntityMetadataManager entityConstants)
		{
			JsHeaderLines.ToList().ForEach(row => WriteLine(row));
			WriteLine(Environment.NewLine + NameSpaceJsName + "." + entityConstants.EntityLogicalName + " = {");
			WriteLine("\t///" + entityConstants.EntityDisplayName + " constants.");
			if (entityConstants.EntityLogicalName == "EntityGenericConstants") return;
				WriteLine("\tlogicalName: \"" + entityConstants.EntityLogicalName + "\",");
				WriteLine("\tdisplayName: \"" + entityConstants.EntityDisplayName + "\",");
				WriteLine("\tentitySetName: \"" + entityConstants.EntitySetName + "\",");
		}

		public override void WriteAttributes(EntityMetadataManager entityConstants, string lastAttribute)
		{
			foreach (var attribute in entityConstants.Attributes)
				WriteAttribute(attribute, lastAttribute, entityConstants.OptionSetAttributes.Count > 0, entityConstants.StatusReasonAttribute);
		}

		private void WriteAttribute(
			AttributeMetadataManager attr,
			string lastAttribute,
			bool optionSetValuesInList,
			AttributeMetadataManager? statusReasonAttribute)
		{
			WriteLine("\t///" + attr.DisplayNameConstant);
			var str = "\t" + attr.LogicalNameConstant + ": \"" + attr.LogicalNameConstant + "\"";
			if (attr.LogicalNameConstant != lastAttribute | optionSetValuesInList || statusReasonAttribute != null && attr != statusReasonAttribute)
				str += ",";
			WriteLine(str);
		}

		public override void WriteRows(
			List<KeyValuePair<int, string>> elements,
			string tabulation = "\t\t",
			bool? isLastAttribute = null)
		{
			var rows = new List<string>();
			var formatted = FormatRowValues(elements);
			var closingBrace = isLastAttribute.HasValue && isLastAttribute.Value ? "}" : "},";

			formatted.ForEach(c => rows.Add($"\t{c.Value}: {c.Key},"));
			rows[rows.Count - 1] = rows.Last().Remove(rows.Last().Length - 1);
			rows.Add(closingBrace ?? string.Empty);
			rows.ForEach(row => WriteLine(tabulation + row));
		}

		public override void WriteAttributeHeader(AttributeMetadataManager attribute)
		{
			WriteLine(Environment.NewLine + "\t/// Values for field " + attribute.DisplayNameConstant);
			WriteLine("\t" + attribute.LogicalNameConstant + "Values: {");
		}

		public override void WriteEndCode()
		{
			WriteLine("};");
		}
	}
}
