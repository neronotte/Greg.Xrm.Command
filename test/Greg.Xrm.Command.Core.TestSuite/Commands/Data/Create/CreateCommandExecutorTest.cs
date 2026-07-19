using Greg.Xrm.Command.Commands.Data.Create;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.Create
{
	[TestClass]
	public class CreateCommandExecutorTest
	{
		private OutputToMemory _output = null!;
		private Mock<IOrganizationServiceRepository> _repoMock = null!;
		private Mock<IOrganizationServiceAsync2> _crmMock = null!;
		private CreateCommandExecutor _executor = null!;

		[TestInitialize]
		public void Setup()
		{
			_output = new OutputToMemory();
			_crmMock = new Mock<IOrganizationServiceAsync2>();
			_repoMock = new Mock<IOrganizationServiceRepository>();
			_repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(_crmMock.Object);
			_executor = new CreateCommandExecutor(_output, _repoMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_HappyPath_WithPlain_ShouldCallCreateAndReturnSuccess()
		{
			var expectedId = Guid.NewGuid();
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario"
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });
			_crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedId);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "contact" &&
				(string)e["firstname"] == "Mario"), It.IsAny<CancellationToken>()), Times.Once);

			var outputText = _output.ToString();
			Assert.IsTrue(outputText.Contains("Record created successfully"));
			Assert.IsTrue(outputText.Contains(expectedId.ToString()));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithReturn_ShouldCallRetrieve()
		{
			var expectedId = Guid.NewGuid();
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario",
				Return = "firstname,lastname"
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });
			_crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedId);

			var retrievedEntity = new Entity("contact") { Id = expectedId };
			retrievedEntity["firstname"] = "Mario";
			retrievedEntity["lastname"] = "Rossi";
			_crmMock.Setup(c => c.RetrieveAsync("contact", expectedId, It.IsAny<ColumnSet>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(retrievedEntity);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.RetrieveAsync("contact", expectedId, It.IsAny<ColumnSet>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithDryRun_ShouldNotCallCreate()
		{
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario",
				DryRun = true
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), Times.Never);
			Assert.IsTrue(_output.ToString().Contains("Dry-run"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenTableNotFound_ShouldReturnFail()
		{
			var command = new CreateCommand
			{
				Table = "nonexistenttable",
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

		[TestMethod]
		public async Task ExecuteAsync_WithConversionErrors_ShouldFailWithoutCallingCreate()
		{
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "numberofemployees=not-a-number"
			};

			SetupEntityMetadata("contact", new IntegerAttributeMetadata { LogicalName = "numberofemployees" });

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			_crmMock.Verify(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithFileField_ShouldEmitWarningAndCreateRecord()
		{
			var expectedId = Guid.NewGuid();
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario;new_attachment=some-value"
			};

			SetupEntityMetadata("contact",
				new StringAttributeMetadata { LogicalName = "firstname" },
				new FileAttributeMetadata { LogicalName = "new_attachment" });

			_crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedId);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), Times.Once);
			Assert.IsTrue(_output.ToString().Contains("Warning"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithCustomId_ShouldAssignIdToEntity()
		{
			var customId = Guid.NewGuid();
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario",
				Id = customId
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });

			Entity? capturedEntity = null;
			_crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()))
				.Callback<Entity, CancellationToken>((e, ct) => capturedEntity = e)
				.ReturnsAsync(customId);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			Assert.IsNotNull(capturedEntity);
			Assert.AreEqual(customId, capturedEntity!.Id);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenCreateThrowsFaultException_ShouldReturnFail()
		{
			var command = new CreateCommand
			{
				Table = "contact",
				Plain = "firstname=Mario"
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });

			_crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					new System.ServiceModel.FaultReason("SDK error")));

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error creating record"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithJson_ShouldCreateRecord()
		{
			var expectedId = Guid.NewGuid();
			var command = new CreateCommand
			{
				Table = "contact",
				Json = "{\"firstname\":\"Mario\"}"
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });
			_crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedId);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.CreateAsync(It.Is<Entity>(e =>
				(string)e["firstname"] == "Mario"), It.IsAny<CancellationToken>()), Times.Once);
		}

		#region Helpers

		private void SetupEntityMetadata(string logicalName, params AttributeMetadata[] attributes)
		{
			var entityMetadata = BuildEntityMetadata(logicalName, attributes);

			var response = new RetrieveEntityResponse();
			response.Results["EntityMetadata"] = entityMetadata;

			_crmMock.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is RetrieveEntityRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync(response);
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
