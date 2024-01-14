using Autofac;
using Greg.Xrm.Command.Commands.Auth;
using Greg.Xrm.Command.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greg.Xrm.Command.Parsing
{
	[TestClass]
    public class CommandLineParserTest
    {
        [TestMethod]
        public void AuthListShouldBeResolvedProperly()
		{
			var log = NullLogger<CommandRegistry>.Instance;
			var output = new OutputToMemory();
			var storage = new Storage();

			var registry = new CommandRegistry(log, output, storage);
			registry.InitializeFromAssembly(typeof(ListCommand).Assembly);

			var parser = new CommandParser(new OutputToMemory(), registry);

            var command = parser.Parse("auth", "list");

            Assert.IsNotNull(command);
            Assert.AreEqual(typeof(ListCommand), command.GetType());
        }



    }
}