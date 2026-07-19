using Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	[TestClass]
	public class MultiSelectChoiceFieldValueConverterTest
	{
		private readonly MultiSelectChoiceFieldValueConverter _converter = new();

		private static MultiSelectPicklistAttributeMetadata BuildMultiSelectWithOptions(params (int value, string label)[] options)
		{
			var optionsList = new OptionMetadataCollection();
			foreach (var (value, label) in options)
			{
				optionsList.Add(new OptionMetadata(new Label(label, 1033), value));
			}
			var optionSet = new OptionSetMetadata(optionsList);

			var meta = new MultiSelectPicklistAttributeMetadata();
			((EnumAttributeMetadata)meta).OptionSet = optionSet;
			return meta;
		}

		[TestMethod]
		public void Convert_WithCommaSeparatedIntegers_ShouldReturnOptionSetValueCollection()
		{
			var meta = BuildMultiSelectWithOptions((1, "Red"), (2, "Blue"), (3, "Green"));
			var result = _converter.Convert("1,2,3", meta, "tags");

			Assert.IsInstanceOfType(result, typeof(OptionSetValueCollection));
			var collection = (OptionSetValueCollection)result!;
			Assert.AreEqual(3, collection.Count);
		}

		[TestMethod]
		public void Convert_WithCommaSeparatedLabels_ShouldReturnMatchingValues()
		{
			var meta = BuildMultiSelectWithOptions((1, "Red"), (2, "Blue"), (3, "Green"));
			var result = _converter.Convert("Red,Blue", meta, "colors");

			Assert.IsInstanceOfType(result, typeof(OptionSetValueCollection));
			var collection = (OptionSetValueCollection)result!;
			Assert.AreEqual(2, collection.Count);
			Assert.IsTrue(collection.Any(v => v.Value == 1));
			Assert.IsTrue(collection.Any(v => v.Value == 2));
		}

		[TestMethod]
		public void Convert_WithListOfLong_ShouldReturnOptionSetValueCollection()
		{
			var meta = BuildMultiSelectWithOptions((1, "Red"), (2, "Blue"), (3, "Green"));
			var list = new List<object?> { 1L, 2L, 3L };
			var result = _converter.Convert(list, meta, "tags");

			Assert.IsInstanceOfType(result, typeof(OptionSetValueCollection));
			var collection = (OptionSetValueCollection)result!;
			Assert.AreEqual(3, collection.Count);
		}

		[TestMethod]
		public void Convert_WithListOfStrings_ShouldReturnOptionSetValueCollection()
		{
			var meta = BuildMultiSelectWithOptions((1, "Red"), (2, "Blue"), (3, "Green"));
			var list = new List<object?> { "Red", "Green" };
			var result = _converter.Convert(list, meta, "colors");

			Assert.IsInstanceOfType(result, typeof(OptionSetValueCollection));
			var collection = (OptionSetValueCollection)result!;
			Assert.AreEqual(2, collection.Count);
		}

		[TestMethod]
		public void Convert_NullValue_ShouldReturnNull()
		{
			var meta = new MultiSelectPicklistAttributeMetadata();
			var result = _converter.Convert(null, meta, "tags");
			Assert.IsNull(result);
		}

		[TestMethod]
		public void Convert_WithUnsupportedType_ShouldThrowFormatException()
		{
			var meta = new MultiSelectPicklistAttributeMetadata();
			// single long (not a list, not a string) should throw
			Assert.Throws<FormatException>(
				() => _converter.Convert(42L, meta, "tags"));
		}
	}
}
