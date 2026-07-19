using Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	[TestClass]
	public class StringFieldValueConverterTest
	{
		private readonly StringFieldValueConverter _converter = new();
		private readonly StringAttributeMetadata _metadata = new();

		[TestMethod]
		public async Task Convert_WithStringValue_ShouldReturnSameString()
		{
			var result = await _converter.ConvertAsync("Hello World", _metadata, "name", CancellationToken.None);
			Assert.AreEqual("Hello World", result);
		}

		[TestMethod]
		public async Task Convert_WithNullValue_ShouldReturnNull()
		{
			var result = await _converter.ConvertAsync(null, _metadata, "name", CancellationToken.None);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Convert_WithEmptyString_ShouldReturnNull()
		{
			var result = await _converter.ConvertAsync("", _metadata, "name", CancellationToken.None);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Convert_WithNonStringObject_ShouldCallToString()
		{
			var result = await _converter.ConvertAsync(42, _metadata, "name", CancellationToken.None);
			Assert.AreEqual("42", result);
		}

		[TestMethod]
		public async Task Convert_WithMemoMetadata_ShouldWork()
		{
			var memoMeta = new MemoAttributeMetadata();
			var result = await _converter.ConvertAsync("Long text here", memoMeta, "description", CancellationToken.None);
			Assert.AreEqual("Long text here", result);
		}
	}
}
