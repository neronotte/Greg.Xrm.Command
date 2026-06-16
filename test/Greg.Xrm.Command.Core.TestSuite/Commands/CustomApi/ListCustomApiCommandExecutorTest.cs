using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class ListCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly ListCustomApiCommandExecutor executor;

		public ListCustomApiCommandExecutorTest()
		{
			this.executor = new ListCustomApiCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		private Entity CreateApiEntity(string uniqueName, string displayName, bool isFunction = false, int bindingType = 0)
		{
			var e = new Entity("customapi") { Id = Guid.NewGuid() };
			e["uniquename"] = uniqueName;
			e["displayname"] = displayName;
			e["isfunction"] = isFunction;
			e["bindingtype"] = new OptionSetValue(bindingType);
			return e;
		}

		private void SetupReturnApis(params Entity[] entities)
		{
			var collection = new EntityCollection(entities.ToList());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ReturnsAsync(collection);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(collection);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApisExist()
		{
			SetupReturnApis(
				CreateApiEntity("nn_GregSum", "Greg Sum"),
				CreateApiEntity("nn_GregMultiply", "Greg Multiply", isFunction: true));

			var result = await executor.ExecuteAsync(new ListCustomApiCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenNoApisFound()
		{
			SetupReturnApis();

			var result = await executor.ExecuteAsync(new ListCustomApiCommand(), CancellationToken.None);

			// No results is still success — prints informational message
			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "No Custom APIs found");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFilterBySubstring_CaseInsensitive()
		{
			SetupReturnApis(
				CreateApiEntity("nn_GregSum", "Greg Sum"),
				CreateApiEntity("nn_Unrelated", "Unrelated"));

			var result = await executor.ExecuteAsync(
				new ListCustomApiCommand { Filter = "greg" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "nn_GregSum");
			Assert.IsFalse(this.Output.ToString().Contains("nn_Unrelated"));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(new ListCustomApiCommand(), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
