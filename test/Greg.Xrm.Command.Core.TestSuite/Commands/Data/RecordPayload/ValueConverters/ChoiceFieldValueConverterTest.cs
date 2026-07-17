using Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	[TestClass]
	public class ChoiceFieldValueConverterTest
	{
		private readonly ChoiceFieldValueConverter _converter = new();

		private static PicklistAttributeMetadata BuildPicklistWithOptions(params (int value, string label)[] options)
		{
			var meta = new PicklistAttributeMetadata();
			var optionSet = new OptionSetMetadata();
			foreach (var (value, label) in options)
			{
				optionSet.Options.Add(new OptionMetadata(new Label(label, 1033), value));
			}
			typeof(PicklistAttributeMetadata)
				.GetProperty(nameof(PicklistAttributeMetadata.OptionSet))!
				.SetValue(meta, optionSet);
			return meta;
		}

		[TestMethod]
		public void Convert_WithLong_ShouldReturnOptionSetValue()
		{
			var meta = new PicklistAttributeMetadata();
			var result = _converter.Convert(1L, meta, "status");

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(1, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public void Convert_WithNumericString_ShouldReturnOptionSetValue()
		{
			var meta = new PicklistAttributeMetadata();
			var result = _converter.Convert("2", meta, "status");

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(2, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public void Convert_WithValidLabel_ShouldReturnMatchingOptionSetValue()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));
			var result = _converter.Convert("Active", meta, "status");

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(1, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public void Convert_WithLabelCaseInsensitive_ShouldWork()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));
			var result = _converter.Convert("active", meta, "status");

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(1, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public void Convert_WithInvalidLabel_ShouldThrowFormatException()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));

			var ex = Assert.Throws<FormatException>(
				() => _converter.Convert("Unknown", meta, "status"));

			Assert.IsTrue(ex.Message.Contains("Active"));
			Assert.IsTrue(ex.Message.Contains("Inactive"));
		}

		[TestMethod]
		public void Convert_NullValue_ShouldReturnNull()
		{
			var meta = new PicklistAttributeMetadata();
			var result = _converter.Convert(null, meta, "status");
			Assert.IsNull(result);
		}
	}
}
