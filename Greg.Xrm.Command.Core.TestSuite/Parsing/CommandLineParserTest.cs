using Greg.Xrm.Command.Commands.Auth;

namespace Greg.Xrm.Command.Parsing
{
	[TestClass]
    public class CommandLineParserTest
    {
        [TestMethod]
        public void AuthListShouldBeResolvedProperly()
        {
            var parser = new CommandLineParser(new OutputToMemory());
            parser.InitializeFromAssembly(typeof(ListCommand).Assembly);

            var command = parser.Parse("auth", "list");

            Assert.IsNotNull(command);
            Assert.AreEqual(typeof(ListCommand), command.GetType());
        }



    }
}