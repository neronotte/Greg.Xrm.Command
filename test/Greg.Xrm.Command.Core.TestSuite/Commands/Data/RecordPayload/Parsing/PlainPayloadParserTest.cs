using Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing
{
	[TestClass]
	public class PlainPayloadParserTest
	{
		[TestMethod]
		public void Parse_WithMultipleSimpleFields_ShouldReturnAllPairs()
		{
			var result = PlainPayloadParser.Parse("name=Acme Corp;revenue=1000000");

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual("Acme Corp", result["name"]);
			Assert.AreEqual("1000000", result["revenue"]);
		}

		[TestMethod]
		public void Parse_WithSingleField_ShouldReturnOnePair()
		{
			var result = PlainPayloadParser.Parse("firstname=Mario");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("Mario", result["firstname"]);
		}

		[TestMethod]
		public void Parse_WithEqualsInValue_ShouldIncludeAllAfterFirstEquals()
		{
			var result = PlainPayloadParser.Parse("ownerid=systemuser(domainname='x@y.com')");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("systemuser(domainname='x@y.com')", result["ownerid"]);
		}

		[TestMethod]
		public void Parse_WithSemicolonInsideQuotes_ShouldTreatAsLiteralCharacter()
		{
			var result = PlainPayloadParser.Parse("description='foo;bar;baz'");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("foo;bar;baz", result["description"]);
		}

		[TestMethod]
		public void Parse_WithEscapedQuote_ShouldProduceLiteralQuote()
		{
			var result = PlainPayloadParser.Parse("name=Riccardo''s Corp");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("Riccardo's Corp", result["name"]);
		}

		[TestMethod]
		public void Parse_WithEmptyValue_ShouldReturnEmptyString()
		{
			var result = PlainPayloadParser.Parse("description=");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("", result["description"]);
		}

		[TestMethod]
		public void Parse_WithParenthesesNoQuotes_ShouldWork()
		{
			var result = PlainPayloadParser.Parse("ownerid=systemuser(3fa85f64-5717-4562-b3fc-2c963f66afa6)");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("systemuser(3fa85f64-5717-4562-b3fc-2c963f66afa6)", result["ownerid"]);
		}

		[TestMethod]
		public void Parse_WithLookupContainingQuotedField_ShouldWork()
		{
			var result = PlainPayloadParser.Parse("ownerid=systemuser(fullname='Mario Rossi')");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("systemuser(fullname='Mario Rossi')", result["ownerid"]);
		}

		[TestMethod]
		public void Parse_WithEmptyString_ShouldReturnEmptyDictionary()
		{
			var result = PlainPayloadParser.Parse(string.Empty);

			Assert.AreEqual(0, result.Count);
		}

		[TestMethod]
		public void Parse_WithWhitespaceInValue_ShouldPreserveWhitespace()
		{
			var result = PlainPayloadParser.Parse("name=Acme Corp Ltd");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("Acme Corp Ltd", result["name"]);
		}

		[TestMethod]
		public void Parse_WithMultipleFieldsIncludingLookup_ShouldReturnAll()
		{
			var result = PlainPayloadParser.Parse("firstname=Mario;lastname=Rossi;ownerid=systemuser(domainname='mario@contoso.com')");

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual("Mario", result["firstname"]);
			Assert.AreEqual("Rossi", result["lastname"]);
			Assert.AreEqual("systemuser(domainname='mario@contoso.com')", result["ownerid"]);
		}

		[TestMethod]
		public void Parse_WithQuotedParenthesisInsideLookup_ShouldNotSplitOnSemicolon()
		{
			var result = PlainPayloadParser.Parse("parentcustomerid=account(name='A);B');firstname=Mario");

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual("account(name='A);B')", result["parentcustomerid"]);
			Assert.AreEqual("Mario", result["firstname"]);
		}

		[TestMethod]
		public void Parse_WithUnmatchedParenthesisInPlainText_ShouldTreatItAsLiteral()
		{
			var result = PlainPayloadParser.Parse("description=Use (legacy");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("Use (legacy", result["description"]);
		}

		[TestMethod]
		public void Parse_WithEscapedQuoteInsideQuotedSection_ShouldProduceLiteralQuote()
		{
			// 'Riccardo''s note' inside quotes → Riccardo's note
			var result = PlainPayloadParser.Parse("description='Riccardo''s note'");

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("Riccardo's note", result["description"]);
		}

		[TestMethod]
		public void Parse_WithEmptyValueFollowedByAnotherField_ShouldWork()
		{
			var result = PlainPayloadParser.Parse("description=;firstname=Mario");

			Assert.AreEqual(2, result.Count);
			Assert.AreEqual("", result["description"]);
			Assert.AreEqual("Mario", result["firstname"]);
		}
	}
}
