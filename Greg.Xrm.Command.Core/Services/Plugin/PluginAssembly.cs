using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ComponentModel;
using System.IO.Packaging;

namespace Greg.Xrm.Command.Services.Plugin
{
	public class PluginAssembly : EntityWrapper
	{
		public PluginAssembly() : base("pluginassembly")
		{
			this.culture = "neutral";
			this.sourcetype = new OptionSetValue((int)CrmAssemblySourceType.Database);
			this.isolationmode = new OptionSetValue((int)CrmAssemblyIsolationMode.Sandbox);
		}

		protected PluginAssembly(Entity entity) : base(entity)
		{
		}

		public string name
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string version
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public EntityReference packageid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}

		public OptionSetValue sourcetype
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}

		public OptionSetValue isolationmode
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}

		public string culture
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string? publickeytoken
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string? content
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public EntityReference? solutionid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}



		public bool ismanaged
		{
  			get => Get<bool>();
			set => SetValue(value);
		}

		public class Repository : IPluginAssemblyRepository
		{
			private static readonly string[] columns = ["name", "version", "packageid", "sourcetype", "isolationmode", "culture", "publickeytoken", "ismanaged"];

			public async Task<PluginAssembly?> GetByNameAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("pluginassembly");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("name", ConditionOperator.Equal, name);
				query.TopCount = 1;
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var assembly = result.Entities.Select(x => new PluginAssembly(x)).FirstOrDefault();
				return assembly;
			}


			public async Task<PluginAssembly[]> GetByPackageIdAsync(IOrganizationServiceAsync2 crm, Guid packageId, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("pluginassembly");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("packageid", ConditionOperator.Equal, packageId);
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var assemblies = result.Entities.Select(x => new PluginAssembly(x)).ToArray();
				return assemblies;
			}

			public async Task<PluginAssembly[]> GetBySolutionIdAsync(IOrganizationServiceAsync2 crm, Guid solutionId, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("pluginassembly");
				query.ColumnSet.AddColumns(columns);
				query.NoLock = true;

				var link = query.AddLink("solutioncomponent", "pluginassemblyid", "objectid");
				link.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
				link.LinkCriteria.AddCondition("componenttype", ConditionOperator.Equal, 91); // 91 = PluginAssembly

				return await crm.RetrieveAllAsync(query, x => new PluginAssembly(x), cancellationToken);
			}

			public async Task<PluginAssembly[]> GetByGuidsAsync(IOrganizationServiceAsync2 crm, Guid[] ids, CancellationToken cancellationToken)
			{
				if (ids.Length == 0) return [];

				var query = new QueryExpression("pluginassembly");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.In, ids.Cast<object>().ToArray());
				query.NoLock = true;

				return await crm.RetrieveAllAsync(query, x => new PluginAssembly(x), cancellationToken);
			}

			public async Task<PluginAssembly[]> SearchByNameAsync(IOrganizationServiceAsync2 crm, string name, ConditionOperator op, CancellationToken cancellationToken)
			{
				// PluginAssembly has no full-text index, so Contains is not supported server-side.
				// Fall back to fetching all assemblies and filtering in memory.
				if (op == ConditionOperator.Contains)
				{
					var allQuery = new QueryExpression("pluginassembly");
					allQuery.ColumnSet.AddColumns(columns);
					allQuery.NoLock = true;
					var all = await crm.RetrieveAllAsync(allQuery, x => new PluginAssembly(x), cancellationToken);
					return [.. all
						.Where(p => p.name?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false)
						.OrderBy(p => p.name)];
				}

				var query = new QueryExpression("pluginassembly");
				query.ColumnSet.AddColumns(columns);
				query.Criteria.AddCondition("name", op, name);
				query.NoLock = true;
				query.AddOrder("name", OrderType.Ascending);

				return await crm.RetrieveAllAsync(query, x => new PluginAssembly(x), cancellationToken);
			}
		}
	}

	public enum CrmAssemblySourceType
	{
		[Description("Database")] Database = 0,
		[Description("Disk")] Disk = 1,
		[Description("GAC")] GAC = 2,
		[Description("Package")] Package = 4,
	}

	public enum CrmAssemblyIsolationMode
	{
		[Description("None")] None = 1,
		[Description("Sandbox")] Sandbox = 2,
	}
}
