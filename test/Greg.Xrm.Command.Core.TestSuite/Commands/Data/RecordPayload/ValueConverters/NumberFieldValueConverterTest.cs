using Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	[TestClass]
	public class NumberFieldValueConverterTest
	{
		private readonly NumberFieldValueConverter _converter = new();

		[TestMethod]
		public void Convert_IntegerFromLong_ShouldReturnInt()
		{
			var result = _converter.Convert(42L, new IntegerAttributeMetadata(), "count");
			Assert.IsInstanceOfType(result, typeof(int));
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public void Convert_IntegerFromString_ShouldReturnInt()
		{
			var result = _converter.Convert("100", new IntegerAttributeMetadata(), "count");
			Assert.IsInstanceOfType(result, typeof(int));
			Assert.AreEqual(100, result);
		}

		[TestMethod]
		public void Convert_IntegerFromInvalidString_ShouldThrowFormatException()
		{
			Assert.Throws<FormatException>(
				() => _converter.Convert("not-a-number", new IntegerAttributeMetadata(), "count"));
		}

		[TestMethod]
		public void Convert_DecimalFromString_ShouldReturnDecimal()
		{
			var result = _converter.Convert("1234.56", new DecimalAttributeMetadata(), "amount");
			Assert.IsInstanceOfType(result, typeof(decimal));
			Assert.AreEqual(1234.56m, result);
		}

		[TestMethod]
		public void Convert_DoubleFromString_ShouldReturnDouble()
		{
			var result = _converter.Convert("3.14", new DoubleAttributeMetadata(), "ratio");
			Assert.IsInstanceOfType(result, typeof(double));
			Assert.AreEqual(3.14, (double)result!, 0.001);
		}

		[TestMethod]
		public void Convert_MoneyFromString_ShouldReturnMoneyObject()
		{
			var result = _converter.Convert("50000.00", new MoneyAttributeMetadata(), "revenue");
			Assert.IsInstanceOfType(result, typeof(Money));
			Assert.AreEqual(50000m, ((Money)result!).Value);
		}

		[TestMethod]
		public void Convert_NullValue_ShouldReturnNull()
		{
			var result = _converter.Convert(null, new IntegerAttributeMetadata(), "count");
			Assert.IsNull(result);
		}

		[TestMethod]
		public void Convert_IntegerFromDouble_ShouldConvert()
		{
			var result = _converter.Convert(3.0, new IntegerAttributeMetadata(), "count");
			Assert.AreEqual(3, result);
		}
	}
}
