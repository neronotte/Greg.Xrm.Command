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
		public void Convert_DateOnly_WithValidDate_ShouldReturnUtcMidnight()
		{
			var meta = DateOnlyMeta();
			var result = _converter.Convert("1990-05-20", meta, "birthdate");

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(1990, dt.Year);
			Assert.AreEqual(5, dt.Month);
			Assert.AreEqual(20, dt.Day);
			Assert.AreEqual(0, dt.Hour);
			Assert.AreEqual(DateTimeKind.Utc, dt.Kind);
		}

		[TestMethod]
		public void Convert_DateOnly_WithInvalidFormat_ShouldThrowFormatException()
		{
			var meta = DateOnlyMeta();
			Assert.Throws<FormatException>(
				() => _converter.Convert("20-05-1990", meta, "birthdate"));
		}

		[TestMethod]
		public void Convert_DateAndTime_WithIso8601_ShouldReturnDateTime()
		{
			var meta = DateAndTimeMeta();
			var result = _converter.Convert("2024-01-15T08:30:00Z", meta, "createdon");

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(2024, dt.Year);
			Assert.AreEqual(1, dt.Month);
			Assert.AreEqual(15, dt.Day);
		}

		[TestMethod]
		public void Convert_DateAndTime_WithIso8601WithOffset_ShouldReturnDateTime()
		{
			var meta = DateAndTimeMeta();
			var result = _converter.Convert("2024-01-15T08:30:00+02:00", meta, "createdon");

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(2024, dt.Year);
			Assert.AreEqual(1, dt.Month);
			Assert.AreEqual(15, dt.Day);
		}

		[TestMethod]
		public void Convert_DateAndTime_WithDateOnlyIso_ShouldReturnDateTime()
		{
			var meta = DateAndTimeMeta();
			var result = _converter.Convert("2024-06-01", meta, "createdon");

			Assert.IsInstanceOfType(result, typeof(DateTime));
			var dt = (DateTime)result!;
			Assert.AreEqual(2024, dt.Year);
			Assert.AreEqual(6, dt.Month);
			Assert.AreEqual(1, dt.Day);
		}

		[TestMethod]
		public void Convert_DateAndTime_WithCultureDependentFormat_ShouldThrowFormatException()
		{
			var meta = DateAndTimeMeta();
			Assert.Throws<FormatException>(
				() => _converter.Convert("01/02/2024", meta, "createdon"));
		}

		[TestMethod]
		public void Convert_DateAndTime_WithInvalidFormat_ShouldThrowFormatException()
		{
			var meta = DateAndTimeMeta();
			Assert.Throws<FormatException>(
				() => _converter.Convert("not-a-date", meta, "createdon"));
		}

		[TestMethod]
		public void Convert_NullValue_ShouldReturnNull()
		{
			var meta = DateAndTimeMeta();
			var result = _converter.Convert(null, meta, "createdon");
			Assert.IsNull(result);
		}
	}
}
