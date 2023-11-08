using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Create.Column
{
	public interface IAttributeMetadataBuilder 
	{
		AttributeMetadata CreateFrom(CreateColumnCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix);
	}
}
