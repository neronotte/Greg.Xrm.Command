using Greg.Xrm.Command.Commands.Solution.Extensions;
using Greg.Xrm.Command.Commands.Solution.Model;
using Greg.Xrm.Command.Services.Output;
using System.Text.RegularExpressions;

namespace Greg.Xrm.Command.Commands.Solution.Writers
{
	public abstract class WriteConstantsToFileBase
	{
		public abstract string FilePath { get; set; }

		private List<string> FileRows { get; set; } = new List<string>();

		protected IOutput? Output { get; set; }

		public abstract List<EntityMetadataManager> EntitiesData { get; set; }

		public void WriteGlobalElements(List<GlobalOptionSetsMetadataManager> globalMetadata, string tabulation)
		{
			foreach (var optionSetMetadata in globalMetadata)
			{
				WriteGlobalOptionSetConstantClassHeader(optionSetMetadata, tabulation);
				WriteRows(optionSetMetadata.PickListValues.ToList(), tabulation, optionSetMetadata == globalMetadata.Last());
			}
		}

		protected void WriteLine(string text)
		{
			FileRows.Add(text);
		}

		public int WriteEntityConstants(string fileType, string tabulation)
		{
			int count = 0;
			foreach (var entityConstants in EntitiesData)
			{
				Output?.WriteLine($"  Writing file {entityConstants.EntityLogicalName}.{fileType}", ConsoleColor.Gray);
				FileRows = new List<string>();
				WriteFileNameSpace();
				WriteEntityConstantClassHeader(entityConstants);
				if (entityConstants.Attributes.Count > 0)
				{
					entityConstants.Attributes = entityConstants.Attributes.OrderBy(a => a.LogicalNameConstant).ToList();
					var lastAttribute = entityConstants.GetLastAttribute();
					WriteCurrentEntityConstants(entityConstants, lastAttribute, tabulation);
				}
				WriteEndCode();
				CommitToFileAndRestart(FilePath + "/" + entityConstants.EntityLogicalName + "." + fileType);
				count++;
			}
			return count;
		}

		public static Dictionary<int, string> StatusReasonElementsWithStateValues(
			AttributeMetadataManager statusReasonAttribute,
			AttributeMetadataManager stateAttribute)
		{
			var result = new Dictionary<int, string>();
			((AttributeMetadataManagerForStatusReason)statusReasonAttribute).StatusReasonValues.ForEach(value =>
			{
				var stateName = ((AttributeMetadataManagerForStatus)stateAttribute).StatusValues
					.Where(s => s.Key == value.Item2)
					.FirstOrDefault().Value
					?.RemoveDiacritics() ?? string.Empty;
				result.Add(value.Item1, $"{value.Item3}_State{stateName}");
			});
			return result;
		}

		public List<KeyValuePair<int, string>> FormatRowValues(List<KeyValuePair<int, string>> elements)
		{
			var result = new List<KeyValuePair<int, string>>();
			elements.ForEach(elem =>
			{
				var str1 = elem.Value.RemoveDiacritics().Replace(" ", "").RemoveSpecialCharacters();
				if (Regex.IsMatch(str1, @"^\d"))
					str1 = str1.Insert(0, "_");

				if (elements.Where(e => e.Value == elem.Value).Count() > 1)
					str1 = RecursiveGetValueFormatted(result, str1, 1);

				var str2 = FormatValueForKeywords(str1, elem.Key);
				result.Add(new KeyValuePair<int, string>(elem.Key, str2));
			});
			return result.OrderBy(c => c.Value).ToList();
		}

		private string RecursiveGetValueFormatted(List<KeyValuePair<int, string>> result, string valueFormatted, int index)
		{
			var key = $"{valueFormatted}{index}";
			return result.Where(c => c.Value == key).Count() > 0
				? RecursiveGetValueFormatted(result, valueFormatted, index + 1)
				: key;
		}

		internal void WriteCurrentEntityConstants(EntityMetadataManager entityConstants, string lastAttribute, string tabulation)
		{
			if (entityConstants.Attributes.Count == 0) return;

			WriteAttributes(entityConstants, lastAttribute);

			if (entityConstants.StatusAttribute != null)
			{
				WriteStateValues(entityConstants.StatusAttribute, tabulation);
				var source = StatusReasonElementsWithStateValues(entityConstants.StatusReasonAttribute!, entityConstants.StatusAttribute);
				WriteAttributeHeader(entityConstants.StatusReasonAttribute!);
				WriteRows(source.ToList(), tabulation, entityConstants.OptionSetAttributes.Count == 0);
			}

			entityConstants.OptionSetAttributes.ForEach(opt =>
			{
				if (opt.PicklistValues.Count <= 0) return;
				WriteAttributeHeader(opt);
				WriteRows(opt.PicklistValues.ToList(), tabulation, opt == entityConstants.OptionSetAttributes.Last());
			});
		}

		public void WriteStateValues(AttributeMetadataManager stateAttribute, string tabulation)
		{
			WriteAttributeHeader(stateAttribute);
			WriteRows(((AttributeMetadataManagerForStatus)stateAttribute).StatusValues.ToList(), tabulation, null);
		}

		public void CommitToFileAndRestart(string fileName)
		{
			using (var w = new StreamWriter(File.Create(fileName)))
				FileRows.ForEach(row => w.WriteLine(row));
			FileRows.Clear();
		}

		public virtual void WriteEntityConstantClassHeader(EntityMetadataManager entityConstants) { }
		public virtual void WriteGlobalOptionSetConstantClassHeader(GlobalOptionSetsMetadataManager optionSetMetadata, string tabulation) { }
		public virtual void WriteAttributes(EntityMetadataManager entityConstants, string lastAttribute) { }
		public virtual void WriteAttributeHeader(AttributeMetadataManager attr) { }
		public virtual void WriteRows(List<KeyValuePair<int, string>> elements, string tabulation = "\t\t", bool? isLastAttribute = null) { }
		public virtual void WriteFileNameSpace() { }
		public virtual void WriteEndCode() { }
		public virtual string FormatValueForKeywords(string label, int value) =>
			string.IsNullOrWhiteSpace(label) ? "Value_" + value : label;
	}
}
