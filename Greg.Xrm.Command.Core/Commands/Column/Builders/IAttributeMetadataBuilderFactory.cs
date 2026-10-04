namespace Greg.Xrm.Command.Commands.Column.Builders
{
	public interface IAttributeMetadataBuilderFactory
	{
		IAttributeMetadataBuilder CreateFor(SupportedAttributeType attributeType);
	}
}
