using Greg.Xrm.Command.Model;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.ComponentResolution
{
	public class ResolverForGlobalOptionSets(ILogger log) : IComponentResolver
	{

		public async Task ResolveAsync(IReadOnlyCollection<SolutionComponent> componentList, IOrganizationServiceAsync2 crm)
		{
			var globalOptionSetList = componentList
				.Where(c => c.componenttype.Value == (int)ComponentType.OptionSet)
				.ToList();

			if (globalOptionSetList.Count == 0) return;


			foreach (var item in globalOptionSetList)
			{
				try
				{
					var request = new RetrieveOptionSetRequest();
					request.MetadataId = item.objectid;

					var response = (RetrieveOptionSetResponse)await crm.ExecuteAsync(request);

					item.Label = GetLabel(response.OptionSetMetadata);
				}
				catch(Exception ex)
				{
					log.LogError(ex, "Error retrieving option set metadata: {Message}", ex.Message);
				}
			}
		}

		private static string GetLabel(OptionSetMetadataBase entity)
		{
			return $"{entity.DisplayName?.UserLocalizedLabel?.Label} ({entity.Name})"
				.Replace("()", string.Empty)
				.Trim();
		}
	}
}
