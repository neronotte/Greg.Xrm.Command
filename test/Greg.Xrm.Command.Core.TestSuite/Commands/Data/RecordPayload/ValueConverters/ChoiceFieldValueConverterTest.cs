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
		public async Task Convert_WithLong_ShouldReturnOptionSetValue()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));
			var result = await _converter.ConvertAsync(1L, meta, "status", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(1, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public async Task Convert_WithNumericString_ShouldReturnOptionSetValue()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));
			var result = await _converter.ConvertAsync("2", meta, "status", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(2, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public async Task Convert_WithUndefinedIntegerCode_ShouldThrowFormatException()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));

			var ex = await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync(3, meta, "status", CancellationToken.None));

			Assert.IsTrue(ex.Message.Contains("Valid codes are: 1, 2."));
		}

		[TestMethod]
		public async Task Convert_WithUndefinedLongCode_ShouldThrowFormatException()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));

			var ex = await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync(3L, meta, "status", CancellationToken.None));

			Assert.IsTrue(ex.Message.Contains("Valid codes are: 1, 2."));
		}

		[TestMethod]
		public async Task Convert_WithUndefinedNumericStringCode_ShouldThrowFormatException()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));

			var ex = await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync("3", meta, "status", CancellationToken.None));

			Assert.IsTrue(ex.Message.Contains("Valid codes are: 1, 2."));
		}

		[TestMethod]
		public async Task Convert_WithOutOfRangeLong_ShouldThrowFormatException()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));

			var ex = await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync((long)int.MaxValue + 1, meta, "status", CancellationToken.None));

			Assert.IsTrue(ex.Message.Contains("out of Int32 range"));
		}

		[TestMethod]
		public async Task Convert_WithValidLabel_ShouldReturnMatchingOptionSetValue()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));
			var result = await _converter.ConvertAsync("Active", meta, "status", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(1, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public async Task Convert_WithLabelCaseInsensitive_ShouldWork()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));
			var result = await _converter.ConvertAsync("active", meta, "status", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(OptionSetValue));
			Assert.AreEqual(1, ((OptionSetValue)result!).Value);
		}

		[TestMethod]
		public async Task Convert_WithInvalidLabel_ShouldThrowFormatException()
		{
			var meta = BuildPicklistWithOptions((1, "Active"), (2, "Inactive"));

			var ex = await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync("Unknown", meta, "status", CancellationToken.None));

			Assert.IsTrue(ex.Message.Contains("Active"));
			Assert.IsTrue(ex.Message.Contains("Inactive"));
		}

		[TestMethod]
		public async Task Convert_NullValue_ShouldReturnNull()
		{
			var meta = new PicklistAttributeMetadata();
			var result = await _converter.ConvertAsync(null, meta, "status", CancellationToken.None);
			Assert.IsNull(result);
		}
	}
}
