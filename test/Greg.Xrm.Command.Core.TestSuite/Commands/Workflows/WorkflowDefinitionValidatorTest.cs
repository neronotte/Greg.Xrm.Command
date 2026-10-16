using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class WorkflowDefinitionValidatorTest : CommandExecutorTestBase
	{
		private readonly WorkflowDefinitionValidator validator;
		private readonly Mock<IConnectionReferenceRepository> connectionReferenceRepositoryMock = new();

		private const string ValidDefinition = "{\"properties\":{\"connectionReferences\":{\"shared_a\":{\"connection\":{\"connectionReferenceLogicalName\":\"new_a_123\"}}},\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\",\"kind\":\"Button\"}},\"actions\":{\"Step1\":{\"type\":\"Compose\",\"runAfter\":{},\"inputs\":\"x\"}}}},\"schemaVersion\":\"1.0.0.0\"}";

		public WorkflowDefinitionValidatorTest()
		{
			this.validator = new WorkflowDefinitionValidator(
				this.Output,
				this.connectionReferenceRepositoryMock.Object);

			this.connectionReferenceRepositoryMock
				.Setup(r => r.GetExistingLogicalNamesAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<IReadOnlyCollection<string>>()))
				.ReturnsAsync(["new_a_123"]);

			this.OrganizationServiceMock
				.Setup(s => s.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(Guid.NewGuid());
		}


		[TestMethod]
		public async Task ValidateAsync_ShouldPass_AndCleanUpTheProbe()
		{
			var updates = new List<Entity>();
			this.OrganizationServiceMock
				.Setup(s => s.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => updates.Add(Clone(e)))
				.Returns(Task.CompletedTask);

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, ValidDefinition, CancellationToken.None);

			Assert.IsNull(result, result?.ErrorMessage);
			this.OrganizationServiceMock.Verify(s => s.CreateAsync(It.IsAny<Entity>()), Times.Once, "The probe flow must be created.");
			Assert.AreEqual(2, updates.Count, "The probe must be activated and deactivated.");
			Assert.AreEqual((int)Workflow.State.Activated, updates[0].GetAttributeValue<OptionSetValue>("statecode").Value, "The first transition must activate the probe, that is what makes the engine validate the template.");
			Assert.AreEqual((int)Workflow.Status.Activated, updates[0].GetAttributeValue<OptionSetValue>("statuscode").Value);
			Assert.AreEqual((int)Workflow.State.Draft, updates[1].GetAttributeValue<OptionSetValue>("statecode").Value, "The second transition must put the probe back to draft.");
			Assert.AreEqual((int)Workflow.Status.Draft, updates[1].GetAttributeValue<OptionSetValue>("statuscode").Value);
			this.OrganizationServiceMock.Verify(s => s.DeleteAsync("workflow", It.IsAny<Guid>()), Times.Once, "The probe must be removed.");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldReturnTheValidationResult_WhenTheProbeCleanupFails()
		{
			this.OrganizationServiceMock
				.Setup(s => s.DeleteAsync("workflow", It.IsAny<Guid>()))
				.ThrowsAsync(new InvalidOperationException("transport broke"));

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, ValidDefinition, CancellationToken.None);

			Assert.IsNull(result, "A failed probe cleanup must not hide a successful validation.");
			StringAssert.Contains(this.Output.ToString(), "remove it manually", "The user must be told to remove the probe.");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldFail_WhenTheProbeCannotBeCreated()
		{
			this.OrganizationServiceMock
				.Setup(s => s.CreateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "no permission" }, new FaultReason("no permission")));

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, ValidDefinition, CancellationToken.None);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unable to create the validation probe");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldFail_WhenTheProbeCannotBeDeactivated_AndStillRemoveIt()
		{
			this.OrganizationServiceMock
				.SetupSequence(s => s.UpdateAsync(It.IsAny<Entity>()))
				.Returns(Task.CompletedTask)
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "stuck" }, new FaultReason("stuck")));

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, ValidDefinition, CancellationToken.None);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unable to deactivate the validation probe");
			this.OrganizationServiceMock.Verify(s => s.DeleteAsync("workflow", It.IsAny<Guid>()), Times.Once, "The probe must be removed even when the deactivation fails.");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldFailLocally_BeforeAnyServerCall()
		{
			var broken = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"Broken\":{\"type\":\"Compose\",\"runAfter\":{\"Does_Not_Exist\":[\"Succeeded\"]},\"inputs\":\"x\"}}}}}";

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, broken, CancellationToken.None);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Does_Not_Exist");
			this.OrganizationServiceMock.Verify(s => s.CreateAsync(It.IsAny<Entity>()), Times.Never, "A structurally broken definition must fail before anything is written.");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldFail_WhenAConnectionReferenceIsMissing()
		{
			this.connectionReferenceRepositoryMock
				.Setup(r => r.GetExistingLogicalNamesAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<IReadOnlyCollection<string>>()))
				.ReturnsAsync([]);

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, ValidDefinition, CancellationToken.None);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "new_a_123");
			this.OrganizationServiceMock.Verify(s => s.CreateAsync(It.IsAny<Entity>()), Times.Never, "A missing connection reference must fail before anything is written.");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldFail_WhenTheEngineRejectsTheDefinition_AndStillRemoveTheProbe()
		{
			this.OrganizationServiceMock
				.Setup(s => s.UpdateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "TemplateValidationError: bad runAfter." }, new FaultReason("TemplateValidationError: bad runAfter.")));

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, ValidDefinition, CancellationToken.None);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "rejected");
			this.OrganizationServiceMock.Verify(s => s.DeleteAsync("workflow", It.IsAny<Guid>()), Times.Once, "The probe must be removed also when the engine rejects the definition.");
		}

		[TestMethod]
		public async Task ValidateAsync_ShouldMuzzleTheProbe_ButNotTheOriginal()
		{
			var scheduled = "{\"properties\":{\"connectionReferences\":{},\"definition\":{\"triggers\":{\"Every_Day\":{\"type\":\"Recurrence\",\"recurrence\":{\"frequency\":\"Day\",\"interval\":1}}},\"actions\":{\"Step1\":{\"type\":\"Compose\",\"runAfter\":{},\"inputs\":\"x\"}}}},\"schemaVersion\":\"1.0.0.0\"}";
			Entity? probe = null;
			this.OrganizationServiceMock
				.Setup(s => s.CreateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => probe = Clone(e))
				.ReturnsAsync(Guid.NewGuid());

			var result = await validator.ValidateAsync(this.OrganizationServiceMock.Object, scheduled, CancellationToken.None);

			Assert.IsNull(result, result?.ErrorMessage);
			Assert.IsNotNull(probe);
			StringAssert.Contains(probe.GetAttributeValue<string>("clientdata"), "@false", "The probe of a scheduled flow must not be able to fire while it is activated.");
			StringAssert.Contains(probe.GetAttributeValue<string>("name"), "pacx validation probe");
		}
	}
}
