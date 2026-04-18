using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public class PluginPackage : EntityWrapper
	{
		protected PluginPackage(Entity entity) : base(entity)
		{
		}

		public PluginPackage() : base(new Entity("pluginpackage"))
		{
		}

#pragma warning disable IDE1006 // Naming Styles
		public string name
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string content
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string version
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string uniquename
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public bool ismanaged
		{
			get => Get<bool>();
			set => SetValue(value);
		}

		public EntityReference solutionid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}
#pragma warning restore IDE1006 // Naming Styles


		public class Repository : IPluginPackageRepository
		{
			private static readonly string[] columns = ["pluginpackageid", "name", "uniquename", "version", "ismanaged"];

			public async Task<PluginPackage?> GetByIdAsync(IOrganizationServiceAsync2 crm, string packageId, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("pluginpackage");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, packageId);
				query.TopCount = 1;
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var pluginPackage = result.Entities.Select(x => new PluginPackage(x)).FirstOrDefault();
				return pluginPackage;
			}

			public async Task<PluginPackage[]> GetByGuidsAsync(IOrganizationServiceAsync2 crm, Guid[] ids, CancellationToken cancellationToken)
			{
				if (ids.Length == 0) return [];

				var query = new QueryExpression("pluginpackage");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("pluginpackageid", ConditionOperator.In, ids.Cast<object>().ToArray());
				query.NoLock = true;

				return await crm.RetrieveAllAsync(query, x => new PluginPackage(x), cancellationToken);
			}

			public async Task<PluginPackage[]> SearchByNameAsync(IOrganizationServiceAsync2 crm, string name, ConditionOperator op, CancellationToken cancellationToken)
			{
				// PluginPackage has no full-text index, so Contains is not supported server-side.
				// Fall back to fetching all packages and filtering in memory.
				if (op == ConditionOperator.Contains)
				{
					var allQuery = new QueryExpression("pluginpackage");
					allQuery.ColumnSet.AddColumns(columns);
					allQuery.NoLock = true;
					var all = await crm.RetrieveAllAsync(allQuery, x => new PluginPackage(x), cancellationToken);
					return [.. all
						.Where(p => p.name?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false)
						.OrderBy(p => p.name)];
				}

				var query = new QueryExpression("pluginpackage");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("name", op, name);
				query.NoLock = true;
				query.AddOrder("name", OrderType.Ascending);

				return await crm.RetrieveAllAsync(query, x => new PluginPackage(x), cancellationToken);
			}
		}

	}
}
