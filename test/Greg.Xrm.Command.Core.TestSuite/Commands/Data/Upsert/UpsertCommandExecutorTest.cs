using Greg.Xrm.Command.Commands.Data.Upsert;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.Upsert
{
	[TestClass]
	public class UpsertCommandExecutorTest
	{
		private OutputToMemory _output = null!;
		private Mock<IOrganizationServiceRepository> _repoMock = null!;
		private Mock<IOrganizationServiceAsync2> _crmMock = null!;
		private UpsertCommandExecutor _executor = null!;

		[TestInitialize]
		public void Setup()
		{
			_output = new OutputToMemory();
			_crmMock = new Mock<IOrganizationServiceAsync2>();
			_repoMock = new Mock<IOrganizationServiceRepository>();
			_repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(_crmMock.Object);
			_executor = new UpsertCommandExecutor(_output, _repoMock.Object);
		}

		// ── Happy path: record created ─────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WhenRecordCreated_ShouldReturnSuccessWithCreatedFlag()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			SetupUpsertResponse(recordId, recordCreated: true);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(recordId, result["Id"]);
			Assert.AreEqual(true, result["RecordCreated"]);

			var outputText = _output.ToString();
			Assert.IsTrue(outputText.Contains("Record created successfully"));
		}

		// ── Happy path: record updated ─────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WhenRecordUpdated_ShouldReturnSuccessWithUpdatedFlag()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			SetupUpsertResponse(recordId, recordCreated: false);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(recordId, result["Id"]);
			Assert.AreEqual(false, result["RecordCreated"]);

			var outputText = _output.ToString();
			Assert.IsTrue(outputText.Contains("Record updated successfully"));
		}

		// ── Key attributes are set on the entity ──────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSetKeyAttributesOnEntity()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			Entity? capturedEntity = null;
			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest r, CancellationToken _) =>
				{
					capturedEntity = ((UpsertRequest)r).Target;
					var response = new UpsertResponse();
					response.Results["RecordCreated"] = true;
					response.Results["Target"] = new EntityReference("account", recordId);
					return response;
				});

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedEntity);
			Assert.IsTrue(capturedEntity!.KeyAttributes.ContainsKey("accountnumber"));
			Assert.AreEqual("ACC001", capturedEntity.KeyAttributes["accountnumber"]);
		}

		// ── Payload attributes are set on the entity ──────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSetPayloadAttributesOnEntity()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			Entity? capturedEntity = null;
			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest r, CancellationToken _) =>
				{
					capturedEntity = ((UpsertRequest)r).Target;
					var response = new UpsertResponse();
					response.Results["RecordCreated"] = true;
					response.Results["Target"] = new EntityReference("account", recordId);
					return response;
				});

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedEntity);
			Assert.IsTrue(capturedEntity!.Contains("name"));
			Assert.AreEqual("Contoso Ltd", capturedEntity["name"]);
		}

		// ── Dry-run ────────────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithDryRun_ShouldNotCallUpsert()
		{
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd",
				DryRun = true
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			_crmMock.Verify(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()), Times.Never);
			Assert.IsTrue(_output.ToString().Contains("Dry-run"));
		}

		// ── --return causes Retrieve ───────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithReturn_ShouldCallRetrieve()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd",
				Return = "name,telephone1"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			SetupUpsertResponse(recordId, recordCreated: true);

			var retrievedEntity = new Entity("account") { Id = recordId };
			retrievedEntity["name"] = "Contoso Ltd";
			_crmMock.Setup(c => c.RetrieveAsync("account", recordId, It.IsAny<ColumnSet>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(retrievedEntity);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			_crmMock.Verify(c => c.RetrieveAsync("account", recordId, It.IsAny<ColumnSet>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		// ── Table not found ────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WhenTableNotFound_ShouldReturnFail()
		{
			var command = new UpsertCommand
			{
				Table = "nonexistenttable",
				Key = "mykey=value1",
				Plain = "field=value"
			};

			_crmMock.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					new System.ServiceModel.FaultReason("Table not found")));

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("nonexistenttable"));
		}

		// ── Payload conversion errors ──────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithConversionErrors_ShouldFailWithoutCallingUpsert()
		{
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "numberofemployees=not-a-number"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new IntegerAttributeMetadata { LogicalName = "numberofemployees" });

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			_crmMock.Verify(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Upsert SDK fault ───────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WhenUpsertThrowsFaultException_ShouldReturnFail()
		{
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					new System.ServiceModel.FaultReason("SDK error")));

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error upserting record"));
		}

		// ── Multi-field alternate key ──────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithMultiFieldKey_ShouldSetAllKeyAttributes()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "new_config",
				Key = "new_category=pricing;new_region=EMEA",
				Plain = "new_value=standard"
			};

			SetupEntityMetadata("new_config",
				new StringAttributeMetadata { LogicalName = "new_category" },
				new StringAttributeMetadata { LogicalName = "new_region" },
				new StringAttributeMetadata { LogicalName = "new_value" });

			Entity? capturedEntity = null;
			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest r, CancellationToken _) =>
				{
					capturedEntity = ((UpsertRequest)r).Target;
					var response = new UpsertResponse();
					response.Results["RecordCreated"] = true;
					response.Results["Target"] = new EntityReference("new_config", recordId);
					return response;
				});

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedEntity);
			Assert.AreEqual(2, capturedEntity!.KeyAttributes.Count);
			Assert.AreEqual("pricing", capturedEntity.KeyAttributes["new_category"]);
			Assert.AreEqual("EMEA", capturedEntity.KeyAttributes["new_region"]);
		}

		// ── JSON payload ───────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithJson_ShouldUpsertRecord()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Key = "accountnumber=ACC001",
				Json = "{\"name\":\"Contoso Ltd\"}"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "accountnumber" },
				new StringAttributeMetadata { LogicalName = "name" });

			SetupUpsertResponse(recordId, recordCreated: true);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			_crmMock.Verify(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()), Times.Once);
		}

		// ── --id mode: record created ──────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithId_WhenRecordCreated_ShouldReturnSuccessWithCreatedFlag()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Id = recordId,
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "name" });

			SetupUpsertResponse(recordId, recordCreated: true);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(recordId, result["Id"]);
			Assert.AreEqual(true, result["RecordCreated"]);
		}

		// ── --id mode: entity Id is set on the request ─────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithId_ShouldSetEntityIdOnRequest()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Id = recordId,
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "name" });

			Entity? capturedEntity = null;
			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest r, CancellationToken _) =>
				{
					capturedEntity = ((UpsertRequest)r).Target;
					var response = new UpsertResponse();
					response.Results["RecordCreated"] = false;
					response.Results["Target"] = new EntityReference("account", recordId);
					return response;
				});

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedEntity);
			Assert.AreEqual(recordId, capturedEntity!.Id);
			Assert.AreEqual(0, capturedEntity.KeyAttributes.Count);
		}

		// ── --id mode: no KeyAttributes set ───────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithId_ShouldNotSetKeyAttributes()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Id = recordId,
				Plain = "name=Contoso Ltd"
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "name" });

			Entity? capturedEntity = null;
			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest r, CancellationToken _) =>
				{
					capturedEntity = ((UpsertRequest)r).Target;
					var response = new UpsertResponse();
					response.Results["RecordCreated"] = false;
					response.Results["Target"] = new EntityReference("account", recordId);
					return response;
				});

			await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsNotNull(capturedEntity);
			Assert.AreEqual(0, capturedEntity!.KeyAttributes.Count);
		}

		// ── --id mode: dry-run ─────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_WithIdAndDryRun_ShouldNotCallUpsert()
		{
			var recordId = Guid.NewGuid();
			var command = new UpsertCommand
			{
				Table = "account",
				Id = recordId,
				Plain = "name=Contoso Ltd",
				DryRun = true
			};

			SetupEntityMetadata("account",
				new StringAttributeMetadata { LogicalName = "name" });

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			_crmMock.Verify(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()), Times.Never);
			Assert.IsTrue(_output.ToString().Contains("Dry-run"));
			Assert.IsTrue(_output.ToString().Contains(recordId.ToString()));
		}

		#region Helpers

		private void SetupEntityMetadata(string logicalName, params AttributeMetadata[] attributes)
		{
			var entityMetadata = BuildEntityMetadata(logicalName, attributes);

			var retrieveEntityResponse = new RetrieveEntityResponse();
			retrieveEntityResponse.Results["EntityMetadata"] = entityMetadata;

			_crmMock.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is RetrieveEntityRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync(retrieveEntityResponse);
		}

		private void SetupUpsertResponse(Guid recordId, bool recordCreated)
		{
			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is UpsertRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync(() =>
				{
					var response = new UpsertResponse();
					response.Results["RecordCreated"] = recordCreated;
					response.Results["Target"] = new EntityReference("account", recordId);
					return response;
				});
		}

		private static EntityMetadata BuildEntityMetadata(string logicalName, AttributeMetadata[] attributes)
		{
			var metadata = new EntityMetadata();
			typeof(EntityMetadata)
				.GetProperty(nameof(EntityMetadata.LogicalName))!
				.SetValue(metadata, logicalName);
			typeof(EntityMetadata)
				.GetProperty(nameof(EntityMetadata.Attributes))!
				.SetValue(metadata, attributes);
			return metadata;
		}

		#endregion
	}
}
