using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public interface IQueryOutputFormatterFactory
	{
		IQueryOutputFormatter BuildFormatter(QueryCommand.OutputFormats format, string? fileName);
	}

	public class QueryOutputFormatterFactory(IAnsiConsole console) : IQueryOutputFormatterFactory
	{
		public IQueryOutputFormatter BuildFormatter(QueryCommand.OutputFormats format, string? fileName)
		{
			return format switch
			{
				QueryCommand.OutputFormats.JSON => new QueryOutputFormatterJson(console, fileName),
				QueryCommand.OutputFormats.CSV => new QueryOutputFormatterCsv(console, fileName),
				QueryCommand.OutputFormats.XML => new QueryOutputFormatterXml(console, fileName),
				QueryCommand.OutputFormats.Excel => new QueryOutputFormatterExcel(console, fileName),
				_ => throw new NotImplementedException($"The output format {format} is not implemented."),
			};
		}
	}
}
