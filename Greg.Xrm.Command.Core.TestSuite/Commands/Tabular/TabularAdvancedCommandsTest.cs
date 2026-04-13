namespace Greg.Xrm.Command.Commands.Tabular
{
	[TestClass]
	public class TabularAdvancedCommandsTest
	{
		[TestMethod]
		public void TabularTranslateParseWithRequiredOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<TabularTranslateCommand>(
				"tabular", "translate",
				"-m", "model-123",
				"-f", "translations.json",
				"-l", "fr-FR");

			Assert.AreEqual("model-123", command.ModelId);
			Assert.AreEqual("translations.json", command.TranslationFile);
			Assert.AreEqual("fr-FR", command.LanguageCode);
			Assert.AreEqual("deploy", command.Mode);
		}

		[TestMethod]
		public void TabularTranslateParseWithModeShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<TabularTranslateCommand>(
				"tabular", "translate",
				"-m", "model-456",
				"-f", "export.json",
				"-l", "ja-JP",
				"--mode", "export",
				"-w", "workspace-789");

			Assert.AreEqual("model-456", command.ModelId);
			Assert.AreEqual("export.json", command.TranslationFile);
			Assert.AreEqual("ja-JP", command.LanguageCode);
			Assert.AreEqual("export", command.Mode);
			Assert.AreEqual("workspace-789", command.WorkspaceId);
		}

		[TestMethod]
		public void TabularRoleAddMeasuresParseShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<TabularRoleAddMeasuresCommand>(
				"tabular", "role", "add-measures",
				"-m", "model-123",
				"--measures", "Sales", "Profit", "Margin");

			Assert.AreEqual("model-123", command.ModelId);
			CollectionAssert.AreEqual(new[] { "Sales", "Profit", "Margin" }, command.Measures);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void TabularRoleAddMeasuresParseWithDryRunShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<TabularRoleAddMeasuresCommand>(
				"tabular", "role", "add-measures",
				"-m", "model-456",
				"--measures", "TotalRevenue",
				"--dry-run");

			Assert.AreEqual("model-456", command.ModelId);
			CollectionAssert.AreEqual(new[] { "TotalRevenue" }, command.Measures);
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void TabularPerspectiveManageParseCreateShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<TabularPerspectiveManageCommand>(
				"tabular", "perspective", "manage",
				"-m", "model-123",
				"-a", "create",
				"-n", "ExecutiveView");

			Assert.AreEqual("model-123", command.ModelId);
			Assert.AreEqual("create", command.Action);
			Assert.AreEqual("ExecutiveView", command.PerspectiveName);
		}

		[TestMethod]
		public void TabularPerspectiveManageParseAddTablesShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<TabularPerspectiveManageCommand>(
				"tabular", "perspective", "manage",
				"-m", "model-456",
				"-a", "add-tables",
				"-n", "SalesPerspective",
				"-t", "Sales", "Products", "Customers");

			Assert.AreEqual("model-456", command.ModelId);
			Assert.AreEqual("add-tables", command.Action);
			Assert.AreEqual("SalesPerspective", command.PerspectiveName);
			CollectionAssert.AreEqual(new[] { "Sales", "Products", "Customers" }, command.Tables);
		}
	}
}
