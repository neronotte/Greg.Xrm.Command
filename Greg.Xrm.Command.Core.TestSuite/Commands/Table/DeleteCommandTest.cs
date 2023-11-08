namespace Greg.Xrm.Command.Commands.Table
{
    [TestClass]
    public class DeleteCommandTest
    {
        [TestMethod]
        public void ParseWithLongNameShouldWork()
        {
            var command = Utility.TestParseCommand<DeleteCommand>("delete", "table", "--name", "Table1");
            Assert.AreEqual("Table1", command.SchemaName);
        }


        [TestMethod]
        public void ParseWithShortNameShouldWork()
        {
            var command = Utility.TestParseCommand<DeleteCommand>("delete", "table", "-n", "Table1");
            Assert.AreEqual("Table1", command.SchemaName);
        }
    }
}
