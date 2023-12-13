using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Relationship
{
    public class CreateNNExplicitStrategy : ICreateNNStrategy
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceAsync2 crm;

		public CreateNNExplicitStrategy(IOutput output, IOrganizationServiceAsync2 crm)
		{
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.crm = crm ?? throw new ArgumentNullException(nameof(crm));
		}





		public async Task<CommandResult> CreateAsync(CreateNNCommand command, string currentSolutionName, int defaultLanguageCode, string publisherPrefix)
		{


			// we need to create a new table, generating the name and the primary attribute
			// primary attribute should be an autonumber

			await crm.CheckManyToManyExplicitEligibilityAsync(command.Table1, command.Table2);

			var table1 = await crm.GetEntityMetadataAsync(command.Table1);
			var table2 = await crm.GetEntityMetadataAsync(command.Table2);


			var parentRequest = new ExecuteTransactionRequest
			{
				ReturnResponses = true,
				Requests = new OrganizationRequestCollection()
			};


			#region Creating intersection table

			output.Write("Setting up CreateEntityRequest...");

			var entityMetadata = new EntityMetadata
			{
				DisplayName = SetTableDisplayName(command, table1, table2, defaultLanguageCode),
				DisplayCollectionName = SetTableDisplayCollectionName(command, table1, table2, defaultLanguageCode),
				Description = SetTableDescription(command, defaultLanguageCode),
				SchemaName = SetTableSchemaName(command, publisherPrefix),
				OwnershipType = command.Ownership,
				IsActivity = false,
				IsAuditEnabled = SetTableIsAuditEnabled(command)
			};

			var primaryAttribute = new StringAttributeMetadata
			{
				DisplayName = SetPrimaryAttributeDisplayName(command, defaultLanguageCode),
				Description = SetPrimaryAttributeDescription(command, defaultLanguageCode),
				RequiredLevel = SetPrimaryAttributeRequiredLevel(command),
				MaxLength = SetPrimaryAttributeMaxLength(command),
				AutoNumberFormat = SetPrimaryAttributeAutoNumberFormat(command, table1, table2, defaultLanguageCode),
				FormatName = StringFormatName.Text
			};
			primaryAttribute.SchemaName = SetPrimaryAttributeSchemaName(command, primaryAttribute.DisplayName, publisherPrefix);

			output.WriteLine(" Done", ConsoleColor.Green);


			output.Write("Creating table...");
			var request = new CreateEntityRequest
			{
				Entity = entityMetadata,
				PrimaryAttribute = primaryAttribute
			};
			
			parentRequest.Requests.Add(request);


			#endregion


			#region Creating lookup against table 1

			output.Write("Setting up CreateOneToManyRequest (1/2)...");

			var request1 = new CreateOneToManyRequest
			{
				SolutionUniqueName = currentSolutionName,
				OneToManyRelationship = new OneToManyRelationshipMetadata
				{
					ReferencingEntity = request.Entity.SchemaName,
					ReferencedEntity = command.Table1,
					SchemaName = CreateRelationshipSchemaName(request.Entity.SchemaName, command.Table1, publisherPrefix),
					AssociatedMenuConfiguration = new AssociatedMenuConfiguration
					{
						Behavior = command.MenuBehavior1,
						Group = command.MenuBehavior1 == AssociatedMenuBehavior.DoNotDisplay ? null : command.MenuGroup1,
						Label = CreateNNImplicitStrategy.GetLabel(command.MenuBehavior1, command.MenuLabel1, defaultLanguageCode),
						Order = command.MenuOrder1
					},
					CascadeConfiguration = new CascadeConfiguration
					{
						Assign = command.CascadeAssign1,
						Archive = command.CascadeArchive1,
						Share = command.CascadeShare1,
						Unshare = command.CascadeUnshare1,
						Delete = command.CascadeDelete1,
						Merge = command.CascadeMerge1,
						Reparent = command.CascadeReparent1
					}
				},
				Lookup = new LookupAttributeMetadata
				{
					DisplayName = CreateLookupDisplayName(command.LookupAttributeDisplayName1, table1, defaultLanguageCode),
					SchemaName = CreateLookupSchemaName(command.LookupAttributeSchemaName1, command.LookupAttributeDisplayName1, table1, publisherPrefix),
					Description = null,
					RequiredLevel = new AttributeRequiredLevelManagedProperty(command.LookupAttributeRequiredLevel1)
				}
			};
			output.WriteLine(" Done", ConsoleColor.Green);

			parentRequest.Requests.Add(request1);


			#endregion


			#region Creating lookup against table 2

			output.Write("Setting up CreateOneToManyRequest (2/2)...");

			var request2 = new CreateOneToManyRequest
			{
				SolutionUniqueName = currentSolutionName,
				OneToManyRelationship = new OneToManyRelationshipMetadata
				{
					ReferencingEntity = request.Entity.SchemaName,
					ReferencedEntity = command.Table2,
					SchemaName = CreateRelationshipSchemaName(request.Entity.SchemaName, command.Table2, publisherPrefix),
					AssociatedMenuConfiguration = new AssociatedMenuConfiguration
					{
						Behavior = command.MenuBehavior2,
						Group = command.MenuBehavior2 == AssociatedMenuBehavior.DoNotDisplay ? null : command.MenuGroup2,
						Label = CreateNNImplicitStrategy.GetLabel(command.MenuBehavior2, command.MenuLabel2, defaultLanguageCode),
						Order = command.MenuOrder2
					},
					CascadeConfiguration = new CascadeConfiguration
					{
						Assign = command.CascadeAssign2,
						Archive = command.CascadeArchive2,
						Share = command.CascadeShare2,
						Unshare = command.CascadeUnshare2,
						Delete = command.CascadeDelete2,
						Merge = command.CascadeMerge2,
						Reparent = command.CascadeReparent2
					}
				},
				Lookup = new LookupAttributeMetadata
				{
					DisplayName = CreateLookupDisplayName(command.LookupAttributeDisplayName2, table2, defaultLanguageCode),
					SchemaName = CreateLookupSchemaName(command.LookupAttributeSchemaName2, command.LookupAttributeDisplayName2, table2, publisherPrefix),
					Description = null,
					RequiredLevel = new AttributeRequiredLevelManagedProperty(command.LookupAttributeRequiredLevel2)
				}
			};
			output.WriteLine(" Done", ConsoleColor.Green);

			parentRequest.Requests.Add(request2);

			#endregion



			output.Write("Executing requests in transaction...");

			var parentResponse = (ExecuteTransactionResponse)await crm.ExecuteAsync(parentRequest);

			output.WriteLine(" Done", ConsoleColor.Green);

			var createTableResponse = (CreateEntityResponse)parentResponse.Responses[0];
			var createLookup1Response = (CreateOneToManyResponse)parentResponse.Responses[1];
			var createLookup2Response = (CreateOneToManyResponse)parentResponse.Responses[2];

			output.Write("Adding table to solution...");
			var requestX = new AddSolutionComponentRequest
			{
				AddRequiredComponents = true,
				ComponentId = createTableResponse.EntityId,
				ComponentType = 1, // Entity
				SolutionUniqueName = currentSolutionName,
				DoNotIncludeSubcomponents = false
			};

			await crm.ExecuteAsync(requestX);
			this.output.WriteLine("Done", ConsoleColor.Green);

			var result = CommandResult.Success();
			result["Entity ID"] = createTableResponse.EntityId;
			result["Primary Column ID"] = createTableResponse.AttributeId;
			result["Table 1 Relationship ID"] = createLookup1Response.RelationshipId;
			result["Table 1 Column ID"] = createLookup1Response.AttributeId;
			result["Table 2 Relationship ID"] = createLookup2Response.RelationshipId;
			result["Table 2 Column ID"] = createLookup2Response.AttributeId;
			return result;
		}














		private static Label SetTableDisplayName(CreateNNCommand command, EntityMetadata table1, EntityMetadata table2, int defaultLanguageCode)
		{
			if (!string.IsNullOrWhiteSpace(command.DisplayName))
			{
				return new Label(command.DisplayName, defaultLanguageCode);
			}

			var table1Name = table1.DisplayName.GetLocalizedLabel(defaultLanguageCode);
			var table2Name = table2.DisplayName.GetLocalizedLabel(defaultLanguageCode);

			return new Label($"{table1Name} - {table2Name}", defaultLanguageCode);
		}



		private static Label SetTableDisplayCollectionName(CreateNNCommand command, EntityMetadata table1, EntityMetadata table2, int defaultLanguageCode)
		{
			if (!string.IsNullOrWhiteSpace(command.DisplayCollectionName))
			{
				return new Label(command.DisplayCollectionName, defaultLanguageCode);
			}

			var table1Name = table1.DisplayCollectionName.GetLocalizedLabel(defaultLanguageCode);
			var table2Name = table2.DisplayCollectionName.GetLocalizedLabel(defaultLanguageCode);

			return new Label($"{table1Name} - {table2Name}", defaultLanguageCode);
		}

		private static Label SetTableDescription(CreateNNCommand command, int defaultLanguageCode)
		{
			if (!string.IsNullOrWhiteSpace(command.Description))
			{
				return new Label(command.Description, defaultLanguageCode);
			}

			return new Label($"Relationship between {command.Table1} and {command.Table2}", defaultLanguageCode);
		}

		private static string SetTableSchemaName(CreateNNCommand command, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(command.IntersectSchemaName))
			{
				if (!string.IsNullOrWhiteSpace(command.IntersectSchemaNameSuffix))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The intersection table schema name suffix cannot be specified when the intersection table schema name is specified. Current value is <{command.IntersectSchemaNameSuffix}>");

				if (!command.IntersectSchemaName.StartsWith(publisherPrefix + "_"))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The intersection table schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{command.IntersectSchemaName.Split("_").FirstOrDefault()}>");

				return command.IntersectSchemaName;
			}


			var sb = new StringBuilder();
			sb.Append(publisherPrefix);
			sb.Append('_');

			var tableName = command.Table1;
			if (tableName.StartsWith(publisherPrefix + "_"))
				sb.Append(tableName.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(tableName);

			sb.Append('_');

			tableName = command.Table2;
			if (tableName.StartsWith(publisherPrefix + "_"))
				sb.Append(tableName.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(tableName);

			if (!string.IsNullOrWhiteSpace(command.IntersectSchemaNameSuffix))
			{
				sb.Append('_');
				sb.Append(command.IntersectSchemaNameSuffix.TrimStart('_'));
			}

			return sb.ToString();
		}

		private static BooleanManagedProperty SetTableIsAuditEnabled(CreateNNCommand command)
		{
			return new BooleanManagedProperty(command.IsAuditEnabled);
		}

		private static Label SetPrimaryAttributeDisplayName(CreateNNCommand command, int defaultLanguageCode)
		{
			return new Label(command.PrimaryAttributeDisplayName, defaultLanguageCode);
		}

		private static string SetPrimaryAttributeSchemaName(CreateNNCommand command, Label displayNameLabel, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(command.PrimaryAttributeSchemaName))
			{
				if (!command.PrimaryAttributeSchemaName.StartsWith(publisherPrefix + "_"))
					throw new ArgumentException($"The primary attribute schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{publisherPrefix.Split("_").FirstOrDefault()}>");

				return command.PrimaryAttributeSchemaName ?? string.Empty;
			}

			var displayName = displayNameLabel.LocalizedLabels[0].Label;
			if (!string.IsNullOrWhiteSpace(displayName))
			{
				var namePart = displayName.OnlyLettersNumbersOrUnderscore();
				if (string.IsNullOrWhiteSpace(namePart))
					throw new ArgumentException($"Is not possible to infer the primary attribute schema name from the display name, please explicit a primary attribute schema name");

				return $"{publisherPrefix}_{namePart}";
			}

			return $"{publisherPrefix}_code";
		}

		private static Label? SetPrimaryAttributeDescription(CreateNNCommand command, int defaultLanguageCode)
		{
			if (string.IsNullOrWhiteSpace(command.PrimaryAttributeDescription))
				return null;

			return new Label(command.PrimaryAttributeDescription, defaultLanguageCode);
		}

		private static AttributeRequiredLevelManagedProperty SetPrimaryAttributeRequiredLevel(CreateNNCommand command)
		{
			return new AttributeRequiredLevelManagedProperty(command.PrimaryAttributeRequiredLevel);
		}

		private static int SetPrimaryAttributeMaxLength(CreateNNCommand command)
		{
			if (command.PrimaryAttributeMaxLength != null)
			{
				if (command.PrimaryAttributeMaxLength < 1)
					throw new ArgumentException($"The primary attribute max length must be greater than 0. Current value is <{command.PrimaryAttributeMaxLength}>");

				return command.PrimaryAttributeMaxLength.Value;
			}

			if (command.PrimaryAttributeAutoNumber == string.Empty)
			{
				return 100;
			}

			return 20;
		}

		private static string? SetPrimaryAttributeAutoNumberFormat(CreateNNCommand command, EntityMetadata table1, EntityMetadata table2, int defaultLanguageCode)
		{
			if (command.PrimaryAttributeAutoNumber == string.Empty)
				return null;

			if (command.PrimaryAttributeAutoNumber is null) 
			{
				var table1Name = table1.DisplayName.GetLocalizedLabel(defaultLanguageCode);
				var table2Name = table2.DisplayName.GetLocalizedLabel(defaultLanguageCode);

				return $"{table1Name.UpperCaseInitials()}{table2Name.UpperCaseInitials()}-{{SEQNUM:10}}";
			}

			return command.PrimaryAttributeAutoNumber;
		}




		private static Label CreateLookupDisplayName(string? displayName, EntityMetadata table1, int defaultLanguageCode)
		{
			if (!string.IsNullOrWhiteSpace(displayName))
			{
				return new Label(displayName, defaultLanguageCode);
			}

			return new Label(table1.DisplayName.GetLocalizedLabel(defaultLanguageCode), defaultLanguageCode);
		}


		private static string CreateLookupSchemaName(string? schemaName, string? displayName, EntityMetadata table, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(schemaName))
			{
				if (!schemaName.StartsWith(publisherPrefix + "_"))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The primary attribute schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{schemaName.Split("_").FirstOrDefault()}>");

				return schemaName;
			}

			if (!string.IsNullOrWhiteSpace(displayName))
			{
				var namePart = displayName.OnlyLettersNumbersOrUnderscore();
				if (string.IsNullOrWhiteSpace(namePart))
					throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the display name, please explicit a primary attribute schema name");

				return $"{publisherPrefix}_{namePart}";
			}

			
			if (table.SchemaName.StartsWith(publisherPrefix + "_"))
				return $"{table.SchemaName}id";

			if (!table.SchemaName.Contains('_'))
				return $"{publisherPrefix}_{table.SchemaName}id";

			var parentTableParts = table.SchemaName.Split("_");
			if (parentTableParts.Length != 2)
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the parent table name, please explicit a primary attribute schema name");

			if (string.IsNullOrWhiteSpace(parentTableParts[1]))
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the parent table name, please explicit a primary attribute schema name");

			return $"{publisherPrefix}_{parentTableParts[1]}id";
		}




		private static string CreateRelationshipSchemaName(string childTable, string parentTable, string publisherPrefix)
		{
			var sb = new StringBuilder();
			sb.Append(publisherPrefix);
			sb.Append('_');


			if (childTable.StartsWith(publisherPrefix + "_"))
				sb.Append(childTable.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(childTable);

			sb.Append('_');

			if (parentTable.StartsWith(publisherPrefix + "_"))
				sb.Append(parentTable.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(parentTable);

			return sb.ToString();
		}
	}
}
