using Autofac;
using Greg.Xrm.Command.Commands.Auth;

namespace Greg.Xrm.Command.Parsing
{
	[TestClass]
    public class CommandLineParserTest
    {
        [TestMethod]
        public void AuthListShouldBeResolvedProperly()
        {
            var container = new ContainerBuilder().Build();
			var registry = new CommandRegistry(container);
			registry.InitializeFromAssembly(typeof(ListCommand).Assembly);

			var parser = new CommandParser(new OutputToMemory(), registry);

            var command = parser.Parse("auth", "list");

            Assert.IsNotNull(command);
            Assert.AreEqual(typeof(ListCommand), command.GetType());
        }



    }
}