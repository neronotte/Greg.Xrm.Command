using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.OptionSet
{
	public interface IOptionSetParser
	{
		IReadOnlyCollection<OptionMetadata> Parse(
			string? optionString, 
			string? colorsString, 
			int customizationOptionValuePrefix,
			int languageCode);
	}
}
