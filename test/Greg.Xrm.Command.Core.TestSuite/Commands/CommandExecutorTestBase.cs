using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands
{
	public abstract class CommandExecutorTestBase
	{
		/// <summary>
		/// EntityWrapper clears the attributes of the saved entity right after the
		/// create/update call, so a capture must clone the entity instead of keeping a reference.
		/// </summary>
		protected static Entity Clone(Entity entity)
		{
			var clone = new Entity(entity.LogicalName, entity.Id);
			foreach (var attribute in entity.Attributes)
			{
				clone[attribute.Key] = attribute.Value;
			}
			return clone;
		}


		protected Mock<IOrganizationServiceRepository> OrganizationServiceRepositoryMock { get; }
		protected Mock<IOrganizationServiceAsync2> OrganizationServiceMock { get; }
		protected OutputToMemory Output { get; }

		protected CommandExecutorTestBase()
		{
			this.OrganizationServiceMock = new Mock<IOrganizationServiceAsync2>();
			this.OrganizationServiceRepositoryMock = new Mock<IOrganizationServiceRepository>();
			this.OrganizationServiceRepositoryMock
				.Setup(m => m.GetCurrentConnectionAsync())
				.ReturnsAsync(this.OrganizationServiceMock.Object);

			this.Output = new OutputToMemory();
		}
	}
}
