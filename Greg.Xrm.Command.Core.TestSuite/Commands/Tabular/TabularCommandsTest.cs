namespace Greg.Xrm.Command.Commands.Tabular
{
	[TestClass]
	public class TabularCommandsTest
	{
		[TestMethod]
		public void TabularDeploy_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<TabularDeployCommand>(
				"tabular", "deploy",
				"-b", "model.bim",
				"-w", "MyWorkspace");

			Assert.AreEqual("model.bim", command.BimFilePath);
			Assert.AreEqual("MyWorkspace", command.Workspace);
			Assert.IsNull(command.DatasetName);
			Assert.AreEqual("auto", command.Mode);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void TabularDeploy_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<TabularDeployCommand>(
				"tabular", "deploy",
				"-b", "model.bim",
				"-w", "MyWorkspace",
				"-d", "MyDataset",
				"-m", "xmla",
				"--dry-run");

			Assert.AreEqual("model.bim", command.BimFilePath);
			Assert.AreEqual("MyWorkspace", command.Workspace);
			Assert.AreEqual("MyDataset", command.DatasetName);
			Assert.AreEqual("xmla", command.Mode);
			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void TabularDiff_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<TabularDiffCommand>(
				"tabular", "diff",
				"-b", "model.bim",
				"-w", "MyWorkspace");

			Assert.AreEqual("model.bim", command.BimFilePath);
			Assert.AreEqual("MyWorkspace", command.Workspace);
			Assert.IsNull(command.DatasetName);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void TabularValidate_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<TabularValidateCommand>(
				"tabular", "validate",
				"-b", "model.bim");

			Assert.AreEqual("model.bim", command.BimFilePath);
			Assert.IsFalse(command.Strict);
		}

		[TestMethod]
		public void TabularValidate_ParseWithStrictShouldWork()
		{
			var command = Utility.TestParseCommand<TabularValidateCommand>(
				"tabular", "validate",
				"-b", "model.bim",
				"--strict");

			Assert.IsTrue(command.Strict);
		}

		[TestMethod]
		public void BimCompare_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<BimCompareCommand>(
				"bim", "compare",
				"-a", "model-a.bim",
				"-b", "model-b.bim");

			Assert.AreEqual("model-a.bim", command.FileA);
			Assert.AreEqual("model-b.bim", command.FileB);
			Assert.AreEqual("table", command.Format);
		}
	}
}
