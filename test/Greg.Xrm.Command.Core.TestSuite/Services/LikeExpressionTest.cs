namespace Greg.Xrm.Command.Services
{
	[TestClass]
	public class LikeExpressionTest
	{
		[TestMethod]
		[DataRow("abc", "abc")]
		[DataRow("a_b", "a[_]b")]
		[DataRow("50%", "50[%]")]
		[DataRow("[x]", "[[]x]")]
		public void EscapeShouldEscapeWildcards(string value, string expected)
		{
			Assert.AreEqual(expected, LikeExpression.Escape(value));
		}

		[TestMethod]
		public void ContainsShouldTrimEscapeAndWrapWithPercent()
		{
			Assert.AreEqual("%ma[_]rio%", LikeExpression.Contains("  ma_rio "));
		}
	}
}
