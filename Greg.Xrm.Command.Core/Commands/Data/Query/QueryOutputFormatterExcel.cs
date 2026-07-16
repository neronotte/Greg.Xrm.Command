using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.Xrm.Sdk;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	internal class QueryOutputFormatterExcel(IAnsiConsole console, string? fileName) : QueryOutputFormatterBase
	{
		public override async Task Print(IReadOnlyCollection<Entity> entities, bool autorun, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				throw new InvalidOperationException("File name must be provided for Excel output format.");

			var columns = new List<string>();
			foreach (var item in entities)
			{
				foreach (var attribute in item.Attributes)
				{
					if (!columns.Contains(attribute.Key))
					{
						columns.Add(attribute.Key);
					}
				}
			}

			if (columns.Count == 0 || entities.Count == 0)
			{
				console.MarkupLine("[red]No data to export.[/]");
				return;
			}



			var wb = new XLWorkbook();
			var ws = wb.AddWorksheet("Data");


			var row = 1;
			var col = 0;
			foreach (var column in columns)
			{
				ws.Cell(row, ++col).SetValue(column);
			}

			foreach (var entity in entities)
			{
				row++;
				col = 0;

				foreach (var column in columns)
				{
					++col;
					
					if (!entity.Attributes.Contains(column)) continue;

					var formattedValue = base.GetPrintableString(entity, column);

					ws.Cell(row, col).SetValue(formattedValue);
				}
			}


			var table = ws.CreateTable("Table1", 1, 1, row, col, true);
			table.Theme = XLTableTheme.TableStyleMedium6;

			wb.SaveAs(fileName);
			wb.Dispose();

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
