using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services
{
	public class ObjectTypeCodeFinder : IObjectTypeCodeFinder
	{
		private Dictionary<string, int> cache = new Dictionary<string, int>();
		

		public async Task<int> GetObjectTypeCodeForTableAsync(IOrganizationServiceAsync2 crm, string tableLogicalName, CancellationToken cancellationToken)
		{
			if (cache.TryGetValue(tableLogicalName.ToLowerInvariant(), out var objectTypeCode))
			{
				return objectTypeCode;
			}



			var request = new Microsoft.Xrm.Sdk.Messages.RetrieveEntityRequest
			{
				LogicalName = tableLogicalName,
				EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity
			};

			var response = (Microsoft.Xrm.Sdk.Messages.RetrieveEntityResponse)await crm.ExecuteAsync(request, cancellationToken);

			objectTypeCode = response.EntityMetadata.ObjectTypeCode ?? throw new ArgumentException($"ObjectTypeCode not found for table '{tableLogicalName}'.");
			cache[tableLogicalName.ToLowerInvariant()] = objectTypeCode;

			return objectTypeCode;
		}
	}
}
