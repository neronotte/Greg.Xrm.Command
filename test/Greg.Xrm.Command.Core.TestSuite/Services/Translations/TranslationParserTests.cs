using Greg.Xrm.Command.Services.Translations;

namespace Greg.Xrm.Command.Services.Translations
{
	[TestClass]
	public class TranslationParserTests
	{
		private readonly TranslationParser parser = new();

		[TestMethod]
		public void Parse_ShouldReadMultipleQuotedLabels()
		{
			var result = parser.Parse("1033:\"Customer\", 1040:\"Nome cliente\"");

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual("Customer", result[1033]);
			Assert.AreEqual("Nome cliente", result[1040]);
		}

		[TestMethod]
		public void Parse_ShouldPreserveCommasAndEscapedQuotes()
		{
			var result = parser.Parse("1033:\"Customer, \\\"primary\\\"\"");

			Assert.AreEqual("Customer, \"primary\"", result[1033]);
		}

		[TestMethod]
		[DataRow("")]
		[DataRow("1033:Customer")]
		[DataRow("1033:\"Customer\",")]
		[DataRow("1033:\"Customer\", 1040:")]
		[DataRow("it-IT:\"Customer\"")]
		[DataRow("0:\"Customer\"")]
		public void Parse_ShouldRejectInvalidSyntax(string value)
		{
			Assert.ThrowsExactly<CommandException>(() => parser.Parse(value));
		}

		[TestMethod]
		public void Parse_ShouldRejectDuplicateLcid()
		{
			var exception = Assert.ThrowsExactly<CommandException>(
				() => parser.Parse("1033:\"Customer\", 1033:\"Client\""));

			StringAssert.Contains(exception.Message, "more than once");
		}

		[TestMethod]
		public void Parse_ShouldRejectUnsupportedEscapeSequence()
		{
			Assert.ThrowsExactly<CommandException>(
				() => parser.Parse("1033:\"Customer\\n\""));
		}
	}
}
