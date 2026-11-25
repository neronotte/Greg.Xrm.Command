using Greg.Xrm.Command.Commands.Forms.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class FormRepositoryTest : CommandExecutorTestBase
	{

		[TestMethod]
		[DataRow(null)]
		[DataRow("Information")]
		public async Task GetByNameAsync_WhenNoMainFormsExist_ShouldFail(string? name)
		{
			SetupForms();
			var service = new Form.Repository(Output);

			var (result, form) = await service.GetByNameAsync(OrganizationServiceMock.Object, "account", name);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsNull(form);
			StringAssert.Contains(result.ErrorMessage, "No main form found");
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("information")]
		public async Task GetByNameAsync_WhenOneFormMatches_ShouldReturnIt(string? name)
		{
			var expected = CreateForm("Information");
			SetupForms(expected);
			var service = new Form.Repository(Output);

			var (result, form) = await service.GetByNameAsync(OrganizationServiceMock.Object, "account", name);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(expected.Id, form?.Id);
			StringAssert.Contains(Output.ToString(), "Main form found: Information");
		}

		[TestMethod]
		public async Task GetByNameAsync_WhenNameIsMissingAndFormsAreAmbiguous_ShouldFail()
		{
			SetupForms(CreateForm("First"), CreateForm("Second"));
			var (result, form) = await new Form.Repository(Output)
				.GetByNameAsync(OrganizationServiceMock.Object, "account", null);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsNull(form);
			StringAssert.Contains(result.ErrorMessage, "--form");
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task GetByNameAsync_WhenNameDoesNotMatch_ShouldFail(bool multiple)
		{
			var forms = new List<Entity> { CreateForm("First") };
			if (multiple) forms.Add(CreateForm("Second"));
			SetupForms(forms.ToArray());
			var (result, form) = await new Form.Repository(Output)
				.GetByNameAsync(OrganizationServiceMock.Object, "account", "Missing");

			Assert.IsFalse(result.IsSuccess);
			Assert.IsNull(form);
			StringAssert.Contains(result.ErrorMessage, "not found");
		}

		[TestMethod]
		public async Task GetByNameAsync_WhenMultipleNamesMatch_ShouldFail()
		{
			SetupForms(CreateForm("Information"), CreateForm("information"));
			var (result, form) = await new Form.Repository(Output)
				.GetByNameAsync(OrganizationServiceMock.Object, "account", "INFORMATION");

			Assert.IsFalse(result.IsSuccess);
			Assert.IsNull(form);
			StringAssert.Contains(result.ErrorMessage, "uniquely identify");
		}

		[TestMethod]
		public async Task GetByNameAsync_WhenOneOfSeveralFormsMatches_ShouldSelectItWithoutAnnouncement()
		{
			var expected = CreateForm("Second");
			SetupForms(CreateForm("First"), expected);
			var (result, form) = await new Form.Repository(Output)
				.GetByNameAsync(OrganizationServiceMock.Object, "account", "second", announce: false);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(expected.Id, form?.Id);
			Assert.IsFalse(Output.ToString().Contains("Main form found:"));
		}

		private void SetupForms(params Entity[] forms)
		{
			OrganizationServiceMock.Setup(crm => crm.RetrieveMultipleAsync(It.Is<QueryExpression>(query =>
				query.EntityName == "systemform" && query.Criteria.Conditions.Any(condition =>
					condition.AttributeName == "objecttypecode" && Equals(condition.Values[0], "account")) &&
				query.Criteria.Conditions.Any(condition => condition.AttributeName == "type" &&
					Equals(condition.Values[0], (int)FormType.Main)))))
				.ReturnsAsync(new EntityCollection(forms.ToList()));
		}

		private static Entity CreateForm(string name)
		{
			var entity = new Entity("systemform", Guid.NewGuid());
			entity["name"] = name;
			return entity;
		}
	}
}
