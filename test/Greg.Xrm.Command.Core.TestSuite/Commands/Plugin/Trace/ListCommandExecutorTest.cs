using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Plugin.Trace
{
	[TestClass]
	public class ListCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly ListCommandExecutor executor;
		private QueryExpression? capturedQuery;

		public ListCommandExecutorTest()
		{
			this.executor = new ListCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object);
		}

		// ── helpers ───────────────────────────────────────────────────────────

		private static Entity CreateTraceRow(
			string typeName = "MyPlugin.Plugin1",
			string messageName = "Create",
			string primaryEntity = "account",
			string? messageBlock = "step 1 done",
			string? exceptionDetails = null)
		{
			var row = new Entity("plugintracelog", Guid.NewGuid());
			row["createdon"] = new DateTime(2026, 7, 12, 10, 30, 0, DateTimeKind.Utc);
			row["typename"] = typeName;
			row["messagename"] = messageName;
			row["primaryentity"] = primaryEntity;
			row["mode"] = new OptionSetValue(0);
			row["depth"] = 1;
			row["performanceexecutionduration"] = 42;
			if (messageBlock != null) row["messageblock"] = messageBlock;
			if (exceptionDetails != null) row["exceptiondetails"] = exceptionDetails;
			return row;
		}

		private void SetupResponse(params Entity[] rows)
		{
			this.OrganizationServiceMock
				.Setup(s => s.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Callback<QueryBase, CancellationToken>((qb, _) => this.capturedQuery = qb as QueryExpression)
				.ReturnsAsync(new EntityCollection(rows.ToList()));
		}

		// ── query construction ────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldQueryPluginTraceLog_NewestFirst()
		{
			SetupResponse(CreateTraceRow());
			var command = new ListCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(this.capturedQuery);
			Assert.AreEqual("plugintracelog", this.capturedQuery.EntityName);
			Assert.AreEqual(10, this.capturedQuery.TopCount);
			Assert.AreEqual(1, this.capturedQuery.Orders.Count);
			Assert.AreEqual("createdon", this.capturedQuery.Orders[0].AttributeName);
			Assert.AreEqual(OrderType.Descending, this.capturedQuery.Orders[0].OrderType);
			Assert.AreEqual(0, this.capturedQuery.Criteria.Conditions.Count);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFilterByTypeName_WhenNameIsSet()
		{
			SetupResponse(CreateTraceRow());
			var command = new ListCommand { TypeName = "MyPlugin" };

			await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsNotNull(this.capturedQuery);
			var condition = this.capturedQuery.Criteria.Conditions.Single();
			Assert.AreEqual("typename", condition.AttributeName);
			Assert.AreEqual(ConditionOperator.Like, condition.Operator);
			Assert.AreEqual("%MyPlugin%", condition.Values[0]);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFilterByExceptionDetails_WhenErrorsOnlyIsSet()
		{
			SetupResponse(CreateTraceRow(exceptionDetails: "boom"));
			var command = new ListCommand { ErrorsOnly = true };

			await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsNotNull(this.capturedQuery);
			var condition = this.capturedQuery.Criteria.Conditions.Single();
			Assert.AreEqual("exceptiondetails", condition.AttributeName);
			Assert.AreEqual(ConditionOperator.NotNull, condition.Operator);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldRespectTop()
		{
			SetupResponse(CreateTraceRow());
			var command = new ListCommand { Top = 3 };

			await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsNotNull(this.capturedQuery);
			Assert.AreEqual(3, this.capturedQuery.TopCount);
		}

		// ── output ────────────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldWriteTraceDetailsToOutput()
		{
			SetupResponse(CreateTraceRow(
				typeName: "MyPlugin.AccountNumberPlugin",
				messageBlock: "generated account number",
				exceptionDetails: "System.InvalidOperationException: boom"));
			var command = new ListCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var text = this.Output.ToString();
			StringAssert.Contains(text, "MyPlugin.AccountNumberPlugin");
			StringAssert.Contains(text, "generated account number");
			StringAssert.Contains(text, "System.InvalidOperationException: boom");
			Assert.AreEqual(1, result["Count"]);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldShowClassNameAndVersion_WhenTypeNameIsAssemblyQualified()
		{
			SetupResponse(CreateTraceRow(
				typeName: "MyPlugin.AccountNumberPlugin, MyPlugin, Version=1.2.0.0, Culture=neutral, PublicKeyToken=abcdef1234567890"));
			var command = new ListCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var text = this.Output.ToString();
			StringAssert.Contains(text, "MyPlugin.AccountNumberPlugin");
			StringAssert.Contains(text, "v1.2.0.0");
			Assert.IsFalse(text.Contains("PublicKeyToken"), "The assembly qualified name should not be printed as-is.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceedWithHint_WhenNoRecordsFound()
		{
			SetupResponse();
			var command = new ListCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "No plugin trace log records found");
		}

		// ── error handling ────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseCallFaults()
		{
			this.OrganizationServiceMock
				.Setup(s => s.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault { Message = "The 'RetrieveMultiple' method does not support entities of type 'plugintracelog'." },
					new FaultReason("The 'RetrieveMultiple' method does not support entities of type 'plugintracelog'.")));

			var command = new ListCommand();

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not support");
		}
	}
}
