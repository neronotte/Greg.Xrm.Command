﻿using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
	internal class AttributeMetadataBuilderDecimal : AttributeMetadataBuilderNumericBase
	{
		public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
		{
			var attribute = new DecimalAttributeMetadata();
			SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			// Set extended properties
			attribute.MinValue = Convert.ToDecimal(GetDoubleValue(command.MinValue, Limit.Min));
			attribute.MaxValue = Convert.ToDecimal(GetDoubleValue(command.MaxValue, Limit.Max));

			attribute.Precision = command.Precision; // 2
			attribute.ImeMode = command.ImeMode; // ImeMode.Disabled

			return Task.FromResult((AttributeMetadata)attribute);
		}
	}
}
