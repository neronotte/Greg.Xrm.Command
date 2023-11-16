using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Parsing
{
    public interface ICanProvideUsageExample
	{
		void WriteUsageExamples(MarkdownWriter writer);
	}
}
