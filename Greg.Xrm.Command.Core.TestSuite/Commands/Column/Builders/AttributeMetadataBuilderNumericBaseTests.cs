using Greg.Xrm.Command.Services.OptionSet;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
	[TestClass]
	public class AttributeMetadataBuilderNumericBaseTests
	{
		[TestMethod]
		public async Task IntegerBuilder_CreateFromAsync_ShouldApplyNumericBounds()
		{
			var output = new OutputToMemory();
			var parser = new OptionSetParser();
			var factory = new AttributeMetadataBuilderFactory(output, parser);
			var builder = factory.CreateFor(SupportedAttributeType.Integer);

			var command = new CreateCommand
			{
				DisplayName = "My Count",
				EntityName = "sample_entity",
				SchemaName = "ava_my_count",
				MinValue = -12,
				MaxValue = 42,
				IntegerFormat = IntegerFormat.None
			};

			var attribute = await builder.CreateFromAsync(Mock.Of<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), command, 1033, "ava", 0);

			Assert.IsInstanceOfType(attribute, typeof(IntegerAttributeMetadata));
			var integerAttribute = (IntegerAttributeMetadata)attribute;
			Assert.AreEqual(-12, integerAttribute.MinValue);
			Assert.AreEqual(42, integerAttribute.MaxValue);
			Assert.AreEqual(IntegerFormat.None, integerAttribute.Format);
		}
	}
}
