namespace Greg.Xrm.Command.Parsing
{
	[TestClass]
	public class CommandRunArgsTest
	{
		private static CommandRunArgs Parse(params string[] args)
		{
			var output = new OutputToMemory();
			var ok = CommandRunArgs.TryParse(args, output, out var result);
			Assert.IsTrue(ok, $"TryParse failed: {output}");
			Assert.IsNotNull(result);
			return result!;
		}

		// ── Basic parsing ────────────────────────────────────────────────────

		[TestMethod]
		public void Parse_Verbs_ShouldWork()
		{
			var result = Parse("table", "list");

			CollectionAssert.AreEqual(new[] { "table", "list" }, result.Verbs.ToArray());
			Assert.AreEqual(0, result.Options.Count);
		}

		// ── --environment is treated as a normal option ───────────────────────

		[TestMethod]
		public void Environment_LongName_ShouldBeInOptions()
		{
			var result = Parse("table", "list", "--environment", "MyConn");

			CollectionAssert.AreEqual(new[] { "table", "list" }, result.Verbs.ToArray());
			Assert.IsTrue(result.Options.ContainsKey("--environment"), "--environment should be in options");
			Assert.AreEqual("MyConn", result.Options["--environment"]);
		}

		[TestMethod]
		public void Environment_ShortName_ShouldBeInOptions()
		{
			var result = Parse("table", "list", "-env", "MyConn");

			Assert.IsTrue(result.Options.ContainsKey("-env"), "-env should be in options");
			Assert.AreEqual("MyConn", result.Options["-env"]);
		}

		[TestMethod]
		public void Environment_DoesNotAffectOtherOptions()
		{
			var result = Parse("table", "list", "--environment", "MyConn", "--name", "foo");

			Assert.AreEqual("MyConn", result.Options["--environment"]);
			Assert.AreEqual("foo", result.Options["--name"]);
		}

		[TestMethod]
		public void Environment_WithUrl_ShouldBeInOptions()
		{
			var result = Parse("table", "list", "--environment", "https://myorg.crm.dynamics.com");

			Assert.AreEqual("https://myorg.crm.dynamics.com", result.Options["--environment"]);
		}

		// ── Coexistence with --interactive ────────────────────────────────────

		[TestMethod]
		public void Environment_WithInteractiveFlag_BothPresentInOptions()
		{
			var result = Parse("--interactive", "--environment", "MyConn");

			Assert.IsTrue(result.Options.ContainsKey("--interactive"), "--interactive must be in options");
			Assert.IsTrue(result.Options.ContainsKey("--environment"), "--environment must be in options");
			Assert.AreEqual("MyConn", result.Options["--environment"]);
		}
	}
}
