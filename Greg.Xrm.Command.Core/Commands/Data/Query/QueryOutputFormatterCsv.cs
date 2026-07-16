using System.Diagnostics;
using System.Text;
using Microsoft.Xrm.Sdk;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	internal class QueryOutputFormatterCsv(IAnsiConsole console, string? fileName) : QueryOutputFormatterBase
	{
		const char Separator = ';';
		const char Delimiter = '"';

		public override async Task Print(IReadOnlyCollection<Entity> entities, bool autorun, CancellationToken cancellationToken)
		{
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


			var sb = new StringBuilder();
			sb.AppendLine(string.Join(";", columns.Select(x => $"{Delimiter}{x}{Delimiter}")));



			foreach (var entity in entities)
			{
				var row = new List<string>();
				foreach (var column in columns)
				{
					if (entity.Attributes.Contains(column))
					{
						var formattedValu = base.GetPrintableString(entity, column);

						row.Add($"{Delimiter}{formattedValu}{Delimiter}");
					}
					else
					{
						row.Add(string.Empty);
					}
				}
				sb.AppendLine(string.Join(Separator, row));
			}

			var csv = sb.ToString();

			Print(console, csv);

			if (!string.IsNullOrWhiteSpace(fileName))
			{
				await File.WriteAllTextAsync(fileName, csv, cancellationToken);

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
