using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class CreateCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>(
				"solution", "create",
				"--name", "My Solution",
				"--uniqueName", "MySolution_Unique",
				"--publisherPrefix", "prfx",
				"--publisherUniqueName", "prfx_pub",
				"--publisherFriendlyName", "Prefix Publisher",
				"--publisherOptionSetPrefix", "12345",
				"--applicationRibbons"
			);

			Assert.AreEqual("My Solution", command.DisplayName);
			Assert.AreEqual("MySolution_Unique", command.UniqueName);
			Assert.AreEqual("prfx", command.PublisherCustomizationPrefix);
			Assert.AreEqual("prfx_pub", command.PublisherUniqueName);
			Assert.AreEqual("Prefix Publisher", command.PublisherFriendlyName);
			Assert.AreEqual(12345, command.PublisherOptionSetPrefix);
			Assert.IsTrue(command.AddApplicationRibbons);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCommand>(
				"solution", "create",
				"-n", "My Solution Short",
				"-un", "ShortUnique",
				"-pp", "shrt",
				"-pun", "shrt_pub",
				"-puf", "Short Publisher",
				"-pop", "54321"
			);

			Assert.AreEqual("My Solution Short", command.DisplayName);
			Assert.AreEqual("ShortUnique", command.UniqueName);
			Assert.AreEqual("shrt", command.PublisherCustomizationPrefix);
			Assert.AreEqual("shrt_pub", command.PublisherUniqueName);
			Assert.AreEqual("Short Publisher", command.PublisherFriendlyName);
			Assert.AreEqual(54321, command.PublisherOptionSetPrefix);
			Assert.IsFalse(command.AddApplicationRibbons);
		}
	}
}
