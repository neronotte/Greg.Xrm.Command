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
		public void Convert_TrueBool_ShouldReturnTrue()
		{
			var result = _converter.Convert(true, _metadata, "flag");
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public void Convert_FalseBool_ShouldReturnFalse()
		{
			var result = _converter.Convert(false, _metadata, "flag");
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		public void Convert_StringTrue_ShouldReturnTrue()
		{
			var result = _converter.Convert("true", _metadata, "flag");
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public void Convert_StringFalse_ShouldReturnFalse()
		{
			var result = _converter.Convert("false", _metadata, "flag");
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		public void Convert_StringTrueCaseInsensitive_ShouldReturnTrue()
		{
			var result = _converter.Convert("TRUE", _metadata, "flag");
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public void Convert_StringOne_ShouldReturnTrue()
		{
			var result = _converter.Convert("1", _metadata, "flag");
			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public void Convert_StringZero_ShouldReturnFalse()
		{
			var result = _converter.Convert("0", _metadata, "flag");
			Assert.AreEqual(false, result);
		}

		[TestMethod]
		public void Convert_InvalidString_ShouldThrowFormatException()
		{
			Assert.Throws<FormatException>(
				() => _converter.Convert("yes", _metadata, "flag"));
		}

		[TestMethod]
		public void Convert_NullValue_ShouldReturnNull()
		{
			var result = _converter.Convert(null, _metadata, "flag");
			Assert.IsNull(result);
		}
	}
}
