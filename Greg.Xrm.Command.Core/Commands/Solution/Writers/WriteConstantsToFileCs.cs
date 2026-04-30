using Greg.Xrm.Command.Commands.Solution.Model;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Solution.Writers
{
	public class WriteConstantsToFileCs : WriteConstantsToFileBase
	{
		public override string FilePath { get; set; }
		public override List<EntityMetadataManager> EntitiesData { get; set; }

		private bool ExtractTypes { get; }
		private bool ExtractDescriptions { get; }
		private EntityMetadataManager? ActivityPointerMetadata { get; }
		private string CurrentNamespace { get; }
		private List<GlobalOptionSetsMetadataManager> GlobalOptionSetsMetadata { get; }
		private List<GlobalOptionSetsMetadataManager> GlobalBooleanOptionSetsMetadata { get; }

		public WriteConstantsToFileCs(
			string filePath,
			string currentNamespace,
			List<EntityMetadataManager> entitiesData,
			EntityMetadataManager? activityPointerMetadata,
			List<GlobalOptionSetsMetadataManager> globalOptionSetsMetadata,
			List<GlobalOptionSetsMetadataManager> globalBooleanOptionSetsMetadata,
			bool extractTypes,
			bool extractDescriptions,
			IOutput? output = null)
		{
			FilePath = filePath;
			CurrentNamespace = currentNamespace;
			EntitiesData = entitiesData;
			ActivityPointerMetadata = activityPointerMetadata;
			GlobalOptionSetsMetadata = globalOptionSetsMetadata;
			GlobalBooleanOptionSetsMetadata = globalBooleanOptionSetsMetadata;
			ExtractTypes = extractTypes;
			ExtractDescriptions = extractDescriptions;
			Output = output;
		}

		public int WriteConstantsToFile()
		{
			var globalFiles = WriteGlobalOptionSetConstants();
			return globalFiles + WriteEntityConstantsClass();
		}

		private int WriteGlobalOptionSetConstants()
		{
			Output?.WriteLine("  Writing C# global option set constants...", ConsoleColor.Gray);
			WriteLine($"namespace {CurrentNamespace}{Environment.NewLine}{{");
			WriteLine($"\tpublic static class GlobalOptionSetConstants{Environment.NewLine}\t{{");
			WriteGlobalElements(GlobalOptionSetsMetadata, "\t\t");
			WriteLine($"\t}}{Environment.NewLine}}}");
			CommitToFileAndRestart(FilePath + "/GlobalOptionSetConstants.cs");

			WriteLine($"namespace {CurrentNamespace}{Environment.NewLine}{{");
			WriteLine($"\tpublic static class GlobalBooleanConstants{Environment.NewLine}\t{{");
			WriteGlobalElements(GlobalBooleanOptionSetsMetadata, "\t\t");
			WriteLine("\t}" + Environment.NewLine + "}");
			CommitToFileAndRestart(FilePath + "/GlobalBooleanConstants.cs");

			return 2;
		}

		private int WriteEntityConstantsClass()
		{
			Output?.WriteLine("  Writing C# entity constants...", ConsoleColor.Gray);
			FilterAttributes(ActivityPointerMetadata);
			return WriteEntityConstants("cs", "\t\t");
		}

		public override void WriteFileNameSpace()
		{
			WriteLine("namespace " + CurrentNamespace + Environment.NewLine + "{");
		}

		private void FilterAttributes(EntityMetadataManager? activityPointerMetadata)
		{
			if (activityPointerMetadata == null) return;

			EntitiesData.ForEach(entityData =>
			{
				if (!entityData.IsActivity) return;

				var activityPointerAttrNames = activityPointerMetadata.Attributes
					.Select(a => a.LogicalNameConstant)
					.ToList();
				activityPointerAttrNames.Sort();

				entityData.Attributes = entityData.Attributes
					.Where(a => !activityPointerAttrNames.Contains(a.LogicalNameConstant))
					.ToList();
			});
		}

		public override void WriteGlobalOptionSetConstantClassHeader(GlobalOptionSetsMetadataManager optionSetMetadata, string tabulation)
		{
			WriteLine(Environment.NewLine + tabulation + "/// <summary>");
			WriteLine(tabulation + "/// " + optionSetMetadata.DisplayName + " constants.");
			WriteLine(tabulation + "/// </summary>");
			WriteLine(tabulation + "public enum " + optionSetMetadata.LogicalName + "Values" + Environment.NewLine + tabulation + "{");
		}

		public override void WriteEntityConstantClassHeader(EntityMetadataManager entityConstants)
		{
			WriteLine(Environment.NewLine + "\t/// <summary>");
			WriteLine("\t/// " + entityConstants.EntityDisplayName + " constants.");
			WriteLine("\t/// </summary>");
			if (entityConstants.IsActivity)
				WriteLine($"\tpublic sealed class {entityConstants.EntityLogicalName} : activitypointer{Environment.NewLine}\t{{");
			else if (entityConstants.EntityLogicalName == "EntityGenericConstants")
				WriteLine("\tpublic class " + entityConstants.EntityLogicalName + Environment.NewLine + "\t{");
			else if (entityConstants.EntityLogicalName == "activitypointer")
				WriteLine("\tpublic class " + entityConstants.EntityLogicalName + " : EntityGenericConstants" + Environment.NewLine + "\t{");
			else
				WriteLine("\tpublic sealed class " + entityConstants.EntityLogicalName + " : EntityGenericConstants" + Environment.NewLine + "\t{");

			if (string.Equals(entityConstants.EntityLogicalName, "EntityGenericConstants", StringComparison.OrdinalIgnoreCase))
				return;

			WriteLine("\t\t/// <summary>");
			WriteLine("\t\t/// " + entityConstants.EntityLogicalName);
			WriteLine("\t\t/// </summary>");
			WriteLine("\t\tpublic static string logicalName => \"" + entityConstants.EntityLogicalName + "\";" + Environment.NewLine);
			WriteLine("\t\t/// <summary>");
			WriteLine("\t\t/// " + entityConstants.EntityDisplayName);
			WriteLine("\t\t/// </summary>");
			WriteLine("\t\tpublic static string displayName => \"" + entityConstants.EntityDisplayName + "\";" + Environment.NewLine);
			WriteLine("\t\t/// <summary>");
			WriteLine("\t\t/// " + entityConstants.EntitySetName);
			WriteLine("\t\t/// </summary>");
			WriteLine("\t\tpublic static string entitySetName => \"" + entityConstants.EntitySetName + "\";" + Environment.NewLine);
		}

		public override void WriteAttributes(EntityMetadataManager manager, string lastAttribute)
		{
			foreach (var attribute in manager.Attributes)
			{
				WriteLine("\t\t/// <summary>");
				WriteLine("\t\t/// Display Name: " + attribute.DisplayNameConstant + ",");
				if (ExtractTypes)
				{
					WriteLine("\t\t/// Type: " + attribute.Type + ",");
					foreach (var line in attribute.WriteFieldInfo())
						WriteLine(line);
				}
				if (ExtractDescriptions)
					WriteLine("\t\t/// Description: " + attribute.Description);
				WriteLine("\t\t/// </summary>");
				WriteLine("\t\tpublic static string " + attribute.LogicalNameConstantLabel + " => \"" + attribute.LogicalNameConstant + "\";" + Environment.NewLine);
			}
		}

		public override void WriteAttributeHeader(AttributeMetadataManager attribute)
		{
			WriteLine(Environment.NewLine + "\t\t/// <summary>");
			WriteLine("\t\t/// Values for field " + attribute.DisplayNameConstant);
			WriteLine("\t\t/// </summary>");
			if (attribute.EntityLogicalName != "EntityGenericConstants")
			{
				switch (attribute)
				{
					case AttributeMetadataManagerForStatusReason _:
					case AttributeMetadataManagerForStatus _:
						WriteLine("\t\tpublic new enum " + attribute.LogicalNameConstant + "Values" + Environment.NewLine + "\t\t{");
						return;
				}
			}
			WriteLine("\t\tpublic enum " + attribute.LogicalNameConstant + "Values" + Environment.NewLine + "\t\t{");
		}

		public override void WriteRows(
			List<KeyValuePair<int, string>> elements,
			string tabulation = "\t\t\t",
			bool? isLastAttribute = null)
		{
			var rows = FormatRowValues(elements).Select(c => $"\t{c.Value} = {c.Key},").ToList();
			rows[rows.Count - 1] = rows[rows.Count - 1].TrimEnd(',');
			rows.Add("}");
			rows.ForEach(row => WriteLine(tabulation + row));
		}

		private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
		{
			"abstract","as","base","bool","break","byte","case","catch","char","checked",
			"class","const","continue","decimal","default","delegate","do","double","else",
			"enum","event","explicit","extern","false","finally","fixed","float","for",
			"foreach","goto","if","implicit","in","int","interface","internal","is","lock",
			"long","namespace","new","null","object","operator","out","override","params",
			"private","protected","public","readonly","ref","return","sbyte","sealed",
			"short","sizeof","stackalloc","static","string","struct","switch","this",
			"throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
			"using","virtual","void","volatile","while"
		};

		public override string FormatValueForKeywords(string label, int value)
		{
			if (string.IsNullOrWhiteSpace(label))
				return "Value_" + value;

			if (CSharpKeywords.Contains(label))
				label = "@" + label;

			return label;
		}

		public override void WriteEndCode()
		{
			WriteLine("\t};" + Environment.NewLine + "}");
		}
	}
}
