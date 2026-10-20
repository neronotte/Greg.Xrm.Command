using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
#pragma warning disable IDE1006 // Naming Styles
	public class ConnectionReference : EntityWrapper
	{
		public ConnectionReference(Entity entity) : base(entity)
		{
		}

		public string? connectionreferencelogicalname
		{
			get => Get<string>();
		}


		public class Repository : IConnectionReferenceRepository
		{
			public async Task<IReadOnlyCollection<string>> GetExistingLogicalNamesAsync(IOrganizationServiceAsync2 crm, IReadOnlyCollection<string> logicalNames)
			{
				if (logicalNames.Count == 0)
					return [];

				var query = new QueryExpression("connectionreference");
				query.ColumnSet.AddColumns(nameof(connectionreferencelogicalname));
				query.Criteria.AddCondition(nameof(connectionreferencelogicalname), ConditionOperator.In, logicalNames.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities
					.Select(e => new ConnectionReference(e).connectionreferencelogicalname)
					.Where(n => !string.IsNullOrWhiteSpace(n))
					.Select(n => n!)
					.ToHashSet(StringComparer.OrdinalIgnoreCase);
			}
		}
	}
#pragma warning restore IDE1006 // Naming Styles
}
