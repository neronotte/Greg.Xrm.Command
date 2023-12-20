using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table
{
    [TestClass]
    public class CreateCommandTests
    {
        [TestMethod]
        public void ShortNamesShouldBeResolvedProperly()
        {
            var command = Utility.TestParseCommand<CreateCommand>("create", "table", "-n", "Table1", "-s", "master");

            Assert.AreEqual("Table1", command.DisplayName);
            Assert.IsNull(command.DisplayCollectionName);
            Assert.IsNull(command.Description);
            Assert.IsNull(command.SchemaName);
            Assert.AreEqual("master", command.SolutionName);
            Assert.AreEqual(OwnershipTypes.UserOwned, command.Ownership);
            Assert.IsFalse(command.IsActivity);
            Assert.IsFalse(command.IsAvailableOffline);
            Assert.IsFalse(command.IsValidForQueue);
            Assert.IsFalse(command.HasNotes);
            Assert.IsFalse(command.HasFeedback);
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
