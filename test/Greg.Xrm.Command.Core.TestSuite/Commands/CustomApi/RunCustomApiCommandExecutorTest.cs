using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class RunCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly RunCustomApiCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();

		public RunCustomApiCommandExecutorTest()
		{
			this.executor = new RunCustomApiCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		// ── Setup helpers ──────────────────────────────────────────────────────────

		private void SetupApiFound()
		{
			var api = new Entity("customapi") { Id = ApiId };
			api["uniquename"] = "nn_GregSum";
			api["isfunction"] = false;
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
		}

		private void SetupApiNotFound()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
		}

		private void SetupParams(params (string shortName, int typeCode, bool isOptional)[] defs)
		{
			var entities = defs.Select(d =>
			{
				var e = new Entity("customapirequestparameter") { Id = Guid.NewGuid() };
				e["uniquename"]  = $"nn_GregSum-in-{d.shortName}";
				e["type"]        = new OptionSetValue(d.typeCode);
				e["isoptional"]  = d.isOptional;
				return e;
			}).ToList();

			var col = new EntityCollection(entities);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(col);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(col);
		}

		private void SetupExecuteResponse(Dictionary<string, object?> responseValues)
		{
			var response = new OrganizationResponse();
			foreach (var kv in responseValues)
				response.Results[kv.Key] = kv.Value;

			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync(response);
		}

		// ── Tests ──────────────────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiHasNoParams()
		{
			SetupApiFound();
			SetupParams();
			SetupExecuteResponse([]);

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldPassIntegerParams_WhenJsonProvided()
		{
			SetupApiFound();
			SetupParams(("Addend1", 7, false), ("Addend2", 7, false)); // 7 = Integer

			OrganizationRequest? captured = null;
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.Callback<OrganizationRequest>(r => captured = r)
				.ReturnsAsync(new OrganizationResponse());

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum", Input = "{\"Addend1\":5,\"Addend2\":3}" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(captured);
			Assert.AreEqual(5, captured["nn_GregSum-in-Addend1"]);
			Assert.AreEqual(3, captured["nn_GregSum-in-Addend2"]);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldDisplayResponseValues()
		{
			SetupApiFound();
			SetupParams();
			SetupExecuteResponse(new Dictionary<string, object?> { ["nn_GregSum-out-Result"] = 8 });

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			// Short name is surfaced in CommandResult
			Assert.AreEqual(8, result["Result"]);

			var text = Output.ToString();
			StringAssert.Contains(text, "Result");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenRequiredParamMissing()
		{
			SetupApiFound();
			SetupParams(("Addend1", 7, false)); // required

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum", Input = "{}" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Addend1");
			this.OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenOptionalParamOmitted()
		{
			SetupApiFound();
			SetupParams(("X", 7, false), ("Note", 10, true)); // Note is optional
			SetupExecuteResponse([]);

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum", Input = "{\"X\":42}" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenApiNotFound()
		{
			SetupApiNotFound();

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_Missing" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "nn_Missing");
			this.OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupApiFound();
			SetupParams();
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenInputFileNotFound()
		{
			SetupApiFound();
			SetupParams();

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum", InputFile = "nonexistent_file_xyz.json" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldWarn_WhenUnknownKeyInInput()
		{
			SetupApiFound();
			SetupParams(("X", 7, false));
			SetupExecuteResponse([]);

			await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum", Input = "{\"X\":1,\"Unknown\":99}" },
				CancellationToken.None);

			StringAssert.Contains(Output.ToString(), "Unknown");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldPassStringParam_WhenJsonProvided()
		{
			SetupApiFound();
			SetupParams(("Label", 10, false)); // 10 = String

			OrganizationRequest? captured = null;
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.Callback<OrganizationRequest>(r => captured = r)
				.ReturnsAsync(new OrganizationResponse());

			var result = await executor.ExecuteAsync(
				new RunCustomApiCommand { UniqueName = "nn_GregSum", Input = "{\"Label\":\"hello\"}" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("hello", captured!["nn_GregSum-in-Label"]);
		}
	}
}
