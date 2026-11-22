using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class UpdateCommandExecutorTests
	{
		[TestMethod]
		public async Task UpdatesRequestedPropertiesAndUsesSpecialRequestFlags()
		{
			var table = new EntityMetadata
			{
				LogicalName = "new_table",
				IsAuditEnabled = new BooleanManagedProperty(true),
				IsDuplicateDetectionEnabled = new BooleanManagedProperty(true),
				IsMailMergeEnabled = new BooleanManagedProperty(true),
				IsConnectionsEnabled = new BooleanManagedProperty(false),
				IsValidForQueue = new BooleanManagedProperty(false),
				IsQuickCreateEnabled = true
			};
			var (executor, requests) = CreateExecutor(table);
			var result = await executor.ExecuteAsync(new UpdateCommand
			{
				SchemaName = "new_table", HasNotes = true, HasFeedback = true, Activities = true,
				Color = "#336699", DuplicateDetection = false, CustomHelp = true,
				HelpUrl = "https://example.com/help", Retention = true, MailMerge = false,
				DocumentManagement = true, Connections = true, AccessTeams = true, Offline = true,
				Queue = true, AutoRouteToOwnerQueue = true, SolutionName = "MySolution"
			}, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var request = requests.OfType<UpdateEntityRequest>().Single();
			Assert.AreEqual(true, request.HasNotes);
			Assert.AreEqual(true, request.HasFeedback);
			Assert.AreEqual(true, request.HasActivities);
			Assert.AreEqual("MySolution", request.SolutionUniqueName);
			Assert.IsTrue(request.MergeLabels);
			Assert.AreEqual("#336699", request.Entity.EntityColor);
			Assert.AreEqual(false, request.Entity.IsDuplicateDetectionEnabled.Value);
			Assert.AreEqual(true, request.Entity.EntityHelpUrlEnabled);
			Assert.AreEqual("https://example.com/help", request.Entity.EntityHelpUrl);
			Assert.AreEqual(true, request.Entity.IsRetentionEnabled);
			Assert.AreEqual(false, request.Entity.IsMailMergeEnabled.Value);
			Assert.AreEqual(true, request.Entity.IsDocumentManagementEnabled);
			Assert.AreEqual(true, request.Entity.IsConnectionsEnabled.Value);
			Assert.AreEqual(true, request.Entity.AutoCreateAccessTeams);
			Assert.AreEqual(true, request.Entity.IsAvailableOffline);
			Assert.AreEqual(true, request.Entity.IsValidForQueue.Value);
			Assert.AreEqual(true, request.Entity.AutoRouteToOwnerQueue);
			Assert.IsTrue(request.Entity.IsAuditEnabled.Value);
			Assert.AreEqual(true, request.Entity.IsQuickCreateEnabled);
		}

		[TestMethod]
		public async Task UnchangedPropertiesDoNotIssueUpdate()
		{
			var (executor, requests) = CreateExecutor(new EntityMetadata { LogicalName = "new_table", HasNotes = true });
			var result = await executor.ExecuteAsync(new UpdateCommand { SchemaName = "new_table", HasNotes = true }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, requests.OfType<UpdateEntityRequest>().Count());
		}

		[TestMethod]
		public async Task UpdatesExistingOptionsWithoutChangingOtherValues()
		{
			var table = new EntityMetadata
			{
				IsAuditEnabled = new BooleanManagedProperty(true),
				ChangeTrackingEnabled = false,
				IsQuickCreateEnabled = true,
				HasEmailAddresses = false,
				EntityColor = "#123456"
			};
			var (executor, requests) = CreateExecutor(table);
			var result = await executor.ExecuteAsync(new UpdateCommand
			{
				SchemaName = "new_table",
				IsAuditEnabled = false,
				ChangeTrackingEnabled = true,
				IsQuickCreateEnabled = false,
				HasEmailAddresses = true
			}, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var update = requests.OfType<UpdateEntityRequest>().Single();
			Assert.IsFalse(update.Entity.IsAuditEnabled.Value);
			Assert.AreEqual(true, update.Entity.ChangeTrackingEnabled);
			Assert.AreEqual(false, update.Entity.IsQuickCreateEnabled);
			Assert.AreEqual(true, update.Entity.HasEmailAddresses);
			Assert.AreEqual("#123456", update.Entity.EntityColor);
			Assert.IsFalse(update.Parameters.ContainsKey("SolutionUniqueName"));
		}

		[TestMethod]
		[DataRow("changeTracking")]
		[DataRow("connections")]
		[DataRow("queue")]
		public async Task RejectsDisablingEnabledCapabilities(string capability)
		{
			var table = new EntityMetadata
			{
				ChangeTrackingEnabled = true,
				IsConnectionsEnabled = new BooleanManagedProperty(true),
				IsValidForQueue = new BooleanManagedProperty(true)
			};
			var command = new UpdateCommand { SchemaName = "new_table" };
			if (capability == "changeTracking") command.ChangeTrackingEnabled = false;
			if (capability == "connections") command.Connections = false;
			if (capability == "queue") command.Queue = false;
			var (executor, requests) = CreateExecutor(table);

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, requests.OfType<UpdateEntityRequest>().Count());
		}

		[TestMethod]
		public async Task RejectsRoutingWithoutQueue()
		{
			var (executor, requests) = CreateExecutor(new EntityMetadata { IsValidForQueue = new BooleanManagedProperty(false) });
			var result = await executor.ExecuteAsync(new UpdateCommand { SchemaName = "new_table", AutoRouteToOwnerQueue = true }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, requests.OfType<UpdateEntityRequest>().Count());
		}

		[TestMethod]
		public async Task UpdatesPrimaryImageWithSolutionAndPreservesLabels()
		{
			var (executor, requests) = CreateExecutor(new EntityMetadata(), new ImageAttributeMetadata { IsPrimaryImage = false });
			var result = await executor.ExecuteAsync(new UpdateCommand { SchemaName = "new_table", PrimaryImage = "new_image", SolutionName = "MySolution" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var update = requests.OfType<UpdateAttributeRequest>().Single();
			Assert.AreEqual(true, ((ImageAttributeMetadata)update.Attribute).IsPrimaryImage);
			Assert.AreEqual("MySolution", update.SolutionUniqueName);
			Assert.IsTrue(update.MergeLabels);
			Assert.AreEqual(0, requests.OfType<UpdateEntityRequest>().Count());
		}

		[TestMethod]
		public async Task AlreadyPrimaryImageDoesNotIssueUpdate()
		{
			var (executor, requests) = CreateExecutor(new EntityMetadata(), new ImageAttributeMetadata { IsPrimaryImage = true });
			var result = await executor.ExecuteAsync(new UpdateCommand { SchemaName = "new_table", PrimaryImage = "new_image" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, requests.OfType<UpdateAttributeRequest>().Count());
		}

		[TestMethod]
		public async Task RejectsNonImageBeforeUpdatingTable()
		{
			var (executor, requests) = CreateExecutor(new EntityMetadata(), new StringAttributeMetadata());
			var result = await executor.ExecuteAsync(new UpdateCommand { SchemaName = "new_table", PrimaryImage = "new_text", Color = "#123456" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, requests.OfType<UpdateEntityRequest>().Count());
		}

		[TestMethod]
		public async Task ReportsPartialSuccessWhenImageUpdateFails()
		{
			var (executor, requests) = CreateExecutor(new EntityMetadata(), new ImageAttributeMetadata { IsPrimaryImage = false }, failImageUpdate: true);
			var result = await executor.ExecuteAsync(new UpdateCommand { SchemaName = "new_table", Color = "#123456", PrimaryImage = "new_image" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "table was updated");
			Assert.AreEqual(1, requests.OfType<UpdateEntityRequest>().Count());
		}

		private static (UpdateCommandExecutor executor, List<OrganizationRequest> requests) CreateExecutor(
			EntityMetadata table, AttributeMetadata? attribute = null, bool failImageUpdate = false)
		{
			var requests = new List<OrganizationRequest>();
			var crm = new Mock<IOrganizationServiceAsync2>();
			crm.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) =>
				{
					requests.Add(request);
					if (request is RetrieveEntityRequest)
						return new RetrieveEntityResponse { Results = { ["EntityMetadata"] = table } };
					if (request is RetrieveAttributeRequest)
						return new RetrieveAttributeResponse { Results = { ["AttributeMetadata"] = attribute! } };
					if (request is UpdateAttributeRequest && failImageUpdate)
						throw new InvalidOperationException("Image update failed");
					return request is UpdateAttributeRequest ? new UpdateAttributeResponse() : new UpdateEntityResponse();
				});
			var repository = new Mock<IOrganizationServiceRepository>();
			repository.Setup(x => x.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);
			return (new UpdateCommandExecutor(new OutputToMemory(), repository.Object), requests);
		}
	}
}
