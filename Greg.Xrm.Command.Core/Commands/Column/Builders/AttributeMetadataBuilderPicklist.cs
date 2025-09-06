using Greg.Xrm.Command.Services.OptionSet;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
	public class AttributeMetadataBuilderPicklist(
	IOutput output,
	IOptionSetParser optionSetParser) : AttributeMetadataBuilderBase
	{
		public override async Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			EnumAttributeMetadata attribute = command.Multiselect ? new MultiSelectPicklistAttributeMetadata() : new PicklistAttributeMetadata();
			SetCommonProperties(attribute, command, languageCode, publisherPrefix);





			var optionSet = new OptionSetMetadata();
			optionSet.DisplayName = attribute.DisplayName;
			optionSet.Description = attribute.Description;

			IReadOnlyCollection<OptionMetadata> options;

			if (command.GlobalOptionSetName != null)
			{
				try
				{
					var request = new RetrieveOptionSetRequest { Name = command.GlobalOptionSetName };

					var response = (RetrieveOptionSetResponse)await crm.ExecuteAsync(request);

					if (response.OptionSetMetadata.OptionSetType == OptionSetType.Boolean)
						throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The global option set '{command.GlobalOptionSetName}' is of type Boolean, that is not yet supported.");

					optionSet.OptionSetType = response.OptionSetMetadata.OptionSetType;
					optionSet.IsGlobal = response.OptionSetMetadata.IsGlobal;
					optionSet.IsCustomizable = response.OptionSetMetadata.IsCustomizable;
					optionSet.IsCustomOptionSet = response.OptionSetMetadata.IsCustomOptionSet;
					optionSet.Name = response.OptionSetMetadata.Name;

					options = GetOptionsFrom(response.OptionSetMetadata);
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The global option set '{command.GlobalOptionSetName}' does not exist", ex);
				}
			}
			else
			{
				optionSet.OptionSetType = OptionSetType.Picklist;
				optionSet.IsGlobal = false;
				optionSet.IsCustomOptionSet = true;
				optionSet.IsCustomizable = new BooleanManagedProperty(true);
				optionSet.Name = GetOptionSetName(command.EntityName, attribute.SchemaName); // se non è global. Se è global, il nome è quello del global option set

				var optionString = command.Options;
				if (string.IsNullOrWhiteSpace(optionString))
					throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The options are required for columns of type Picklist");

				options = optionSetParser.Parse(optionString, null, customizationOptionValuePrefix, languageCode);
				optionSet.Options.AddRange(options);
			}

			attribute.DefaultFormValue = GetDefaultValue(command.DefaultFormValue, options, languageCode);
			attribute.OptionSet = optionSet;

			return attribute;
		}

		private int? GetDefaultValue(string? defaultFormValue, IReadOnlyCollection<OptionMetadata> options, int lcid)
		{
			if (string.IsNullOrWhiteSpace(defaultFormValue)) return null;

			var option = options.FirstOrDefault(o => o.Label.GetLocalizedLabel(lcid)?.Equals(defaultFormValue, StringComparison.InvariantCultureIgnoreCase) ?? false);
			if (option != null)
			{
				return option.Value;
			}


			// try by value
			if (!int.TryParse(defaultFormValue, out var optionValue))
			{
				output.WriteLine($"The provided default value '{defaultFormValue}' is not valid. It must be either one of the labels of the option, or an integer value.", ConsoleColor.Yellow);
				return null;
			}
				
			option = options.FirstOrDefault(o => o.Value == optionValue);
			if (option == null)
			{
				output.WriteLine($"The provided default value '{defaultFormValue}' is not valid. It does not match any value for the current Picklist.", ConsoleColor.Yellow);
				return null;
			}

			return option.Value;
		}

		private static IReadOnlyCollection<OptionMetadata> GetOptionsFrom(OptionSetMetadataBase optionSetMetadata)
		{
			if (optionSetMetadata is OptionSetMetadata o1)
				return o1.Options;

			return [];
		}

		private static string GetOptionSetName(string? entityName, string schemaName)
		{
			return $"{entityName}_{schemaName}";
		}

		protected override string GetSchemaName(string? displayName, string? schemaName, string publisherPrefix)
		{
			var newSchemaName = base.GetSchemaName(displayName, schemaName, publisherPrefix);

			if (!newSchemaName.EndsWith("code"))
			{
				newSchemaName += "code";
			}

			return newSchemaName;
		}
	}
}
