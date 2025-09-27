using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	public class PluginType : EntityWrapper
	{
		protected PluginType(Entity entity) : base(entity)
		{
		}

		public PluginType() : base("plugintype")
		{
			this.friendlyname = Guid.NewGuid().ToString();
		}


		public string? name
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? typename
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? culture
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? friendlyname
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public bool ismanaged
		{
			get => Get<bool>();
			set => SetValue(value);
		}
		public bool isworkflowactivity
		{
			get => Get<bool>();
			set => SetValue(value);
		}
		public string? plugintypeexportkey
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? publickeytoken
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public string? version
		{
			get => Get<string>();
			set => SetValue(value);
		}
		public EntityReference? pluginassemblyid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}


		public class Repository : IPluginTypeRepository
		{
			public async Task<PluginType[]> GetByAssemblyId(IOrganizationServiceAsync2 crm, Guid assemblyId, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("plugintype");
				query.ColumnSet.AddColumns("plugintypeid", "name", "typename", "culture", "friendlyname", "ismanaged", "isworkflowactivity", "plugintypeexportkey", "publickeytoken", "version", "pluginassemblyid");
				query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assemblyId);
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(e => new PluginType(e)).ToArray();
			}
		}
	}
}
