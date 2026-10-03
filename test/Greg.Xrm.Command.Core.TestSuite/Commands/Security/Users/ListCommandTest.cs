using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	[TestClass]
	public class ListCommandTest
	{
		[TestMethod]
		public void ParseWithoutArgumentsShouldUseDefaults()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "user", "list");

			Assert.IsNull(command.Name);
			Assert.IsFalse(command.IncludeDisabled);
			Assert.IsFalse(command.IncludeApplicationUsers);
			Assert.IsNull(command.Top);
		}

		[TestMethod]
		public void ParseWithAllLongOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "user", "list", "--name", "mario", "--include-disabled", "--include-app-users", "--top", "5");

			Assert.AreEqual("mario", command.Name);
			Assert.IsTrue(command.IncludeDisabled);
			Assert.IsTrue(command.IncludeApplicationUsers);
			Assert.AreEqual(5, command.Top);
		}

		[TestMethod]
		public void ParseWithAllShortOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "user", "list", "-n", "mario", "-d", "-app", "-t", "5");

			Assert.AreEqual("mario", command.Name);
			Assert.IsTrue(command.IncludeDisabled);
			Assert.IsTrue(command.IncludeApplicationUsers);
			Assert.AreEqual(5, command.Top);
		}

		[TestMethod]
		public void ValidateShouldRejectNonPositiveTop()
		{
			var context = new System.ComponentModel.DataAnnotations.ValidationContext(new object());
			Assert.AreEqual(1, new ListCommand { Top = 0 }.Validate(context).Count());
			Assert.AreEqual(0, new ListCommand { Top = 1 }.Validate(context).Count());
			Assert.AreEqual(0, new ListCommand().Validate(context).Count());
		}

		[TestMethod]
		public void ParseWithNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "user", "list", "--name", "mario");

			Assert.AreEqual("mario", command.Name);
		}

		[TestMethod]
		public void ParseWithNameShortShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "user", "list", "-n", "mario");

			Assert.AreEqual("mario", command.Name);
		}

		private readonly OutputToMemory output = new();
		private readonly Mock<IOrganizationServiceRepository> repoMock = new();
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly Mock<ISystemUserRepository> userRepoMock = new();

		private ListCommandExecutor CreateExecutor()
		{
			this.repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(this.crmMock.Object);
			return new ListCommandExecutor(this.output, this.repoMock.Object, this.userRepoMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsyncShouldPassQueryToRepositoryAndPrintUsers()
		{
			var userId = Guid.NewGuid();
			var user = new SystemUser(userId, @"DOMAIN\mario.rossi", "Mario", "Rossi", "Mario Rossi", new EntityReference("businessunit", Guid.NewGuid()) { Name = "Root BU" });
			this.userRepoMock
				.Setup(r => r.SearchAsync(this.crmMock.Object, "mario", new SystemUserSearchOptions(true, false, 10), It.IsAny<CancellationToken>()))
				.ReturnsAsync([user]);

			var result = await CreateExecutor().ExecuteAsync(new ListCommand { Name = "mario", IncludeDisabled = true, Top = 10 }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(1, result["Count"]);
			this.userRepoMock.Verify(r => r.SearchAsync(this.crmMock.Object, "mario", new SystemUserSearchOptions(true, false, 10), It.IsAny<CancellationToken>()), Times.Once);
			StringAssert.Contains(this.output.ToString(), "Found 1 user.");
			var text = this.output.ToString();
			StringAssert.Contains(text, userId.ToString());
			StringAssert.Contains(text, @"DOMAIN\mario.rossi");
			StringAssert.Contains(text, "Rossi");
			StringAssert.Contains(text, "Root BU");
		}

		[TestMethod]
		public async Task ExecuteAsyncWithNoUsersShouldSucceed()
		{
			this.userRepoMock
				.Setup(r => r.SearchAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string?>(), It.IsAny<SystemUserSearchOptions?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var result = await CreateExecutor().ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, result["Count"]);
		}

		[TestMethod]
		public async Task ExecuteAsyncShouldFailOnDataverseFault()
		{
			this.userRepoMock
				.Setup(r => r.SearchAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string?>(), It.IsAny<SystemUserSearchOptions?>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "boom"));

			var result = await CreateExecutor().ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "boom");
		}
	}
}
