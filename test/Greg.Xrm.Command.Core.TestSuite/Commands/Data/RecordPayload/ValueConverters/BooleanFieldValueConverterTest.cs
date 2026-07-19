using Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	[TestClass]
	public class BooleanFieldValueConverterTest
	{
		private readonly BooleanFieldValueConverter _converter = new();
		private readonly BooleanAttributeMetadata _metadata = new();

		[TestMethod]
		public async Task Convert_TrueBool_ShouldReturnTrue()
		{
			var result = await _converter.ConvertAsync(true, _metadata, "flag", CancellationToken.None);
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public async Task Convert_FalseBool_ShouldReturnFalse()
		{
			var result = await _converter.ConvertAsync(false, _metadata, "flag", CancellationToken.None);
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		public async Task Convert_StringTrue_ShouldReturnTrue()
		{
			var result = await _converter.ConvertAsync("true", _metadata, "flag", CancellationToken.None);
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public async Task Convert_StringFalse_ShouldReturnFalse()
		{
			var result = await _converter.ConvertAsync("false", _metadata, "flag", CancellationToken.None);
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		public async Task Convert_StringTrueCaseInsensitive_ShouldReturnTrue()
		{
			var result = await _converter.ConvertAsync("TRUE", _metadata, "flag", CancellationToken.None);
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public async Task Convert_StringOne_ShouldReturnTrue()
		{
			var result = await _converter.ConvertAsync("1", _metadata, "flag", CancellationToken.None);
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public async Task Convert_StringZero_ShouldReturnFalse()
		{
			var result = await _converter.ConvertAsync("0", _metadata, "flag", CancellationToken.None);
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		public async Task Convert_InvalidString_ShouldThrowFormatException()
		{
			await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync("yes", _metadata, "flag", CancellationToken.None));
		}

		[TestMethod]
		public async Task Convert_NullValue_ShouldReturnNull()
		{
			var result = await _converter.ConvertAsync(null, _metadata, "flag", CancellationToken.None);
			Assert.IsNull(result);
		}
	}
}
