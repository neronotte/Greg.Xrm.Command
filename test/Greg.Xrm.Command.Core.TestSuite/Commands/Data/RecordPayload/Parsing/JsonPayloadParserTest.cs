using System.Text.Json;
using Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing
{
	[TestClass]
	public class JsonPayloadParserTest
	{
		[TestMethod]
		public void ParseInline_WithSimpleTypes_ShouldReturnCorrectDotNetTypes()
		{
			var json = "{\"name\":\"Acme\",\"revenue\":1000000,\"active\":true,\"score\":3.14,\"description\":null}";
			var result = JsonPayloadParser.ParseInline(json);

			Assert.AreEqual("Acme", result["name"]);
			Assert.AreEqual(1000000L, result["revenue"]);
				Assert.AreEqual(true, result["active"]);
				Assert.AreEqual(3.14m, (decimal)result["score"]!);
				Assert.IsNull(result["description"]);
		}

		[TestMethod]
		public void ParseInline_WithArrayOfIntegers_ShouldReturnListOfLong()
		{
			var json = "{\"choices\":[1,2,3]}";
			var result = JsonPayloadParser.ParseInline(json);

			var list = result["choices"] as List<object?>;
			Assert.IsNotNull(list);
			Assert.AreEqual(3, list.Count);
			Assert.AreEqual(1L, list[0]);
			Assert.AreEqual(2L, list[1]);
			Assert.AreEqual(3L, list[2]);
		}

		[TestMethod]
		public void ParseInline_WithArrayOfStrings_ShouldReturnListOfString()
		{
			var json = "{\"tags\":[\"Red\",\"Blue\",\"Green\"]}";
			var result = JsonPayloadParser.ParseInline(json);

			var list = result["tags"] as List<object?>;
			Assert.IsNotNull(list);
			Assert.AreEqual(3, list.Count);
			Assert.AreEqual("Red", list[0]);
			Assert.AreEqual("Blue", list[1]);
			Assert.AreEqual("Green", list[2]);
		}

		[TestMethod]
		public void ParseInline_WithNullValue_ShouldReturnNull()
		{
			var json = "{\"field\":null}";
			var result = JsonPayloadParser.ParseInline(json);

			Assert.IsTrue(result.ContainsKey("field"));
			Assert.IsNull(result["field"]);
		}

		[TestMethod]
		public void ParseInline_WithObjectValue_ShouldThrowInvalidOperationException()
		{
			var json = "{\"address\":{\"city\":\"Milan\"}}";

			var ex = Assert.Throws<InvalidOperationException>(() => JsonPayloadParser.ParseInline(json));
			Assert.IsTrue(ex.Message.Contains("JSON objects are not supported"));
		}

		[TestMethod]
		public void ParseInline_WithMalformedJson_ShouldThrowJsonException()
		{
			var json = "{invalid json}";

			Assert.Throws<JsonException>(() => JsonPayloadParser.ParseInline(json));
		}

		[TestMethod]
		public void ParseFile_WithValidFile_ShouldParseCorrectly()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				File.WriteAllText(tempFile, "{\"name\":\"Test\",\"count\":42}");
				var result = JsonPayloadParser.ParseFile(tempFile);

				Assert.AreEqual("Test", result["name"]);
				Assert.AreEqual(42L, result["count"]);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void ParseFile_WithNonExistentFile_ShouldThrow()
		{
			var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

			Assert.Throws<FileNotFoundException>(() => JsonPayloadParser.ParseFile(nonExistentPath));
		}

		[TestMethod]
		public void ParseInline_WithBooleanFalse_ShouldReturnFalse()
		{
			var json = "{\"active\":false}";
			var result = JsonPayloadParser.ParseInline(json);

			Assert.AreEqual(false, result["active"]);
		}
	}
}
