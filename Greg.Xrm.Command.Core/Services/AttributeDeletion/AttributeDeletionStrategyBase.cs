using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
	public abstract class AttributeDeletionStrategyBase : IAttributeDeletionStrategy
	{
		public abstract ComponentType ComponentType { get; }


		public async Task HandleAsync(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, DependencyList dependencies)
		{
			var dependeciesFiltered = dependencies.OfType(this.ComponentType);
			if (dependeciesFiltered.Count == 0) return;

			await this.HandleInternalAsync(crm, attribute, dependeciesFiltered);
		}

		protected abstract Task HandleInternalAsync(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyList<Dependency> dependencies);



		protected static async Task<IReadOnlyList<Entity>> RetrieveDataAsync(
			IOrganizationServiceAsync2 crm,
			IReadOnlyCollection<Dependency> dependencies,
			string tableName,
			string idColumn,
			params string[] columns)
		{
			if (dependencies.Count == 0)
				return [];

			var ids = dependencies.Select(x => x.dependentcomponentobjectid).Cast<object>().ToArray();


			var query = new QueryExpression(tableName);
			query.ColumnSet.AddColumns(columns);
			query.Criteria.AddCondition(idColumn, ConditionOperator.In, ids);
			query.NoLock = true;

			var result = await crm.RetrieveMultipleAsync(query);
			return result.Entities.ToArray();
		}



		protected static string RemoveValueFromCsvValues(string csv, string value)
		{
			var values = csv.Split(',').Where(s => s != value);
			return string.Join(",", values);
		}



		protected static string? RemoveFieldFromFetchXml(string? xml, string attName)
		{
			if (xml == null) return null;

			xml = RemoveXmlNodesWithTagValue(xml, "condition", "attribute", attName);
			xml = RemoveXmlNodesWithTagValue(xml, "attribute", "name", attName);
			xml = RemoveXmlNodesWithTagValue(xml, "cell", "name", attName); // Layout Xml from Views
			xml = RemoveXmlNodesWithTagValue(xml, "order", "attribute", attName);
			return xml;
		}




		protected static string RemoveXmlNodesWithTagValue(string? xml, string nodeName, string tagName, string tagValue)
		{
			if (string.IsNullOrWhiteSpace(xml)) return string.Empty;

			var nodeStart = $"<{nodeName} ";
			const string nodeEnd = "/>";
			var nodeFullEnd = $"</{nodeName}>";

			foreach (var node in xml.SubstringAllByString(nodeStart, nodeFullEnd)
						.Where(c => c.Contains($"{tagName}=\"{tagValue}\"", StringComparison.OrdinalIgnoreCase)).ToArray())
			{
				xml = xml.Replace(nodeStart + node + nodeFullEnd, string.Empty);
			}

			foreach (var node in xml.SubstringAllByString(nodeStart, nodeEnd)
									.Where(c => !c.Contains(nodeFullEnd, StringComparison.OrdinalIgnoreCase) && c.Contains($"{tagName}=\"{tagValue}\"")).ToArray())
			{
				xml = xml.Replace(nodeStart + node + nodeEnd, string.Empty);
			}

			return xml;
		}
	}
}
