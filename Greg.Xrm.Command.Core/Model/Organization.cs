using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Xml;
using System.Xml.Linq;

namespace Greg.Xrm.Command.Model
{
	public class Organization : EntityWrapper
	{
		private Organization(Entity entity) : base(entity) { }

		public string? orgdborgsettings => this.Get<string>();

		public bool OwnershipAcrossBusinessUnitsEnabled
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.orgdborgsettings)) return false;
				var document = XDocument.Parse(this.orgdborgsettings);
				var setting = document.Descendants().SingleOrDefault(element => element.Name.LocalName == "EnableOwnershipAcrossBusinessUnits");
				return setting != null && XmlConvert.ToBoolean(setting.Value.Trim().ToLowerInvariant());
			}
		}

		public class Repository
		{
			public async Task<Organization> GetAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("organization") { ColumnSet = new ColumnSet("orgdborgsettings"), TopCount = 1 };
				var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (response.Entities.Count != 1)
					throw new InvalidOperationException("Unable to retrieve the organization's record ownership across business units setting.");
				return new Organization(response.Entities[0]);
			}
		}
	}
}