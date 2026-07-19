using Greg.Xrm.Command.Commands.Data.Delete;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.Delete
{
	[TestClass]
	public class DeleteCommandExecutorTest
	{
		private OutputToMemory _output = null!;
		private Mock<IOrganizationServiceRepository> _repoMock = null!;
		private Mock<IOrganizationServiceAsync2> _crmMock = null!;
		private DeleteCommandExecutor _executor = null!;

		[TestInitialize]
		public void Setup()
		{
			_output = new OutputToMemory();
			_crmMock = new Mock<IOrganizationServiceAsync2>();
			_repoMock = new Mock<IOrganizationServiceRepository>();
			_repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(_crmMock.Object);
			_executor = new DeleteCommandExecutor(_output, _repoMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_HappyPath_ShouldCallDeleteAndReturnSuccess()
		{
			var recordId = Guid.NewGuid();
			var command = new DeleteCommand { Table = "contact", Id = recordId };

			SetupEntityMetadata("contact", "fullname");
			SetupRetrieveRecord(recordId, "contact", "fullname", "Mario Rossi");
			_crmMock.Setup(c => c.DeleteAsync("contact", recordId, It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(recordId, result["Id"]);
			_crmMock.Verify(c => c.DeleteAsync("contact", recordId, It.IsAny<CancellationToken>()), Times.Once);
			Assert.IsTrue(_output.ToString().Contains("Record deleted successfully"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WithDryRun_ShouldNotCallDelete()
		{
			var recordId = Guid.NewGuid();
			var command = new DeleteCommand { Table = "contact", Id = recordId, DryRun = true };

			SetupEntityMetadata("contact", "fullname");
			SetupRetrieveRecord(recordId, "contact", "fullname", "Mario Rossi");

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			_crmMock.Verify(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
			Assert.IsTrue(_output.ToString().Contains("Dry-run"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenTableNotFound_ShouldReturnFail()
		{
			var command = new DeleteCommand { Table = "nonexistenttable", Id = Guid.NewGuid() };

			_crmMock.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					new System.ServiceModel.FaultReason("Table not found")));

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("nonexistenttable"));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenRecordNotFound_ShouldReturnFail()
		{
			var recordId = Guid.NewGuid();
			var command = new DeleteCommand { Table = "contact", Id = recordId };

			SetupEntityMetadata("contact", "fullname");
			_crmMock.Setup(c => c.RetrieveAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<ColumnSet>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					new System.ServiceModel.FaultReason("Record not found")));

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains(recordId.ToString()));
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenDeleteThrowsFaultException_ShouldReturnFail()
		{
			var recordId = Guid.NewGuid();
			var command = new DeleteCommand { Table = "contact", Id = recordId };

			SetupEntityMetadata("contact", "fullname");
			SetupRetrieveRecord(recordId, "contact", "fullname", "Mario Rossi");
			_crmMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					new System.ServiceModel.FaultReason("SDK error")));

			var result = await _executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage!.Contains("Error deleting record"));
		}

		#region Helpers

		private void SetupEntityMetadata(string logicalName, string primaryNameAttribute)
		{
			var metadata = new EntityMetadata();
			typeof(EntityMetadata)
				.GetProperty(nameof(EntityMetadata.LogicalName))!
				.SetValue(metadata, logicalName);
			typeof(EntityMetadata)
				.GetProperty(nameof(EntityMetadata.PrimaryNameAttribute))!
				.SetValue(metadata, primaryNameAttribute);

			var response = new RetrieveEntityResponse();
			response.Results["EntityMetadata"] = metadata;

			_crmMock.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is RetrieveEntityRequest), It.IsAny<CancellationToken>()))
				.ReturnsAsync(response);
		}

		private void SetupRetrieveRecord(Guid id, string logicalName, string primaryNameAttribute, string primaryNameValue)
		{
			var entity = new Entity(logicalName, id);
			entity[primaryNameAttribute] = primaryNameValue;

			_crmMock.Setup(c => c.RetrieveAsync(logicalName, id, It.IsAny<ColumnSet>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(entity);
		}

		#endregion
	}
}
