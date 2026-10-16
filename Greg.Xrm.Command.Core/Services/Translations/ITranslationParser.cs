namespace Greg.Xrm.Command.Services.Translations
{
	public interface ITranslationParser
	{
		IReadOnlyDictionary<int, string> Parse(string value);
	}
}
