using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.Text;

namespace Greg.Xrm.Command.Commands.Relationship
{
    public class CreatePolyCommandExecutor : ICommandExecutor<CreatePolyCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public CreatePolyCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}



		public async Task<CommandResult> ExecuteAsync(CreatePolyCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();

				var currentSolutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(currentSolutionName))
				{
					currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (currentSolutionName == null)
					{
						return CommandResult.Fail("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.");
					}
				}






				output.WriteLine("Checking solution existence and retrieving publisher prefix");

				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("ismanaged");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, currentSolutionName);
				var link = query.AddLink("publisher", "publisherid", "publisherid");
				link.Columns.AddColumns("customizationprefix");
				link.EntityAlias = "publisher";
				query.NoLock = true;
				query.TopCount = 1;


				var solutionList = (await crm.RetrieveMultipleAsync(query)).Entities;
				if (solutionList.Count == 0)
				{
					return CommandResult.Fail($"Invalid solution name: {currentSolutionName}");
				}

				var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
				if (managed)
				{
					return CommandResult.Fail("The provided solution is managed. You must specify an unmanaged solution.");
				}

				var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
				if (string.IsNullOrWhiteSpace(publisherPrefix))
				{
					return CommandResult.Fail("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.");
				}


				var parents = command.Parents.Split(",|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				if (parents.Length == 0)
				{
					return CommandResult.Fail($"The parent table must be specified. Current value: <{command.Parents}>");
				}





				await crm.CheckManyToOneEligibilityAsync(parents, command.ChildTable);


				output.Write("Executing CreatePolymorphicLookupAttribute Request...");

				var relationshipList = new List<OneToManyRelationshipMetadata>();
				foreach (var parent in parents)
				{
					var r = new OneToManyRelationshipMetadata
					{
						ReferencingEntity = command.ChildTable,
						ReferencedEntity = parent,
						SchemaName = CreateRelationshipSchemaName(command, parent, publisherPrefix),
						AssociatedMenuConfiguration = new AssociatedMenuConfiguration
						{
							Behavior = command.MenuBehavior,
							Group = CreateMenuGroup(command),
							Label = CreateMenuLabel(command, defaultLanguageCode),
							Order = command.MenuOrder
						},
						CascadeConfiguration = new CascadeConfiguration
						{
							Assign = command.CascadeAssign,
							Archive = command.CascadeArchive,
							Share = command.CascadeShare,
							Unshare = command.CascadeUnshare,
							Delete = command.CascadeDelete,
							Merge = command.CascadeMerge,
							Reparent = command.CascadeReparent
						}
					};
					relationshipList.Add(r);
				}



				var request = new OrganizationRequest("CreatePolymorphicLookupAttribute");
				request["SolutionUniqueName"] = currentSolutionName;
				request["Lookup"] = new LookupAttributeMetadata
				{
					DisplayName = CreateLookupDisplayName(command, defaultLanguageCode),
					SchemaName = CreateLookupSchemaName(command, publisherPrefix),
					Description = null,
					RequiredLevel = new AttributeRequiredLevelManagedProperty(command.RequiredLevel)
				};
				request["OneToManyRelationships"] = relationshipList.ToArray();

				this.output.WriteLine("Done", ConsoleColor.Green);



				this.output.Write("Executing request...");

				var response = await crm.ExecuteAsync(request);

				this.output.WriteLine("Done", ConsoleColor.Green);


				var result = CommandResult.Success();
				result["Lookup Column ID"] = response["AttributeId"];
				result["Relationship IDs"] = string.Join(", ", ((Guid[])response["RelationshipIds"]).Select(x => x.ToString()));
				return result;
			}
			catch (Exception ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}


		private static AssociatedMenuGroup? CreateMenuGroup(CreatePolyCommand command)
		{
			if (command.MenuBehavior == AssociatedMenuBehavior.DoNotDisplay)
				return null;

			return command.MenuGroup;
		}

		private static Label? CreateMenuLabel(CreatePolyCommand command, int defaultLanguageCode)
		{
			if (command.MenuBehavior != AssociatedMenuBehavior.UseLabel)
				return null;

			if (string.IsNullOrWhiteSpace(command.MenuLabel))
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "The menu label is required when the menu behavior is set to UseLabel");

			return new Label(command.MenuLabel, defaultLanguageCode);
		}

		private static string CreateRelationshipSchemaName(CreatePolyCommand command, string parentTable, string publisherPrefix)
		{
			var sb = new StringBuilder();
			sb.Append(publisherPrefix);
			sb.Append('_');


			var childTable = command.ChildTable ?? string.Empty;
			if (childTable.StartsWith(publisherPrefix + "_"))
				sb.Append(childTable.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(childTable);

			sb.Append('_');

			if (parentTable.StartsWith(publisherPrefix + "_"))
				sb.Append(parentTable.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(parentTable);

			if (!string.IsNullOrWhiteSpace(command.RelationshipNameSuffix))
			{
				sb.Append('_');
				sb.Append(command.RelationshipNameSuffix);
			}
			else 
			{
				var suffix = command.LookupAttributeDisplayName.OnlyLettersNumbersOrUnderscore();

				sb.Append('_');
				sb.Append(suffix);
			}

			return sb.ToString();
		}

		private static string CreateLookupSchemaName(CreatePolyCommand command, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(command.LookupAttributeSchemaName))
			{
				if (!command.LookupAttributeSchemaName.StartsWith(publisherPrefix + "_"))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The primary attribute schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{command.LookupAttributeSchemaName.Split("_").FirstOrDefault()}>");

				return command.LookupAttributeSchemaName;
			}


			var namePart = command.LookupAttributeDisplayName.OnlyLettersNumbersOrUnderscore();
			if (string.IsNullOrWhiteSpace(namePart))
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the display name, please explicit a primary attribute schema name");

			if (!namePart.EndsWith("id")) namePart += "id";

			return $"{publisherPrefix}_{namePart}";
		}


		private static Label CreateLookupDisplayName(CreatePolyCommand command, int defaultLanguageCode)
		{
			return new Label(command.LookupAttributeDisplayName, defaultLanguageCode);
		}
	}
}
