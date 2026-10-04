using System.Xml.Linq;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Views.Model
{
	public static class ViewXmlEditor
	{
		private const int DefaultWidth = 100;

		public static string GetTableName(string fetchXml)
		{
			var entity = ParseFetch(fetchXml).Root!.Element("entity")!;
			var name = (string?)entity.Attribute("name");
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("FetchXML must specify the main entity name.");
			return name;
		}

		public static (string FetchXml, string LayoutXml) CreateFromFetchXml(string fetchXml, EntityMetadata metadata)
		{
			if (string.IsNullOrWhiteSpace(metadata.PrimaryIdAttribute) || metadata.ObjectTypeCode == null)
				throw new ArgumentException("Table metadata must include a primary ID attribute and object type code.");
			return CreateFromFetchXml(fetchXml, metadata.LogicalName, metadata.PrimaryIdAttribute,
				metadata.PrimaryNameAttribute, metadata.ObjectTypeCode.Value);
		}

		public static (string FetchXml, string LayoutXml) CreateFromFetchXml(
			string fetchXml, string tableLogicalName, string primaryId, string? primaryName, int objectTypeCode)
		{
			var tableName = GetTableName(fetchXml);
			if (!string.Equals(tableName, tableLogicalName, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException($"FetchXML entity must be '{tableLogicalName}'.");
			if (string.IsNullOrWhiteSpace(primaryId)) throw new ArgumentException("The table must have a primary ID attribute.");

			var attributes = ParseFetch(fetchXml).Root!.Element("entity")!.Elements("attribute")
				.Select(a => (string?)a.Attribute("name")).Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
			var jump = attributes.FirstOrDefault(a => string.Equals(a, primaryName, StringComparison.OrdinalIgnoreCase))
				?? attributes.FirstOrDefault() ?? primaryId;
			var fetch = ParseFetch(fetchXml);
			var entity = fetch.Root!.Element("entity")!;
			if (!entity.Elements("attribute").Any(a => string.Equals((string?)a.Attribute("name"), primaryId, StringComparison.OrdinalIgnoreCase)))
				entity.Add(new XElement("attribute", new XAttribute("name", primaryId)));
			var layout = new XDocument(new XElement("grid",
				new XAttribute("name", "resultset"),
				new XAttribute("jump", jump),
				new XAttribute("select", "1"),
				new XAttribute("preview", "1"),
				new XAttribute("icon", "1"),
				new XAttribute("object", objectTypeCode),
				new XElement("row", new XAttribute("name", "result"), new XAttribute("id", primaryId))));
			return SetFetchXml(layout.ToString(), fetch.ToString(), tableName);
		}

		public static string SetFilter(string? fetchXml, string filterXml)
		{
			var fetch = ParseFetch(fetchXml);
			var filter = XDocument.Parse(filterXml).Root;
			if (filter?.Name != "filter") throw new ArgumentException("The filter must be a FetchXML <filter> element.");

			var entity = fetch.Root!.Element("entity")!;
			var oldFilters = entity.Elements("filter").ToList();
			if (oldFilters.Count > 0)
			{
				oldFilters[0].AddBeforeSelf(filter);
				foreach (var oldFilter in oldFilters) oldFilter.Remove();
			}
			else
			{
				entity.Add(filter);
			}
			return fetch.ToString();
		}

		public static (string FetchXml, string LayoutXml) SetFetchXml(string? layoutXml, string fetchXml, string tableName)
		{
			var fetch = ParseFetch(fetchXml);
			var entity = fetch.Root!.Element("entity")!;
			if (!string.Equals((string?)entity.Attribute("name"), tableName, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException($"FetchXML entity must be '{tableName}'.");

			var layout = ParseLayout(layoutXml);
			var row = layout.Root!.Element("row")!;
			var id = (string?)row.Attribute("id");
			var columns = entity.Elements().SelectMany(element =>
			{
				if (element.Name == "attribute") return new string?[] { (string?)element.Attribute("name") };
				if (element.Name != "link-entity") return Enumerable.Empty<string?>();
				var alias = (string?)element.Attribute("alias");
				return element.Elements("attribute").Select(a => alias == null ? null : alias + "." + (string?)a.Attribute("name"));
			})
				.Where(name => !string.IsNullOrWhiteSpace(name) && !string.Equals(name, id, StringComparison.OrdinalIgnoreCase))
				.Select(name => name!)
				.ToList();
			if (columns.Count == 0) throw new ArgumentException("FetchXML must specify at least one display attribute.");
			SetLayoutColumns(layout, columns);
			return (fetch.ToString(), layout.ToString());
		}

		public static (string FetchXml, string LayoutXml) SetColumns(string? fetchXml, string? layoutXml, string columnsText)
		{
			var columns = columnsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (columns.Length == 0 || columns.Any(c => c.StartsWith('.') || c.EndsWith('.') || c.Count(ch => ch == '.') > 1) || columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Length)
				throw new ArgumentException("Specify unique, comma-separated attribute names, optionally prefixed by a linked entity alias.");

			var fetch = ParseFetch(fetchXml);
			var layout = ParseLayout(layoutXml);
			var entity = fetch.Root!.Element("entity")!;
			var row = layout.Root!.Element("row")!;
			var id = (string?)row.Attribute("id");
			var links = entity.Elements("link-entity").Where(link => link.Attribute("alias") != null)
				.ToDictionary(link => (string)link.Attribute("alias")!, StringComparer.OrdinalIgnoreCase);
			var mainColumns = columns.Where(c => !c.Contains('.')).ToList();
			var linkedColumns = columns.Where(c => c.Contains('.')).Select(c => c.Split('.', 2)).ToList();
			foreach (var column in linkedColumns)
				if (!links.ContainsKey(column[0])) throw new ArgumentException($"Unknown linked entity alias '{column[0]}'.");
			var existing = entity.Elements("attribute").ToDictionary(a => (string?)a.Attribute("name") ?? string.Empty, StringComparer.OrdinalIgnoreCase);
			var kept = mainColumns.Select(c => existing.TryGetValue(c, out var attribute) ? new XElement(attribute) : new XElement("attribute", new XAttribute("name", c))).ToList();
			if (id != null && existing.TryGetValue(id, out var idAttribute) && !columns.Contains(id, StringComparer.OrdinalIgnoreCase))
				kept.Add(new XElement(idAttribute));
			entity.Elements("attribute").Remove();
			entity.AddFirst(kept);
			foreach (var (alias, link) in links)
			{
				var oldAttributes = link.Elements("attribute").ToDictionary(a => (string?)a.Attribute("name") ?? string.Empty, StringComparer.OrdinalIgnoreCase);
				var selected = linkedColumns.Where(c => string.Equals(c[0], alias, StringComparison.OrdinalIgnoreCase))
					.Select(c => oldAttributes.TryGetValue(c[1], out var attribute) ? new XElement(attribute) : new XElement("attribute", new XAttribute("name", c[1]))).ToList();
				link.Elements("attribute").Remove();
				link.AddFirst(selected);
			}
			SetLayoutColumns(layout, columns.Where(c => !string.Equals(c, id, StringComparison.OrdinalIgnoreCase)).ToList());
			return (fetch.ToString(), layout.ToString());
		}

		public static (string FetchXml, string LayoutXml, IReadOnlyList<string> UnusedAttributes) SetView(
			string fetchXml, string layoutXml, string tableName)
		{
			var fetch = ParseFetch(fetchXml);
			var entity = fetch.Root!.Element("entity")!;
			if (!string.Equals((string?)entity.Attribute("name"), tableName, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException($"FetchXML entity must be '{tableName}'.");
			var layout = ParseLayout(layoutXml);
			var rows = layout.Root!.Elements("row").ToList();
			if (rows.Count != 1 || string.IsNullOrWhiteSpace((string?)rows[0].Attribute("id")))
				throw new ArgumentException("LayoutXML must contain one <row> with an id attribute.");
			var row = rows[0];
			var cells = row.Elements("cell").ToList();
			var cellNames = cells.Select(cell => (string?)cell.Attribute("name")).ToList();
			if (cellNames.Count == 0 || cellNames.Any(string.IsNullOrWhiteSpace) ||
				cellNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != cellNames.Count)
				throw new ArgumentException("LayoutXML must contain distinct, named <cell> elements.");

			if (entity.Descendants("all-attributes").Any())
				throw new ArgumentException("FetchXML <all-attributes/> cannot be validated against layout cells; use explicit <attribute> elements.");

			var selected = new List<string>();
			selected.AddRange(entity.Elements("attribute").Select(GetSelectedName));
			foreach (var link in entity.Descendants("link-entity"))
			{
				var linkAlias = (string?)link.Attribute("alias");
				foreach (var attribute in link.Elements("attribute"))
				{
					var attributeAlias = (string?)attribute.Attribute("alias");
					var name = (string?)attribute.Attribute("name");
					selected.Add(!string.IsNullOrWhiteSpace(attributeAlias) ? attributeAlias :
						!string.IsNullOrWhiteSpace(linkAlias) && !string.IsNullOrWhiteSpace(name) ? linkAlias + "." + name : string.Empty);
				}
			}
			if (selected.Any(string.IsNullOrWhiteSpace))
				throw new ArgumentException("Every FetchXML <attribute> must have a name; linked attributes also need a link alias or attribute alias.");

			var selectedNames = selected.ToHashSet(StringComparer.OrdinalIgnoreCase);
			var missing = cellNames.Where(name => !selectedNames.Contains(name!)).ToList();
			if (missing.Count > 0)
				throw new ArgumentException("LayoutXML columns missing from FetchXML: " + string.Join(", ", missing));

			var displayed = cellNames.Select(name => name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
			var rowId = (string?)row.Attribute("id");
			var unused = selected.Where(name => !displayed.Contains(name) && !string.Equals(name, rowId, StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			return (fetch.ToString(), layout.ToString(), unused);
		}

		private static string GetSelectedName(XElement attribute)
		{
			return (string?)attribute.Attribute("alias") ?? (string?)attribute.Attribute("name") ?? string.Empty;
		}

		private static XDocument ParseFetch(string? xml)
		{
			if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException("The view has no FetchXML.");
			var fetch = XDocument.Parse(xml);
			if (fetch.Root?.Name != "fetch" || fetch.Root.Element("entity") == null)
				throw new ArgumentException("Expected FetchXML with a <fetch><entity> structure.");
			return fetch;
		}

		private static XDocument ParseLayout(string? xml)
		{
			if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException("The view has no LayoutXML.");
			var layout = XDocument.Parse(xml);
			if (layout.Root?.Name != "grid" || layout.Root.Element("row") == null)
				throw new ArgumentException("Expected LayoutXML with a <grid><row> structure.");
			return layout;
		}

		private static void SetLayoutColumns(XDocument layout, IReadOnlyList<string> columns)
		{
			if (columns.Count == 0 || columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Count)
				throw new ArgumentException("Display attributes must be unique and nonempty.");
			var row = layout.Root!.Element("row")!;
			var oldCells = row.Elements("cell").ToDictionary(c => (string?)c.Attribute("name") ?? string.Empty, StringComparer.OrdinalIgnoreCase);
			var cells = columns.Select(name => oldCells.TryGetValue(name, out var old)
				? new XElement(old)
				: new XElement("cell", new XAttribute("name", name), new XAttribute("width", DefaultWidth))).ToList();
			row.Elements("cell").Remove();
			row.Add(cells);
		}
	}
}
