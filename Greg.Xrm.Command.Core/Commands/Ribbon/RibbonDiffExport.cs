using System.Xml.Linq;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	internal static class RibbonDiffExport
	{
		private const int MaximumEntities = 5;

		public static async Task<string?> ValidateSolutionAsync(
			IOrganizationServiceAsync2 crm, Guid solutionId, string solutionName,
			Guid componentId, ComponentType componentType, CancellationToken cancellationToken)
		{
			var entitiesQuery = new QueryExpression("solutioncomponent") { TopCount = MaximumEntities + 1 };
			entitiesQuery.ColumnSet.AddColumn("objectid");
			entitiesQuery.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
			entitiesQuery.Criteria.AddCondition("componenttype", ConditionOperator.Equal, (int)ComponentType.Entity);
			var entities = await crm.RetrieveMultipleAsync(entitiesQuery, cancellationToken);
			if (entities.Entities.Count > MaximumEntities)
				return componentType == ComponentType.Entity
					? $"Solution <{solutionName}> contains more than {MaximumEntities} tables. Ribbon diff operations require a small solution for performance. Create a dedicated solution with only the target table, without entity metadata or other assets, and pass --solution RibbonDiff."
					: $"Solution <{solutionName}> contains more than {MaximumEntities} tables. Ribbon diff operations require a small solution for performance. Create a dedicated solution with Application Ribbons and pass --solution RibbonDiff.";

			if (componentType == ComponentType.Entity &&
				!entities.Entities.Any(entity => entity.GetAttributeValue<Guid>("objectid") == componentId))
				return $"Solution <{solutionName}> does not contain the target table. Add the table without entity metadata or other assets, or pass a suitable solution with --solution RibbonDiff.";

			if (componentType == ComponentType.RibbonCustomization)
			{
				var ribbonQuery = new QueryExpression("solutioncomponent") { TopCount = 1 };
				ribbonQuery.ColumnSet.AddColumn("solutioncomponentid");
				ribbonQuery.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
				ribbonQuery.Criteria.AddCondition("componenttype", ConditionOperator.Equal, (int)ComponentType.RibbonCustomization);
				ribbonQuery.Criteria.AddCondition("objectid", ConditionOperator.Equal, componentId);
				var ribbons = await crm.RetrieveMultipleAsync(ribbonQuery, cancellationToken);
				if (ribbons.Entities.Count == 0)
					return $"Solution <{solutionName}> does not contain Application Ribbons. Add that component or pass a suitable solution with --solution RibbonDiff.";
			}

			return null;
		}

		public static async Task<(Guid componentId, ComponentType componentType)> ResolveComponentAsync(
			IOrganizationServiceAsync2 crm, string tableName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				var query = new QueryExpression("ribboncustomization") { TopCount = 2 };
				query.ColumnSet.AddColumn("ribboncustomizationid");
				query.Criteria.AddCondition("entity", ConditionOperator.Null);
				query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, new Guid("FD140AAE-4DF4-11DD-BD17-0019B9312238"));
				var ribbons = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (ribbons.Entities.Count != 1)
					throw new InvalidOperationException("Could not uniquely identify the active application ribbon customization.");
				return (ribbons.Entities[0].Id, ComponentType.RibbonCustomization);
			}

			var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
			{
				LogicalName = tableName,
				EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity
			}, cancellationToken);
			if (response.EntityMetadata.MetadataId is not Guid metadataId)
				throw new InvalidOperationException($"Table <{tableName}> has no metadata ID.");
			return (metadataId, ComponentType.Entity);
		}

		public static XElement? FindRibbonDiff(XDocument document, string tableName)
		{
			var root = document.Root;
			if (root?.Name != "ImportExportXml") return null;
			if (string.IsNullOrWhiteSpace(tableName)) return root.Element("RibbonDiffXml");
			return root.Element("Entities")?.Elements("Entity")
				.FirstOrDefault(entity => string.Equals((string?)entity.Element("Name"), tableName, StringComparison.OrdinalIgnoreCase))
				?.Element("RibbonDiffXml");
		}
	}
}
