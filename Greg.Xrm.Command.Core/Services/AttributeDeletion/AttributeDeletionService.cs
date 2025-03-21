using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
	public class AttributeDeletionService(
		IOutput output,
		IEnumerable<IAttributeDeletionStrategy> strategies)
		: IAttributeDeletionService
	{
		public async Task DeleteAttributeAsync(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, DependencyList dependencies, bool? simulation = false)
		{
			foreach(var strategy in strategies)
			{
				try
				{
					await strategy.HandleAsync(crm, attribute, dependencies);
				}
				catch(AttributeDeletionException ex)
				{
					output.WriteLine($"Error while trying to remove a dependency on {strategy.GetType().Name}: " + ex.Message, ConsoleColor.Red);
				}

			}

			await PublishAll(crm);
		}


		private async Task PublishAll(IOrganizationServiceAsync2 crm)
		{
			output.Write("Publishing all...");
			await crm.ExecuteAsync(new PublishAllXmlRequest());
			output.WriteLine("Done", ConsoleColor.Green);
		}
	}
}
