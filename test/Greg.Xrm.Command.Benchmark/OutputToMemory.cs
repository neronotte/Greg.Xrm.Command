using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Benchmark;

/// <summary>
/// A no-op output implementation for benchmarking (avoids console overhead)
/// </summary>
public class OutputToMemory : IOutput
{
    public IOutput Write(object? value)
    {
        return this;
    }

    public IOutput Write(object? value, ConsoleColor foregroundColor)
    {
        return this;
    }

    public IOutput WriteLine()
    {
        return this;
    }

    public IOutput WriteLine(object? value)
    {
        return this;
    }

    public IOutput WriteLine(object? value, ConsoleColor foregroundColor)
    {
        return this;
    }

    public IOutput WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, TRow, ConsoleColor?>? colorPicker = null)
    {
        return this;
    }

    public IOutput WriteTitle(string title)
    {
        return this;
    }

    public IOutput WriteTitle2(string title)
    {
        return this;
    }

    public IOutput WriteTitle3(string title)
    {
        return this;
    }
}
