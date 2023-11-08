using Greg.Xrm.Command.Commands.Auth;
using Greg.Xrm.Command.Commands.Create;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Metadata;

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





        [TestMethod]
        public void ShortNamesShouldBeResolvedProperly()
        {
            var parser = new CommandLineParser(new OutputToMemory());
            parser.InitializeFromAssembly(typeof(ListCommand).Assembly);

            var parseResult = parser.Parse("create", "table", "-n", "Table1", "-s", "master");

            Assert.IsNotNull(parseResult);
            Assert.AreEqual(typeof(CreateTableCommand), parseResult.GetType());

            var command = (CreateTableCommand)parseResult;
            Assert.AreEqual("Table1", command.DisplayName);
            Assert.IsNull(command.DisplayCollectionName);
            Assert.IsNull(command.Description);
            Assert.IsNull(command.SchemaName);
            Assert.AreEqual("master", command.SolutionName);
            Assert.AreEqual(OwnershipTypes.UserOwned, command.Ownership);
            Assert.IsFalse(command.IsActivity);
            Assert.IsTrue(command.IsAuditEnabled);
            Assert.IsNull(command.PrimaryAttributeSchemaName);
            Assert.IsNull(command.PrimaryAttributeDisplayName);
            Assert.IsNull(command.PrimaryAttributeDescription);
            Assert.IsNull(command.PrimaryAttributeAutoNumber);
            Assert.IsNull(command.PrimaryAttributeMaxLength);
            Assert.IsNull(command.PrimaryAttributeRequiredLevel);
        }
    }
}