using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Services
{
	public class MarkdownWriter : IDisposable
	{
		private readonly TextWriter writer;

		public MarkdownWriter(string fileName)
		{
			writer = new StreamWriter(fileName, false, System.Text.Encoding.UTF8);
		}

		public MarkdownWriter(TextWriter writer)
		{
			this.writer = writer;
		}

		#region IDisposable implementation

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing) return;

			writer.Dispose();
		}

		~MarkdownWriter()
		{
			Dispose(false);
		}

		#endregion


		public MarkdownWriter WriteTitle1(string title)
		{
			writer.WriteLine("# " + title);
			writer.WriteLine();
			return this;
		}
		public MarkdownWriter WriteTitle2(string title)
		{
			writer.WriteLine("## " + title);
			writer.WriteLine();
			return this;
		}
		public MarkdownWriter WriteTitle3(string title)
		{
			writer.WriteLine("### " + title);
			writer.WriteLine();
			return this;
		}

		public MarkdownWriter WriteParagraph(string text)
		{
			writer.WriteLine(text);
			writer.WriteLine();
			return this;
		}

		public MarkdownWriter WriteList(params string[] lines)
		{
			return WriteList(0, lines);
		}
		public MarkdownWriter WriteList(int indentLevel, params string[] lines)
		{
			var indent = new string(' ', indentLevel * 2);

			foreach (var line in lines)
			{
				writer.Write(indent);
				writer.Write("- ");
				writer.WriteLine(line);
			}
			writer.WriteLine();
			return this;
		}


		public MarkdownWriter WriteTable<TRow>(IReadOnlyList<TRow> collection, string[] rowHeaders, Func<TRow, string?[]> rowData)
		{
			return WriteTable(collection, () => rowHeaders, rowData);
		}

		public MarkdownWriter WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string?[]> rowData)
		{
			var headers = rowHeaders();
			var rows = collection.Select(rowData).ToList();

			var columnWidths = new int[headers.Length];
			for (var i = 0; i < headers.Length; i++)
			{
				columnWidths[i] = Math.Max(headers[i].Length, rows.Max(row => row[i]?.Length ?? 0));
			}

			var header = "| " + string.Join(" | ", headers.Select((col, i) => col.PadRight(columnWidths[i]))) + " |";
			var separator = "|-" + string.Join("-|-", columnWidths.Select(colWidth => new string('-', colWidth))) + "-|";
			var body = string.Join(Environment.NewLine,
				 rows.Select(
					row => "| " + string.Join(" | ", row.Select((col, i) => col?.PadRight(columnWidths[i]))) + " |"
				 )
			);

			writer.WriteLine(header + Environment.NewLine + separator + Environment.NewLine + body);
			return this;
		}

		public MarkdownWriter WriteCodeBlock(string code, string? language = null)
		{
			writer.Write("```");
			if (language != null)
				writer.Write(language);
			writer.WriteLine();
			writer.WriteLine(code);
			writer.WriteLine("```");
			writer.WriteLine();
			return this;
		}
		public MarkdownWriter WriteCodeBlockStart(string language)
		{
			writer.Write("```");
			writer.Write(language);
			writer.WriteLine();
			return this;
		}
		public MarkdownWriter WriteCodeBlockEnd()
		{
			writer.WriteLine("```");
			return this;
		}

		public MarkdownWriter Write(string text)
		{
			writer.Write(text);
			return this;
		}

		public MarkdownWriter WriteCode(string text)
		{
			writer.Write(text.ToMarkdownCode());
			return this;
		}

		public MarkdownWriter WriteLine(string? text = null)
		{
			writer.WriteLine(text);
			return this;
		}
	}
}
