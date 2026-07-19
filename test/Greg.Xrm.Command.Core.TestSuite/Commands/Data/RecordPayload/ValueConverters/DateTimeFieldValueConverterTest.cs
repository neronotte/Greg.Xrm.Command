using Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	[TestClass]
	public class DateTimeFieldValueConverterTest
	{
		private readonly DateTimeFieldValueConverter _converter = new();

		private static DateTimeAttributeMetadata DateOnlyMeta()
		{
			var meta = new DateTimeAttributeMetadata(DateTimeFormat.DateOnly);
			// DateTimeBehavior is settable via the property
			typeof(DateTimeAttributeMetadata)
				.GetProperty(nameof(DateTimeAttributeMetadata.DateTimeBehavior))!
				.SetValue(meta, DateTimeBehavior.DateOnly);
			return meta;
		}

		private static DateTimeAttributeMetadata DateAndTimeMeta()
		{
			var meta = new DateTimeAttributeMetadata(DateTimeFormat.DateAndTime);
			typeof(DateTimeAttributeMetadata)
				.GetProperty(nameof(DateTimeAttributeMetadata.DateTimeBehavior))!
				.SetValue(meta, DateTimeBehavior.UserLocal);
			return meta;
		}

		[TestMethod]
		public async Task Convert_DateOnly_WithValidDate_ShouldReturnUnspecifiedMidnight()
		{
			var meta = DateOnlyMeta();
			var result = await _converter.ConvertAsync("1990-05-20", meta, "birthdate", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(1990, dt.Year);
			Assert.AreEqual(5, dt.Month);
			Assert.AreEqual(20, dt.Day);
			Assert.AreEqual(0, dt.Hour);
			Assert.AreEqual(DateTimeKind.Unspecified, dt.Kind);
		}

		[TestMethod]
		public async Task Convert_DateOnly_WithInvalidFormat_ShouldThrowFormatException()
		{
			var meta = DateOnlyMeta();
			await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync("20-05-1990", meta, "birthdate", CancellationToken.None));
		}

		[TestMethod]
		public async Task Convert_DateAndTime_WithIso8601_ShouldReturnDateTime()
		{
			var meta = DateAndTimeMeta();
			var result = await _converter.ConvertAsync("2024-01-15T08:30:00Z", meta, "createdon", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(2024, dt.Year);
			Assert.AreEqual(1, dt.Month);
			Assert.AreEqual(15, dt.Day);
		}

		[TestMethod]
		public async Task Convert_DateAndTime_WithIso8601WithOffset_ShouldReturnDateTime()
		{
			var meta = DateAndTimeMeta();
			var result = await _converter.ConvertAsync("2024-01-15T08:30:00+02:00", meta, "createdon", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(2024, dt.Year);
			Assert.AreEqual(1, dt.Month);
			Assert.AreEqual(15, dt.Day);
			Assert.AreEqual(6, dt.Hour);
			Assert.AreEqual(30, dt.Minute);
			Assert.AreEqual(DateTimeKind.Utc, dt.Kind);
		}

		[TestMethod]
		public async Task Convert_DateAndTime_WithDateOnlyIso_ShouldReturnDateTime()
		{
			var meta = DateAndTimeMeta();
			var result = await _converter.ConvertAsync("2024-06-01", meta, "createdon", CancellationToken.None);

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(2024, dt.Year);
			Assert.AreEqual(6, dt.Month);
			Assert.AreEqual(1, dt.Day);
		}

		[TestMethod]
		public async Task Convert_DateAndTime_WithCultureDependentFormat_ShouldThrowFormatException()
		{
			var meta = DateAndTimeMeta();
			await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync("01/02/2024", meta, "createdon", CancellationToken.None));
		}

		[TestMethod]
		public async Task Convert_DateAndTime_WithInvalidFormat_ShouldThrowFormatException()
		{
			var meta = DateAndTimeMeta();
			await Assert.ThrowsAsync<FormatException>(
				() => _converter.ConvertAsync("not-a-date", meta, "createdon", CancellationToken.None));
		}

		[TestMethod]
		public async Task Convert_NullValue_ShouldReturnNull()
		{
			var meta = DateAndTimeMeta();
			var result = await _converter.ConvertAsync(null, meta, "createdon", CancellationToken.None);
			Assert.IsNull(result);
		}
	}
}
