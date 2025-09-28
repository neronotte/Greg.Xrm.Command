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
			public async Task<PluginAssembly?> GetByNameAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("pluginassembly");
				query.ColumnSet.AddColumns("name", "version", "packageid", "sourcetype", "isolationmode", "culture", "publickeytoken", "ismanaged", "content", "solutionid");
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
				query.ColumnSet.AddColumns("name", "version", "packageid", "sourcetype", "isolationmode", "culture", "publickeytoken", "ismanaged", "content", "solutionid");
				query.Criteria.AddCondition("packageid", ConditionOperator.Equal, packageId);
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var assemblies = result.Entities.Select(x => new PluginAssembly(x)).ToArray();
				return assemblies;
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
