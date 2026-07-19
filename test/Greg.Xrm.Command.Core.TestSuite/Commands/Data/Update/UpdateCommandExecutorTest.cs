using Greg.Xrm.Command.Commands.Data.Update;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.Update
{
	[TestClass]
	public class UpdateCommandExecutorTest
	{
		private OutputToMemory _output = null!;
		private Mock<IOrganizationServiceRepository> _repoMock = null!;
		private Mock<IOrganizationServiceAsync2> _crmMock = null!;
		private UpdateCommandExecutor _executor = null!;

		[TestInitialize]
		public void Setup()
		{
			_output = new OutputToMemory();
			_crmMock = new Mock<IOrganizationServiceAsync2>();
			_repoMock = new Mock<IOrganizationServiceRepository>();
			_repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(_crmMock.Object);
			_executor = new UpdateCommandExecutor(_output, _repoMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_HappyPath_WithPlain_ShouldCallUpdateAndReturnSuccess()
		{
			var recordId = Guid.NewGuid();
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = recordId,
				Plain = "firstname=Mario"
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });
			_crmMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.UpdateAsync(It.Is<Entity>(e =>
				e.LogicalName == "contact" &&
				e.Id == recordId &&
				(string)e["firstname"] == "Mario"), It.IsAny<CancellationToken>()), Times.Once);

			var outputText = _output.ToString();
			Assert.IsTrue(outputText.Contains("Record updated successfully"));
			Assert.IsTrue(outputText.Contains(recordId.ToString()));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithDryRun_ShouldNotCallUpdate()
		{
			var recordId = Guid.NewGuid();
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = recordId,
				Plain = "firstname=Mario",
				DryRun = true
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), Times.Never);
			Assert.IsTrue(_output.ToString().Contains("Dry-run"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenTableNotFound_ShouldReturnFail()
		{
			var command = new UpdateCommand
			{
				Table = "nonexistenttable",
				Id = Guid.NewGuid(),
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
		public async Task ExecuteAsync_WithConversionErrors_ShouldFailWithoutCallingUpdate()
		{
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = Guid.NewGuid(),
				Plain = "numberofemployees=not-a-number"
			};

			SetupEntityMetadata("contact", new IntegerAttributeMetadata { LogicalName = "numberofemployees" });

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			_crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldAlwaysSetRecordId()
		{
			var recordId = Guid.NewGuid();
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = recordId,
				Plain = "firstname=Mario"
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });

			Entity? capturedEntity = null;
			_crmMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()))
				.Callback<Entity, CancellationToken>((e, ct) => capturedEntity = e)
				.Returns(Task.CompletedTask);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			Assert.IsNotNull(capturedEntity);
			Assert.AreEqual(recordId, capturedEntity!.Id);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenUpdateThrowsFaultException_ShouldReturnFail()
		{
			var command = new UpdateCommand
			{
				Table = "contact",
				Id = Guid.NewGuid(),
				Plain = "firstname=Mario"
			};

			SetupEntityMetadata("contact", new StringAttributeMetadata { LogicalName = "firstname" });

			_crmMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					new System.ServiceModel.FaultReason("SDK error")));

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error updating record"));
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
