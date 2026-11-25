namespace Greg.Xrm.Command.Model
{
	[TestClass]
	public class WorkflowRepositoryTest
	{
		[TestMethod]
		public void EscapeLikePattern_ShouldEscapeTheLikeWildcards()
		{
			Assert.AreEqual("[[]DEV] Sync Contacts", Workflow.Repository.EscapeLikePattern("[DEV] Sync Contacts"));
			Assert.AreEqual("100[%] done", Workflow.Repository.EscapeLikePattern("100% done"));
			Assert.AreEqual("my[_]flow", Workflow.Repository.EscapeLikePattern("my_flow"));
		}

		[TestMethod]
		public void EscapeLikePattern_ShouldLeavePlainNamesUntouched()
		{
			Assert.AreEqual("My Flow", Workflow.Repository.EscapeLikePattern("My Flow"));
		}

		[TestMethod]
		public void EscapeLikePattern_ShouldSurviveWildcardOnlyNames()
		{
			Assert.AreEqual("[%][%][%]", Workflow.Repository.EscapeLikePattern("%%%"));
			Assert.AreEqual("[[]DEV] 100[%][_]Test", Workflow.Repository.EscapeLikePattern("[DEV] 100%_Test"));
			Assert.AreEqual("a[[][_]]b", Workflow.Repository.EscapeLikePattern("a[_]b"));
		}
	}
}
