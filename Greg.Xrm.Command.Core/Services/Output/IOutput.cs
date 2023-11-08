
namespace Greg.Xrm.Command.Services.Output
{
	public interface IOutput
	{
		IOutput Write(object? text);
		IOutput Write(object? text, ConsoleColor color);
		IOutput WriteLine();
		IOutput WriteLine(object? text);
		IOutput WriteLine(object? text, ConsoleColor color);
	}
}
