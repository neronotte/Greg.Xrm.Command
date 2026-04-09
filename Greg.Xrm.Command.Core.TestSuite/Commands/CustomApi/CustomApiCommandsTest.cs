namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CustomApiCommandsTest
	{
		[TestMethod]
		public void CreateCommand_ParseWithRequiredArgumentsShouldWork()
		{
			var command = Utility.TestParseCommand<CustomApiCreateCommand>(
				"custom-api", "create",
				"--name", "new_MyAction");

			Assert.AreEqual("new_MyAction", command.Name);
			Assert.IsNull(command.Inputs);
			Assert.IsNull(command.Outputs);
			Assert.AreEqual("Global", command.BindingType);
			Assert.IsFalse(command.IsFunction);
		}

		[TestMethod]
		public void CreateCommand_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<CustomApiCreateCommand>(
				"custom-api", "create",
				"--name", "new_MyAction",
				"--display-name", "My Action",
				"--description", "Does something",
				"--input", "String:Target",
				"--input", "Int:Count",
				"--output", "Entity:Result",
				"--binding-type", "Entity",
				"--entity", "account",
				"--solution", "MySolution",
				"--execute-plugin", "MyPlugin",
				"--is-function");

			Assert.AreEqual("new_MyAction", command.Name);
			Assert.AreEqual("My Action", command.DisplayName);
			Assert.AreEqual("Does something", command.Description);
			Assert.AreEqual(2, command.Inputs?.Length);
			Assert.AreEqual("String:Target", command.Inputs![0]);
			Assert.AreEqual("Entity:Result", command.Outputs?[0]);
			Assert.AreEqual("Entity", command.BindingType);
			Assert.AreEqual("account", command.EntityLogicalName);
			Assert.IsTrue(command.IsFunction);
		}

		[TestMethod]
		public void ListCommand_ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<CustomApiListCommand>(
				"custom-api", "list");

			Assert.AreEqual("table", command.Format);
			Assert.IsNull(command.EntityLogicalName);
		}

		[TestMethod]
		public void ListCommand_ParseWithEntityFilterShouldWork()
		{
			var command = Utility.TestParseCommand<CustomApiListCommand>(
				"custom-api", "list",
				"--entity", "account",
				"-f", "json");

			Assert.AreEqual("account", command.EntityLogicalName);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void DeleteCommand_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<CustomApiDeleteCommand>(
				"custom-api", "delete",
				"--name", "new_MyAction");

			Assert.AreEqual("new_MyAction", command.Name);
		}
	}
}
