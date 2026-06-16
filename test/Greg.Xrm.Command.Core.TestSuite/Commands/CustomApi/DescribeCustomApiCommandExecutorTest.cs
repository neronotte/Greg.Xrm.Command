using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class DescribeCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly DescribeCustomApiCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();

		public DescribeCustomApiCommandExecutorTest()
		{
			this.executor = new DescribeCustomApiCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		// ── Helpers ────────────────────────────────────────────────────────────────

		private Entity MakeApi(string uniqueName = "nn_GregSum", bool isFunction = false, string? description = null)
		{
			var e = new Entity("customapi") { Id = ApiId };
			e["uniquename"]    = uniqueName;
			e["displayname"]   = "Greg Sum";
			e["isfunction"]    = isFunction;
			e["isprivate"]     = false;
			e["bindingtype"]   = new OptionSetValue(0);   // Global
			e["allowedcustomprocessingsteptype"] = new OptionSetValue(2); // SyncAndAsync
			if (description != null) e["description"] = description;
			return e;
		}

		private Entity MakeParam(string shortName, int typeCode = 7 /* Integer */, bool isOptional = false, string? description = null)
		{
			var e = new Entity("customapirequestparameter") { Id = Guid.NewGuid() };
					e["name"]        = $"GregSum-In-{shortName}";
					e["uniquename"]  = shortName;
				e["displayname"] = shortName;
				e["type"]        = new OptionSetValue(typeCode);
				e["isoptional"]  = isOptional;
				if (description != null) e["description"] = description;
				return e;
			}

			private Entity MakeResponse(string shortName, int typeCode = 7 /* Integer */, string? description = null)
			{
				var e = new Entity("customapiresponseproperty") { Id = Guid.NewGuid() };
					e["name"]        = $"GregSum-Out-{shortName}";
					e["uniquename"]  = shortName;
				e["displayname"] = shortName;
				e["type"]        = new OptionSetValue(typeCode);
				if (description != null) e["description"] = description;
				return e;
			}

		private void SetupApiFound(Entity api)
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
		}

		private void SetupParams(params Entity[] entities)
		{
			var col = new EntityCollection(entities.ToList());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(col);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(col);
		}

		private void SetupResponses(params Entity[] entities)
		{
			var col = new EntityCollection(entities.ToList());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty")))
				.ReturnsAsync(col);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(col);
		}

		// ── Tests ──────────────────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiExistsWithNoParamsOrResponses()
		{
			SetupApiFound(MakeApi());
			SetupParams();
			SetupResponses();

			var result = await executor.ExecuteAsync(
				new DescribeCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("nn_GregSum", result["UniqueName"]);
			Assert.AreEqual("Action", result["Type"]);
			Assert.AreEqual(0, result["ParameterCount"]);
			Assert.AreEqual(0, result["ResponseCount"]);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldReturnFunction_WhenApiFlagIsSet()
		{
			SetupApiFound(MakeApi(isFunction: true));
			SetupParams();
			SetupResponses();

			var result = await executor.ExecuteAsync(
				new DescribeCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("Function", result["Type"]);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WithParamsAndResponses()
		{
			SetupApiFound(MakeApi(description: "Sums two integers."));
			SetupParams(
				MakeParam("Addend1", typeCode: 7),
				MakeParam("Addend2", typeCode: 7),
				MakeParam("Comment", typeCode: 10 /* String */, isOptional: true, description: "Optional label"));
			SetupResponses(
				MakeResponse("Result", typeCode: 7));

			var result = await executor.ExecuteAsync(
				new DescribeCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(3, result["ParameterCount"]);
			Assert.AreEqual(1, result["ResponseCount"]);

			// Signature and table content rendered via IOutput
			var text = Output.ToString();
			StringAssert.Contains(text, "nn_GregSum");
			StringAssert.Contains(text, "Addend1");
			StringAssert.Contains(text, "Integer");
			StringAssert.Contains(text, "Result");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldIncludeDescription_WhenPresent()
		{
			SetupApiFound(MakeApi(description: "My API description."));
			SetupParams();
			SetupResponses();

			var result = await executor.ExecuteAsync(
				new DescribeCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(Output.ToString(), "My API description.");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenApiNotFound()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var result = await executor.ExecuteAsync(
				new DescribeCustomApiCommand { UniqueName = "nn_Missing" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "nn_Missing");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new DescribeCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldShowSignatureWithOptionalParam()
		{
			SetupApiFound(MakeApi());
			SetupParams(
				MakeParam("X", typeCode: 7, isOptional: false),
				MakeParam("Note", typeCode: 10, isOptional: true));
			SetupResponses(
				MakeResponse("Result", typeCode: 7));

			await executor.ExecuteAsync(
				new DescribeCustomApiCommand { UniqueName = "nn_GregSum" },
				CancellationToken.None);

			var text = Output.ToString();
			// Optional params appear with [] and ? in the signature
			StringAssert.Contains(text, "[Note?:");
			// Required params appear without brackets
			StringAssert.Contains(text, "X: Integer");
			// Arrow separator for response
			StringAssert.Contains(text, "->");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldWriteInputFile_WhenOptionProvided()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				SetupApiFound(MakeApi());
				SetupParams(
					MakeParam("Addend1", typeCode: 7, isOptional: false),
					MakeParam("Comment", typeCode: 10, isOptional: true));
				SetupResponses();

				var result = await executor.ExecuteAsync(
					new DescribeCustomApiCommand { UniqueName = "nn_GregSum", GenerateInputFile = tempFile },
					CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsTrue(File.Exists(tempFile), "Input file should have been created");

				var json = await File.ReadAllTextAsync(tempFile);
				StringAssert.Contains(json, "Addend1");
				StringAssert.Contains(json, "Comment");
				// Integer sample value
				StringAssert.Contains(json, "0");
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldWriteSchemaFile_WhenOptionProvided()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				SetupApiFound(MakeApi(description: "Sums two integers."));
				SetupParams(
					MakeParam("Addend1", typeCode: 7, isOptional: false),
					MakeParam("Comment", typeCode: 10, isOptional: true, description: "A label"));
				SetupResponses();

				var result = await executor.ExecuteAsync(
					new DescribeCustomApiCommand { UniqueName = "nn_GregSum", GenerateSchemaFile = tempFile },
					CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.IsTrue(File.Exists(tempFile), "Schema file should have been created");

				var json = await File.ReadAllTextAsync(tempFile);
				// JSON Schema markers
				StringAssert.Contains(json, "$schema");
				StringAssert.Contains(json, "2020-12");
				StringAssert.Contains(json, "\"title\"");
				StringAssert.Contains(json, "\"properties\"");
				// Required array contains only mandatory params
				StringAssert.Contains(json, "\"required\"");
				StringAssert.Contains(json, "Addend1");
				// Optional param appears in properties but NOT in required
				StringAssert.Contains(json, "Comment");
				// A label was set as description on Comment
				StringAssert.Contains(json, "A label");
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_SchemaFile_ShouldNotContainRequired_WhenAllParamsAreOptional()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				SetupApiFound(MakeApi());
				SetupParams(
					MakeParam("X", typeCode: 7, isOptional: true));
				SetupResponses();

				await executor.ExecuteAsync(
					new DescribeCustomApiCommand { UniqueName = "nn_GregSum", GenerateSchemaFile = tempFile },
					CancellationToken.None);

				var json = await File.ReadAllTextAsync(tempFile);
				// "required" key should not appear if all params are optional
				Assert.IsFalse(json.Contains("\"required\""), "Schema should not have 'required' when all params are optional");
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_InputFile_ShouldContainEntityObjectForEntityType()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				SetupApiFound(MakeApi());
				SetupParams(
					MakeParam("Target", typeCode: 3 /* Entity */, isOptional: false));
				SetupResponses();

				await executor.ExecuteAsync(
					new DescribeCustomApiCommand { UniqueName = "nn_GregSum", GenerateInputFile = tempFile },
					CancellationToken.None);

				var json = await File.ReadAllTextAsync(tempFile);
				// Entity sample should have logicalname and id
				StringAssert.Contains(json, "logicalname");
				StringAssert.Contains(json, "id");
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_InputFile_ShouldContainArrayForStringArrayType()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				SetupApiFound(MakeApi());
				SetupParams(
					MakeParam("Tags", typeCode: 11 /* StringArray */, isOptional: false));
				SetupResponses();

				await executor.ExecuteAsync(
					new DescribeCustomApiCommand { UniqueName = "nn_GregSum", GenerateInputFile = tempFile },
					CancellationToken.None);

				var json = await File.ReadAllTextAsync(tempFile);
				// StringArray sample is a JSON array
				StringAssert.Contains(json, "Tags");
				StringAssert.Contains(json, "[");
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_SchemaFile_ShouldContainArraySchemaForStringArrayType()
		{
			var tempFile = Path.GetTempFileName();
			try
			{
				SetupApiFound(MakeApi());
				SetupParams(
					MakeParam("Tags", typeCode: 11 /* StringArray */, isOptional: false));
				SetupResponses();

				await executor.ExecuteAsync(
					new DescribeCustomApiCommand { UniqueName = "nn_GregSum", GenerateSchemaFile = tempFile },
					CancellationToken.None);

				var json = await File.ReadAllTextAsync(tempFile);
				// StringArray → { "type": "array", "items": { "type": "string" } }
				StringAssert.Contains(json, "\"array\"");
				StringAssert.Contains(json, "\"items\"");
			}
			finally
			{
				if (File.Exists(tempFile)) File.Delete(tempFile);
			}
		}
	}
}
