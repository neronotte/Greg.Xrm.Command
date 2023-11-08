using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Create.Column
{
	public interface IAttributeMetadataBuilderFactory
	{
		IAttributeMetadataBuilder CreateFor(AttributeTypeCode attributeType);
	}
}
