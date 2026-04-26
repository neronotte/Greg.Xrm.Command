using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class ListCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly ListCommandExecutor executor;

		public ListCommandExecutorTest()
		{
			this.executor = new ListCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		private Entity CreateSolution(string uniqueName, bool isManaged, bool isVisible, DateTime createdOn, DateTime modifiedOn)
		{
			var entity = new Entity("solution");
			entity["uniquename"] = uniqueName;
			entity["friendlyname"] = uniqueName + "_friendly";
			entity["version"] = "1.0.0.0";
			entity["ismanaged"] = isManaged;
			entity["isvisible"] = isVisible;
			entity["createdon"] = createdOn;
			entity["modifiedon"] = modifiedOn;

			var publisher = new Entity("publisher");
			publisher["uniquename"] = "default";
			publisher["friendlyname"] = "Default Publisher";
			publisher["customizationprefix"] = "new";

			// Set the aliased values from the joined publisher
			entity["p.uniquename"] = new AliasedValue("publisher", "uniquename", publisher["uniquename"]);
			entity["p.friendlyname"] = new AliasedValue("publisher", "friendlyname", publisher["friendlyname"]);
			entity["p.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", publisher["customizationprefix"]);

			return entity;
		}

		private void SetupMockData(params Entity[] solutions)
		{
			var collection = new EntityCollection(solutions);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(collection);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(collection);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithFaultException_ShouldFail()
		{
			var exception = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "Test error" }, new FaultReason("Test error"));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ThrowsAsync(exception);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(exception);

			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage.Contains("Test error"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithNoSolutions_ShouldFail()
		{
			SetupMockData();

			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual("No solutions found in the current environment.", result.ErrorMessage);
		}

		[TestMethod]
		public async Task ExecuteAsync_FilteringManaged_ShouldQueryCorrectly()
		{
			SetupMockData(CreateSolution("A", true, true, DateTime.Now, DateTime.Now));

			var command = new ListCommand { Type = ListCommand.SolutionType.Managed };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
				q.Criteria.Conditions.Any(c => c.AttributeName == "ismanaged" && (bool)c.Values[0] == true)
			)), Times.AtLeastOnce);
		}

		[TestMethod]
		public async Task ExecuteAsync_FilteringUnmanaged_ShouldQueryCorrectly()
		{
			SetupMockData(CreateSolution("A", false, true, DateTime.Now, DateTime.Now));

			var command = new ListCommand { Type = ListCommand.SolutionType.Unmanaged };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
				q.Criteria.Conditions.Any(c => c.AttributeName == "ismanaged" && (bool)c.Values[0] == false)
			)), Times.AtLeastOnce);
		}

		[TestMethod]
		public async Task ExecuteAsync_FilteringVisible_ShouldQueryCorrectly()
		{
			SetupMockData(CreateSolution("A", false, true, DateTime.Now, DateTime.Now));

			// Default is Hidden = false, which means isvisible = true
			var command = new ListCommand { Hidden = false };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
				q.Criteria.Conditions.Any(c => c.AttributeName == "isvisible" && (bool)c.Values[0] == true)
			)), Times.AtLeastOnce);
		}

		[TestMethod]
		public async Task ExecuteAsync_Sorting_ByCreatedOn_ShouldOrderNewestFirst()
		{
			// Z_Managed was created in 2023, A_Unmanaged in 2024
			SetupMockData(
				CreateSolution("Z_Managed", true, true, new DateTime(2023, 1, 1), new DateTime(2023, 1, 1)),
				CreateSolution("A_Unmanaged", false, true, new DateTime(2024, 1, 1), new DateTime(2024, 1, 1))
			);

			var command = new ListCommand { Format = ListCommand.OutputFormat.Json, OrderBy = ListCommand.OutputOrder.CreatedOn };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			var output = this.Output.ToString();
			Assert.IsFalse(string.IsNullOrWhiteSpace(output));

			var managedIndex = output.IndexOf("Z_Managed", StringComparison.Ordinal);
			var unmanagedIndex = output.IndexOf("A_Unmanaged", StringComparison.Ordinal);

			Assert.IsTrue(managedIndex >= 0, "Expected JSON output to contain solution 'Z_Managed'.");
			Assert.IsTrue(unmanagedIndex >= 0, "Expected JSON output to contain solution 'A_Unmanaged'.");
			// Sorting by CreatedOn is descending: A_Unmanaged (2024) must appear before Z_Managed (2023)
			Assert.IsTrue(unmanagedIndex < managedIndex, "Expected solutions to be ordered by CreatedOn descending (newest first).");
		}

		[TestMethod]
		public async Task ExecuteAsync_Sorting_ByName_ShouldOrderAlphabetically()
		{
			SetupMockData(
				CreateSolution("Z_Managed", true, true, new DateTime(2023, 1, 1), new DateTime(2023, 1, 1)),
				CreateSolution("A_Unmanaged", false, true, new DateTime(2024, 1, 1), new DateTime(2024, 1, 1))
			);

			var command = new ListCommand { Format = ListCommand.OutputFormat.Json, OrderBy = ListCommand.OutputOrder.Name };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			var output = this.Output.ToString();

			var managedIndex = output.IndexOf("Z_Managed", StringComparison.Ordinal);
			var unmanagedIndex = output.IndexOf("A_Unmanaged", StringComparison.Ordinal);

			Assert.IsTrue(managedIndex >= 0, "Expected JSON output to contain solution 'Z_Managed'.");
			Assert.IsTrue(unmanagedIndex >= 0, "Expected JSON output to contain solution 'A_Unmanaged'.");
			// Sorting by Name ascending: A_Unmanaged must appear before Z_Managed
			Assert.IsTrue(unmanagedIndex < managedIndex, "Expected solutions to be ordered by name ascending.");
		}
	}
}
