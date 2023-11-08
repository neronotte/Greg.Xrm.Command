using System.Text;

namespace Greg.Xrm.Command.Services.Output
{
	public class OutputToMemory : IOutput
	{
		private readonly StringBuilder sb = new();

		public IOutput Write(object? text)
		{
			sb.Append(text);
			return this;
		}

		public IOutput Write(object? text, ConsoleColor color)
		{
			sb.Append('<').Append(color.ToString()).Append('>');
			sb.Append(text);
			sb.Append("</").Append(color.ToString()).Append('>');
			return this;
		}

		public IOutput WriteLine(object? text)
		{
			sb.AppendLine(text?.ToString());
			return this;
		}

		public IOutput WriteLine()
		{
			sb.AppendLine();
			return this;
		}

		public IOutput WriteLine(object? text, ConsoleColor color)
		{
			sb.Append('<').Append(color.ToString()).Append('>');
			sb.Append(text);
			sb.Append("</").Append(color.ToString()).Append('>');
			sb.AppendLine();
			return this;
		}

		public override string ToString()
		{
			return sb.ToString();
		}
	}
}
