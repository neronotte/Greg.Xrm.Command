namespace Greg.Xrm.Command.Commands.Catalog
{
	[TestClass]
	public class CatalogPublishCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<CatalogPublishCommand>(
				"catalog", "publish-item",
				"--name", "MyEvent");

			Assert.AreEqual("MyEvent", command.Name);
			Assert.AreEqual("BusinessEvent", command.Type);
			Assert.AreEqual("1.0.0", command.Version);
			Assert.IsNull(command.Description);
			Assert.IsNull(command.DefinitionPath);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<CatalogPublishCommand>(
				"catalog", "publish-item",
				"--name", "MyEvent",
				"--type", "ApiDefinition",
				"--description", "An API definition",
				"--version", "2.0.0",
				"-d", "definition.json",
				"--dry-run");

			Assert.AreEqual("MyEvent", command.Name);
			Assert.AreEqual("ApiDefinition", command.Type);
			Assert.AreEqual("An API definition", command.Description);
			Assert.AreEqual("2.0.0", command.Version);
			Assert.AreEqual("definition.json", command.DefinitionPath);
			Assert.IsTrue(command.DryRun);
		}
	}
}
