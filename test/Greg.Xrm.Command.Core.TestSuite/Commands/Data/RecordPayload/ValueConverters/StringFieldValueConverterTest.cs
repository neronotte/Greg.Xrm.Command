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
		public void Convert_WithStringValue_ShouldReturnSameString()
		{
			var result = _converter.Convert("Hello World", _metadata, "name");
			Assert.AreEqual("Hello World", result);
		}

		[TestMethod]
		public void Convert_WithNullValue_ShouldReturnNull()
		{
			var result = _converter.Convert(null, _metadata, "name");
			Assert.IsNull(result);
		}

		[TestMethod]
		public void Convert_WithEmptyString_ShouldReturnNull()
		{
			var result = _converter.Convert("", _metadata, "name");
			Assert.IsNull(result);
		}

		[TestMethod]
		public void Convert_WithNonStringObject_ShouldCallToString()
		{
			var result = _converter.Convert(42, _metadata, "name");
			Assert.AreEqual("42", result);
		}

		[TestMethod]
		public void Convert_WithMemoMetadata_ShouldWork()
		{
			var memoMeta = new MemoAttributeMetadata();
			var result = _converter.Convert("Long text here", memoMeta, "description");
			Assert.AreEqual("Long text here", result);
		}
	}
}
