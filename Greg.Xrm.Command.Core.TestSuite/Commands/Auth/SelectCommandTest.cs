using Greg.Xrm.Command.Commands.Auth;

namespace Greg.Xrm.Command.Commands.Delete
{
	[TestClass]
	public class SelectCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<SelectCommand>("auth", "select", "--name", "Conn1");
			Assert.AreEqual("Conn1", command.Name);
		}


		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<SelectCommand>("auth", "select", "-n", "Conn1");
			Assert.AreEqual("Conn1", command.Name);
		}
	}
}
