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
			public async Task<PluginPackage?> GetByIdAsync(IOrganizationServiceAsync2 crm, string packageId, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("pluginpackage");
				query.ColumnSet.AddColumns("pluginpackageid", "name", "uniquename", "version", "content", "solutionid", "ismanaged");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, packageId);
				query.TopCount = 1;
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var pluginPackage = result.Entities.Select(x => new PluginPackage(x)).FirstOrDefault();
				return pluginPackage;
			}
		}

	}
}
