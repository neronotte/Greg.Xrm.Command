using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class GenerateCommandTest
	{
		// ── Long-name parsing ──────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithOutputCsLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "generateLateBoundConstants",
				"--outputCs", "C:/output/cs",
				"--namespaceCs", "MyApp.Constants");

			Assert.AreEqual("C:/output/cs", command.OutputCs);
			Assert.AreEqual("MyApp.Constants", command.NamespaceCs);
		}

		[TestMethod]
		public void ParseWithOutputJsLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "generateLateBoundConstants",
				"--outputJs", "C:/output/js",
				"--namespaceJs", "MyApp");

			Assert.AreEqual("C:/output/js", command.OutputJs);
			Assert.AreEqual("MyApp", command.NamespaceJs);
		}

		[TestMethod]
		public void ParseWithSolutionLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "generateLateBoundConstants",
				"--solutionName", "MySolution",
				"--outputCs", "C:/output/cs",
				"--namespaceCs", "MyApp.Constants");

			Assert.AreEqual("MySolution", command.Solution);
		}

		[TestMethod]
		public void ParseWithJsHeaderLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "generateLateBoundConstants",
				"--outputJs", "C:/output/js",
				"--namespaceJs", "MyApp",
				"--jsHeader", "// generated file");

			Assert.AreEqual("// generated file", command.JsHeader);
		}

		[TestMethod]
		public void ParseWithBoolFlagsExplicitShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "generateLateBoundConstants",
				"--outputCs", "C:/output/cs",
				"--namespaceCs", "MyApp.Constants",
				"--withTypes", "false",
				"--withDescriptions", "false");

			Assert.IsFalse(command.WithTypes);
			Assert.IsFalse(command.WithDescriptions);
		}

		// ── Short-name parsing ─────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "generateLateBoundConstants",
				"-sn", "MySolution",
				"-ocs", "C:/output/cs",
				"-ncs", "MyApp.Constants",
				"-ojs", "C:/output/js",
				"-njs", "MyApp",
				"-jsh", "// header");

			Assert.AreEqual("MySolution", command.Solution);
			Assert.AreEqual("C:/output/cs", command.OutputCs);
			Assert.AreEqual("MyApp.Constants", command.NamespaceCs);
			Assert.AreEqual("C:/output/js", command.OutputJs);
			Assert.AreEqual("MyApp", command.NamespaceJs);
			Assert.AreEqual("// header", command.JsHeader);
		}

		// ── Alias verb order ──────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithAliasConstantsShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "constants",
				"--outputCs", "C:/output/cs",
				"--namespaceCs", "MyApp.Constants");

			Assert.AreEqual("C:/output/cs", command.OutputCs);
			Assert.AreEqual("MyApp.Constants", command.NamespaceCs);
		}

		[TestMethod]
		public void ParseWithAliasLateBoundShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "late-bound",
				"--outputCs", "C:/output/cs",
				"--namespaceCs", "MyApp.Constants");

			Assert.AreEqual("C:/output/cs", command.OutputCs);
		}

		[TestMethod]
		public void ParseWithAliasLateBoundCamelCaseShouldWork()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "lateBound",
				"--outputCs", "C:/output/cs",
				"--namespaceCs", "MyApp.Constants");

			Assert.AreEqual("C:/output/cs", command.OutputCs);
		}

		// ── Default values ─────────────────────────────────────────────────────

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<GenerateLateBoundConstantsCommand>(
				"solution", "generateLateBoundConstants",
				"--outputCs", "C:/output/cs",
				"--namespaceCs", "MyApp.Constants");

			Assert.IsNull(command.Solution);
			Assert.IsNull(command.OutputJs);
			Assert.IsNull(command.NamespaceJs);
			Assert.IsNull(command.JsHeader);
			Assert.IsTrue(command.WithTypes);
			Assert.IsTrue(command.WithDescriptions);
		}

		// ── Cross-option validation ────────────────────────────────────────────

		[TestMethod]
		public void ValidationFailsWhenNeitherOutputProvided()
		{
			var command = new GenerateLateBoundConstantsCommand();
			var context = new ValidationContext(command);
			var results = command.Validate(context).ToList();

			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(GenerateLateBoundConstantsCommand.OutputCs))));
		}

		[TestMethod]
		public void ValidationFailsWhenOutputCsWithoutNamespace()
		{
			var command = new GenerateLateBoundConstantsCommand { OutputCs = "C:/output/cs" };
			var context = new ValidationContext(command);
			var results = command.Validate(context).ToList();

			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(GenerateLateBoundConstantsCommand.NamespaceCs))));
		}

		[TestMethod]
		public void ValidationFailsWhenOutputJsWithoutNamespace()
		{
			var command = new GenerateLateBoundConstantsCommand { OutputJs = "C:/output/js" };
			var context = new ValidationContext(command);
			var results = command.Validate(context).ToList();

			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(GenerateLateBoundConstantsCommand.NamespaceJs))));
		}

		[TestMethod]
		public void ValidationPassesWithOutputCsAndNamespace()
		{
			var command = new GenerateLateBoundConstantsCommand
			{
				OutputCs = "C:/output/cs",
				NamespaceCs = "MyApp.Constants"
			};
			var context = new ValidationContext(command);
			var results = command.Validate(context).ToList();

			Assert.IsFalse(results.Any());
		}

		[TestMethod]
		public void ValidationPassesWithBothOutputsAndNamespaces()
		{
			var command = new GenerateLateBoundConstantsCommand
			{
				OutputCs = "C:/output/cs",
				NamespaceCs = "MyApp.Constants",
				OutputJs = "C:/output/js",
				NamespaceJs = "MyApp"
			};
			var context = new ValidationContext(command);
			var results = command.Validate(context).ToList();

			Assert.IsFalse(results.Any());
		}

		[TestMethod]
		public void ValidationPassesWithOutputJsOnlyAndNamespace()
		{
			var command = new GenerateLateBoundConstantsCommand
			{
				OutputJs = "C:/output/js",
				NamespaceJs = "MyApp"
			};
			var context = new ValidationContext(command);
			var results = command.Validate(context).ToList();

			Assert.IsFalse(results.Any());
		}
	}
}
