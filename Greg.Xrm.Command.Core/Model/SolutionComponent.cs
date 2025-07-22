using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public class SolutionComponent : EntityWrapper
	{
		public SolutionComponent(Entity entity) : base(entity)
		{
			this.TypeLabel = this.ComponentTypeName 
				?? this.SolutionComponentDefinitionPrimaryEntityName 
				?? GetSolutionComponentTypeName(componenttype.Value);
			this.Label = $"{this.TypeLabel}: {this.objectid}";
		}


		public string ComponentTypeName => this.GetFormatted(nameof(componenttype)) ?? string.Empty;

#pragma warning disable IDE1006 // Naming Styles
		public OptionSetValue componenttype => this.Get<OptionSetValue>();

		public EntityReference solutionid => this.Get<EntityReference>();

		public Guid objectid => this.Get<Guid>();

		public bool ismetadata => this.Get<bool>();

#pragma warning restore IDE1006 // Naming Styles

		public string SolutionComponentDefinitionName => this.GetAliased<string>("scd", "name") ?? string.Empty;
		public string SolutionComponentDefinitionPrimaryEntityName => this.GetAliased<string>("scd", "primaryentityname") ?? string.Empty;
		public int SolutionComponentDefinitionObjectTypeCode => this.GetAliased<int>("scd", "objecttypecode");


		public string Label { get; set; }

		public string TypeLabel { get; set; }


		public override string ToString()
		{
			return $"{ComponentTypeName} {objectid}";
		}


		private string GetSolutionComponentTypeName(int componentType)
		{
			return Enum.GetName(typeof(ComponentType), componentType) ?? this.componenttype.Value.ToString();
		}



		public class Repository : ISolutionComponentRepository
		{
			public async Task<List<SolutionComponent>> GetBySolutionIdAsync(IOrganizationServiceAsync2 crm, Guid solutionId)
			{
				var query = new QueryExpression("solutioncomponent");
				query.ColumnSet.AddColumns("componenttype", "rootsolutioncomponentid", "solutionid", "objectid", "ismetadata");
				query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
				var link = query.AddLink("solutioncomponentdefinition", "componenttype", "solutioncomponenttype", JoinOperator.LeftOuter);
				link.EntityAlias = "scd";
				link.Columns.AddColumns("name", "primaryentityname", "objecttypecode");

				query.PageInfo = new PagingInfo
				{
					PageNumber = 1,
					Count = 5000 // Adjust as needed
				};

				EntityCollection result;
				var components = new List<SolutionComponent>();
				do
				{
					result = await crm.RetrieveMultipleAsync(query);

					if (result.Entities.Count > 0)
					{
						components.AddRange(result.Entities.Select(e => new SolutionComponent(e)));
					}

					if (result.MoreRecords)
					{
						query.PageInfo.PageNumber++;
						query.PageInfo.PagingCookie = result.PagingCookie;
					}

				} while (result.MoreRecords);

				return components;
			}
		}
	}
}
