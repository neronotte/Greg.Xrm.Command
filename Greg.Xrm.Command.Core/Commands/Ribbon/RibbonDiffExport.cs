using System.Xml.Linq;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	internal static class RibbonDiffExport
	{
		private const int MaximumEntities = 5;
		private const int MaximumComponents = MaximumEntities + 1; // Five tables and one application ribbon.
		private const int DoNotIncludeSubcomponents = 1;
		private const int IncludeAsShellOnly = 2;

		public static async Task<string?> ValidateSolutionAsync(
			IOrganizationServiceAsync2 crm, Guid solutionId, string solutionName,
			Guid componentId, ComponentType componentType, CancellationToken cancellationToken)
		{
			// Fetch one row more than the largest permitted solution. Seven rows already prove it is unsafe.
			var query = new QueryExpression("solutioncomponent") { TopCount = MaximumComponents + 1 };
			query.ColumnSet.AddColumns("componenttype", "objectid", "rootcomponentbehavior", "rootsolutioncomponentid");
			query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
			var components = (await crm.RetrieveMultipleAsync(query, cancellationToken)).Entities;

			var tables = new List<Entity>();
			var ribbons = new List<Entity>();
			foreach (var component in components)
			{
				var type = Choice(component, "componenttype");
				if (type == (int)ComponentType.Entity)
				{
					tables.Add(component);
					if (tables.Count > MaximumEntities)
						return $"Solution <{solutionName}> contains more than {MaximumEntities} tables. Use a dedicated solution with at most {MaximumEntities} segmented tables and Application Ribbons. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
					if (Choice(component, "rootcomponentbehavior") is not (DoNotIncludeSubcomponents or IncludeAsShellOnly))
						return $"Solution <{solutionName}> contains a table with all subcomponents or unknown segmentation. Add each table without subcomponents (or as a shell), and remove other table components. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
				}
				else if (type == (int)ComponentType.RibbonCustomization)
				{
					ribbons.Add(component);
					if (ribbons.Count > 1)
						return $"Solution <{solutionName}> contains more than one ribbon customization. Only Application Ribbons is allowed. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
				}
				else
					return $"Solution <{solutionName}> contains an unsupported component type. Only segmented tables and Application Ribbons are allowed. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
			}

			if (componentType == ComponentType.Entity && !tables.Any(c => c.GetAttributeValue<Guid>("objectid") == componentId))
				return $"Solution <{solutionName}> does not contain the target table. Add it without subcomponents or pass a suitable solution with --solution RibbonDiff.";

			if (componentType == ComponentType.RibbonCustomization && !ribbons.Any(c => c.GetAttributeValue<Guid>("objectid") == componentId))
				return $"Solution <{solutionName}> does not contain Application Ribbons. Add that component or pass a suitable solution with --solution RibbonDiff.";

			if (ribbons.Count == 1)
			{
				var ribbonQuery = new QueryExpression("ribboncustomization") { TopCount = 1 };
				ribbonQuery.ColumnSet.AddColumn("ribboncustomizationid");
				ribbonQuery.Criteria.AddCondition("ribboncustomizationid", ConditionOperator.Equal, ribbons[0].GetAttributeValue<Guid>("objectid"));
				ribbonQuery.Criteria.AddCondition("entity", ConditionOperator.Null);
				var applicationRibbon = await crm.RetrieveMultipleAsync(ribbonQuery, cancellationToken);
				if (applicationRibbon.Entities.Count != 1)
					return $"Solution <{solutionName}> contains a ribbon customization that is not Application Ribbons. {await DescribeComponentAsync(crm, ribbons[0], components, cancellationToken)}";
			}

			return null;
		}

		private static int? Choice(Entity component, string attributeName) => component.Attributes.TryGetValue(attributeName, out var value)
			? value switch { OptionSetValue option => option.Value, int number => number, _ => null }
			: null;

		private static async Task<string> DescribeComponentAsync(
			IOrganizationServiceAsync2 crm, Entity component, IEnumerable<Entity> components, CancellationToken cancellationToken)
		{
			var typeNumber = Choice(component, "componenttype");
			var typeName = typeNumber.HasValue && Enum.IsDefined(typeof(ComponentType), typeNumber.Value)
				? ((ComponentType)typeNumber.Value).ToString()
				: "Unknown";
			var objectId = component.GetAttributeValue<Guid>("objectid");
			var details = $"First invalid component: type={typeName} ({typeNumber?.ToString() ?? "unknown"}), objectId={objectId}, solutionComponentId={component.Id}";
			var rootId = component.GetAttributeValue<Guid?>("rootsolutioncomponentid");
			if (rootId.HasValue)
				details += $", rootSolutionComponentId={rootId.Value}";
			var behavior = Choice(component, "rootcomponentbehavior");
			if (behavior.HasValue)
				details += $", rootComponentBehavior={behavior.Value}";

			try
			{
				string? tableName = null;
				string? itemName = null;
				if (typeNumber == (int)ComponentType.Attribute && objectId != Guid.Empty)
				{
					try
					{
						var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(new RetrieveAttributeRequest
						{
							MetadataId = objectId,
							RetrieveAsIfPublished = true
						}, cancellationToken);
						tableName = response.AttributeMetadata?.EntityLogicalName;
						itemName = response.AttributeMetadata?.LogicalName;
					}
					catch (OperationCanceledException) { throw; }
					catch (Exception) { /* A removed field can still have an intact root table component. */ }
				}
				else if (typeNumber == (int)ComponentType.Entity && objectId != Guid.Empty)
				{
					var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
					{
						MetadataId = objectId,
						EntityFilters = EntityFilters.Entity,
						RetrieveAsIfPublished = true
					}, cancellationToken);
					tableName = response.EntityMetadata?.LogicalName;
				}
				else if (typeNumber is (int)ComponentType.SystemForm or (int)ComponentType.SavedQuery or (int)ComponentType.SavedQueryVisualization)
				{
					var (entityName, idColumn, tableColumn, nameColumn) = typeNumber switch
					{
						(int)ComponentType.SystemForm => ("systemform", "formid", "objecttypecode", "name"),
						(int)ComponentType.SavedQuery => ("savedquery", "savedqueryid", "returnedtypecode", "name"),
						_ => ("savedqueryvisualization", "savedqueryvisualizationid", "primaryentitytypecode", "name")
					};
					var query = new QueryExpression(entityName) { TopCount = 1 };
					query.ColumnSet.AddColumns(tableColumn, nameColumn);
					query.Criteria.AddCondition(idColumn, ConditionOperator.Equal, objectId);
					var record = (await crm.RetrieveMultipleAsync(query, cancellationToken)).Entities.FirstOrDefault();
					tableName = record?.GetAttributeValue<string>(tableColumn);
					itemName = record?.GetAttributeValue<string>(nameColumn);
				}
				if (tableName == null && rootId.HasValue)
				{
					var root = components.FirstOrDefault(c => c.Id == rootId.Value && Choice(c, "componenttype") == (int)ComponentType.Entity);
					if (root != null)
					{
						var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
						{
							MetadataId = root.GetAttributeValue<Guid>("objectid"),
							EntityFilters = EntityFilters.Entity,
							RetrieveAsIfPublished = true
						}, cancellationToken);
						tableName = response.EntityMetadata?.LogicalName;
					}
				}
				if (!string.IsNullOrWhiteSpace(tableName)) details += $", table={tableName}";
				if (!string.IsNullOrWhiteSpace(itemName)) details += $", name={itemName}";
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception) { /* A stale component may no longer have retrievable metadata. Keep its IDs in the error. */ }
			return details + ".";
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
