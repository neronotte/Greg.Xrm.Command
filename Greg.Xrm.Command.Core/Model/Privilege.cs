using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class Privilege : EntityWrapper
	{
		private Privilege(Entity entity) : base(entity)
		{
		}

		public string name => this.Get<string>();
		public bool canbebasic => this.Get<bool>();
		public bool canbelocal => this.Get<bool>();
		public bool canbedeep => this.Get<bool>();
		public bool canbeglobal => this.Get<bool>();

		public bool Supports(PrivilegeDepth depth) => depth switch
		{
			PrivilegeDepth.Basic => this.canbebasic,
			PrivilegeDepth.Local => this.canbelocal,
			PrivilegeDepth.Deep => this.canbedeep,
			PrivilegeDepth.Global => this.canbeglobal,
			_ => false
		};

        public PrivilegeDepth[] GetAllowedLevels()
        {
            return [.. Enum.GetValues<PrivilegeDepth>().Where(this.Supports)]; 
        }

		public class Repository
		{
			public async Task<Privilege?> GetByNameAsync(IOrganizationServiceAsync2 crm, string name, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("privilege")
				{
					ColumnSet = new ColumnSet("name", "canbebasic", "canbelocal", "canbedeep", "canbeglobal"),
					TopCount = 2,
					NoLock = true
				};
				query.Criteria.AddCondition("name", ConditionOperator.Equal, name);
				var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
				var matches = response.Entities.Where(entity => string.Equals(entity.GetAttributeValue<string>("name"), name, StringComparison.OrdinalIgnoreCase)).ToList();
				return matches.Count == 1 ? new Privilege(matches[0]) : null;
			}
		}
	}
}