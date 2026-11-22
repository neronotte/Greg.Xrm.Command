using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Services
{
	[TestClass]
	public class ComponentEditorUrlTest
	{
		[TestMethod]
		[DataRow("form", "form/edit")]
		[DataRow("view", "view")]
		public void Create_ShouldBuildMakerEditorUrl(string type, string path)
		{
			var id = Guid.Parse("12345678-1234-1234-1234-123456789abc");
			var url = ComponentEditorUrl.Create("Default-123", "new_account", type, id);

			Assert.AreEqual($"https://make.powerapps.com/e/Default-123/s/fd140aaf-4df4-11dd-bd17-0019b9312238/entity/new_account/{path}/{id}", url);
		}

		[TestMethod]
		public void Create_ShouldNotProduceIncompleteUrls()
		{
			Assert.IsNull(ComponentEditorUrl.Create(null, "account", "view", Guid.NewGuid()));
			Assert.IsNull(ComponentEditorUrl.Create("env", "", "form", Guid.NewGuid()));
			Assert.IsNull(ComponentEditorUrl.Create("env", "account", "view", Guid.Empty));
			StringAssert.Contains(ComponentEditorUrl.Create("env/a", "table name", "view", Guid.NewGuid())!, "/e/env%2Fa/");
			StringAssert.Contains(ComponentEditorUrl.Create("env", "table name", "view", Guid.NewGuid())!, "/entity/table%20name/");
		}
	}
}
