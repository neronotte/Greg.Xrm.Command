using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	internal class QueryOutputFormatterXml(IAnsiConsole console, string? fileName) : QueryOutputFormatterBase
	{
		public override async Task Print(IReadOnlyCollection<Entity> entities, bool autorun, CancellationToken cancellationToken)
		{
			if (entities.Count == 0)
			{
				console.MarkupLine("[red]No data to export.[/]");
				return;
			}


			var items = new XDocument(new XElement("Entities"));
			foreach (var entity in entities)
			{
				var item = new XElement("Entity");
				foreach (var attribute in entity.Attributes)
				{
					var formattedValue = base.GetPrintableString(entity, attribute.Key);
					item.Add(new XElement(System.Xml.XmlConvert.EncodeLocalName(attribute.Key), formattedValue));
				}
				items.Root!.Add(item);
			}

			var xml = items.ToString(SaveOptions.None);

			Print(console, xml);


			if (!string.IsNullOrWhiteSpace(fileName))
			{
				await File.WriteAllTextAsync(fileName, xml, cancellationToken);

				if (autorun)
				{
					Process.Start(new ProcessStartInfo
					{
						UseShellExecute = true,
						FileName = fileName
					});
				}
			}
		}
	}
}
