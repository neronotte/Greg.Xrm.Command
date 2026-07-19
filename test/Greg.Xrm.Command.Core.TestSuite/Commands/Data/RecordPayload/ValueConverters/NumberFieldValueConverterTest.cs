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
		public async Task Convert_IntegerFromLong_ShouldReturnInt()
		{
			var result = await _converter.ConvertAsync(42L, new IntegerAttributeMetadata(), "count", CancellationToken.None);
			Assert.IsInstanceOfType(result, typeof(int));
			Assert.AreEqual(42, result);
		}

		[TestMethod]
		public async Task Convert_IntegerFromString_ShouldReturnInt()
		{
			var result = await _converter.ConvertAsync("100", new IntegerAttributeMetadata(), "count", CancellationToken.None);
			Assert.IsInstanceOfType(result, typeof(int));
			Assert.AreEqual(100, result);
		}

		[TestMethod]
		public async Task Convert_IntegerFromInvalidString_ShouldThrowFormatException()
		{
			await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync("not-a-number", new IntegerAttributeMetadata(), "count", CancellationToken.None));
		}

		[TestMethod]
		public async Task Convert_DecimalFromString_ShouldReturnDecimal()
		{
			var result = await _converter.ConvertAsync("1234.56", new DecimalAttributeMetadata(), "amount", CancellationToken.None);
			Assert.IsInstanceOfType(result, typeof(decimal));
			Assert.AreEqual(1234.56m, result);
		}

		[TestMethod]
		public async Task Convert_DoubleFromString_ShouldReturnDouble()
		{
			var result = await _converter.ConvertAsync("3.14", new DoubleAttributeMetadata(), "ratio", CancellationToken.None);
			Assert.IsInstanceOfType(result, typeof(double));
			Assert.AreEqual(3.14, (double)result!, 0.001);
		}

		[TestMethod]
		public async Task Convert_MoneyFromString_ShouldReturnMoneyObject()
		{
			var result = await _converter.ConvertAsync("50000.00", new MoneyAttributeMetadata(), "revenue", CancellationToken.None);
			Assert.IsInstanceOfType(result, typeof(Money));
			Assert.AreEqual(50000m, ((Money)result!).Value);
		}

		[TestMethod]
		public async Task Convert_NullValue_ShouldReturnNull()
		{
			var result = await _converter.ConvertAsync(null, new IntegerAttributeMetadata(), "count", CancellationToken.None);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Convert_IntegerFromDouble_ShouldConvert()
		{
			var result = await _converter.ConvertAsync(3.0, new IntegerAttributeMetadata(), "count", CancellationToken.None);
			Assert.AreEqual(3, result);
		}
	}
}
