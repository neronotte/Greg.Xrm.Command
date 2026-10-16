using System.Text;
using System.Xml.Linq;
using Greg.Xrm.Command.Commands.Forms;

namespace Greg.Xrm.Command.Services.Forms
{
	/// <summary>Projects form XML onto the hierarchy shown by the model-driven form designer.</summary>
	public class FormLayoutRenderer : IFormLayoutRenderer
	{
		public IEnumerable<string> Render(XElement form, LayoutDisplay display, string? formName = null)
		{
			var rootVisible = IsVisible(form);
			var title = !string.IsNullOrWhiteSpace(formName) ? formName : (string?)form.Attribute("name");
			var heading = new StringBuilder("Form");
			if (!string.IsNullOrWhiteSpace(title)) heading.Append(' ').Append(title);
			yield return heading.Append(Visibility(form, true)).ToString();

			var nodes = new List<LayoutNode>();
			var header = form.Element("header");
			if (header != null) nodes.Add(BuildArea(header, "Header", rootVisible));
			var tabs = form.Element("tabs")?.Elements("tab").ToArray() ?? [];
			for (var i = 0; i < tabs.Length; i++) nodes.Add(BuildTab(tabs[i], i + 1, rootVisible));
			var footer = form.Element("footer");
			if (footer != null) nodes.Add(BuildArea(footer, "Footer", rootVisible));
			foreach (var line in Print(nodes, string.Empty, display)) yield return line;
		}

		private static LayoutNode BuildArea(XElement area, string kind, bool parentVisible)
		{
			var node = new LayoutNode(area, kind, "", parentVisible);
			node.Children.AddRange(BuildControls(area, node.EffectiveVisible));
			return node;
		}

		private static LayoutNode BuildTab(XElement tab, int tabIndex, bool parentVisible)
		{
			var node = new LayoutNode(tab, "Tab", $"tab={tabIndex}", parentVisible);
			var columns = tab.Element("columns")?.Elements("column").ToArray() ?? [];
			for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
			{
				var column = columns[columnIndex];
				var columnVisible = node.EffectiveVisible && IsVisible(column);
				var sections = column.Element("sections")?.Elements("section").ToArray() ?? [];
				for (var sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
				{
					var section = sections[sectionIndex];
					var position = new StringBuilder().Append("column=").Append(columnIndex + 1)
						.Append(", section=").Append(sectionIndex + 1).Append(OrderMetadata(column, "column."));
					var sectionNode = new LayoutNode(section, "Section", position.ToString(), columnVisible);
					sectionNode.Children.AddRange(BuildControls(section, sectionNode.EffectiveVisible));
					node.Children.Add(sectionNode);
				}
			}
			return node;
		}

		private static IEnumerable<LayoutNode> BuildControls(XElement container, bool parentVisible)
		{
			var rows = container.Element("rows")?.Elements("row").ToArray() ?? [];
			for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
			{
				var row = rows[rowIndex];
				var rowVisible = parentVisible && IsVisible(row);
				var cells = row.Elements("cell").ToArray();
				for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
				{
					var cell = cells[cellIndex];
					var cellVisible = rowVisible && IsVisible(cell);
					var position = new StringBuilder().Append("row=").Append(rowIndex + 1)
						.Append(", cell=").Append(cellIndex + 1).Append(OrderMetadata(row, "row.")).Append(OrderMetadata(cell, "cell."));
					var controls = cell.Elements("control").ToArray();
					if (controls.Length == 0)
					{
						yield return new LayoutNode(cell, "Spacer", position.ToString(), rowVisible);
						continue;
					}
					for (var controlIndex = 0; controlIndex < controls.Length; controlIndex++)
					{
						var control = controls[controlIndex];
						var kind = string.IsNullOrWhiteSpace((string?)control.Attribute("datafieldname")) ? "Control" : "Field";
						var controlPosition = new StringBuilder(position.ToString());
						if (controls.Length > 1) controlPosition.Append(", control=").Append(controlIndex + 1);
						yield return new LayoutNode(control, kind, controlPosition.ToString(), cellVisible);
					}
				}
			}
		}

		private static IEnumerable<string> Print(IReadOnlyList<LayoutNode> nodes, string prefix, LayoutDisplay display)
		{
			for (var i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				var last = i == nodes.Count - 1;
				yield return new StringBuilder(prefix).Append(last ? "`-- " : "|-- ").Append(Describe(node, display)).ToString();
				var childPrefix = new StringBuilder(prefix).Append(last ? "    " : "|   ").ToString();
				foreach (var line in Print(node.Children, childPrefix, display)) yield return line;
			}
		}

		private static string Describe(LayoutNode node, LayoutDisplay display)
		{
			var element = node.Element;
			var name = node.Kind is "Header" or "Footer" or "Spacer" ? null
				: (string?)element.Attribute("datafieldname") ?? (string?)element.Attribute("name")
					?? (node.Kind is "Field" or "Control" ? (string?)element.Attribute("id") : null);
			var label = (string?)element.Element("labels")?.Elements("label").FirstOrDefault()?.Attribute("description")
				?? (element.Name.LocalName == "control" ? (string?)element.Parent?.Element("labels")?.Elements("label").FirstOrDefault()?.Attribute("description") : null);
			var identity = display == LayoutDisplay.Names ? name : label ?? name;
			var showBoth = display != LayoutDisplay.Names && display != LayoutDisplay.Labels;
			if (showBoth && string.IsNullOrWhiteSpace(label)) identity = name;
			var text = new StringBuilder(node.Kind);
			if (!string.IsNullOrWhiteSpace(identity)) text.Append(' ').Append(identity);
			if (showBoth && !string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(name) && label != name)
				text.Append(" [name=").Append(name).Append(']');
			if (!string.IsNullOrEmpty(node.Position)) text.Append(" [").Append(node.Position).Append(']');
			return text.Append(Visibility(element, node.ParentVisible)).ToString();
		}

		private static string Visibility(XElement element, bool parentVisible) =>
			!parentVisible ? " (hidden: parent)" : IsVisible(element) ? string.Empty : " (hidden)";

		private static bool IsVisible(XElement element)
		{
			var value = (string?)element.Attribute("visible");
			return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) && value != "0";
		}

		private static string OrderMetadata(XElement element, string prefix = "")
		{
			var text = new StringBuilder();
			foreach (var key in new[] { "ordinal", "order", "sequence" })
			{
				var attribute = element.Attribute(key);
				if (attribute != null) text.Append(", ").Append(prefix).Append(key).Append('=').Append(attribute.Value);
			}
			return text.ToString();
		}

		private sealed class LayoutNode(XElement element, string kind, string position, bool parentVisible)
		{
			public XElement Element { get; } = element;
			public string Kind { get; } = kind;
			public string Position { get; } = new StringBuilder(position).Append(OrderMetadata(element)).ToString().TrimStart(',', ' ');
			public bool ParentVisible { get; } = parentVisible;
			public bool EffectiveVisible => ParentVisible && IsVisible(Element);
			public List<LayoutNode> Children { get; } = [];
		}
	}
}
