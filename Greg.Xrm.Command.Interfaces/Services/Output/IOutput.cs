﻿
namespace Greg.Xrm.Command.Services.Output
{
	public interface IOutput
	{
		IOutput Write(object? text);
		IOutput Write(object? text, ConsoleColor color);
		IOutput WriteLine();
		IOutput WriteLine(object? text);
		IOutput WriteLine(object? text, ConsoleColor color);
		IOutput WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, TRow, ConsoleColor?>? colorPicker = null);
	}
}
