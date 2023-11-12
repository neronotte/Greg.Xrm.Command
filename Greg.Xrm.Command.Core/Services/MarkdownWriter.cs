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
			return this;
		}
		public MarkdownWriter WriteTitle2(string title)
		{
			writer.WriteLine("## " + title);
			return this;
		}
		public MarkdownWriter WriteTitle3(string title)
		{
			writer.WriteLine("### " + title);
			return this;
		}

		public MarkdownWriter WriteParagraph(string text)
		{
			writer.WriteLine(text);
			writer.WriteLine();
			return this;
		}

		public MarkdownWriter WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData)
		{
			var headers = rowHeaders();
			var rows = collection.Select(rowData).ToList();

			var columnWidths = new int[headers.Length];
			for (var i = 0; i < headers.Length; i++)
			{
				columnWidths[i] = Math.Max(headers[i].Length, rows.Max(_ => _[i].Length));
			}

			var header = "| " + string.Join(" | ", headers.Select((_, i) => _.PadRight(columnWidths[i]))) + " |";
			var separator = "|-" + string.Join("-|-", columnWidths.Select(_ => new string('-', _))) + "-|";
			var body = string.Join(Environment.NewLine,
				 rows.Select(
					_ => "| " + string.Join(" | ", _.Select((__, i) => __.PadRight(columnWidths[i]))) + " |"
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
