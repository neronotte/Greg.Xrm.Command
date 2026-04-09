namespace Greg.Xrm.Command.Commands.ElasticTable
{
	[TestClass]
	public class ElasticTableManageCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<ElasticTableManageCommand>(
				"elastic-table", "manage",
				"-t", "myelastictable");

			Assert.AreEqual("myelastictable", command.TableLogicalName);
			Assert.IsNull(command.RetentionPeriod);
			Assert.IsNull(command.ScaleCapacity);
			Assert.IsNull(command.EnableChangefeed);
			Assert.IsFalse(command.ShowConfig);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<ElasticTableManageCommand>(
				"elastic-table", "manage",
				"-t", "myelastictable",
				"--retention", "90d",
				"--scale", "High",
				"--changelog",
				"-f", "json");

			Assert.AreEqual("myelastictable", command.TableLogicalName);
			Assert.AreEqual("90d", command.RetentionPeriod);
			Assert.AreEqual("High", command.ScaleCapacity);
			Assert.IsTrue(command.EnableChangefeed);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void ParseWithShowConfigShouldWork()
		{
			var command = Utility.TestParseCommand<ElasticTableManageCommand>(
				"elastic-table", "manage",
				"-t", "myelastictable",
				"--show");

			Assert.AreEqual("myelastictable", command.TableLogicalName);
			Assert.IsTrue(command.ShowConfig);
		}
	}
}
