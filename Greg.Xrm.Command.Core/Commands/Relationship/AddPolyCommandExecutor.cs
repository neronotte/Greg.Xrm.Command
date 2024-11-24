using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Text;

namespace Greg.Xrm.Command.Commands.Relationship
{
	public class AddPolyCommandExecutor(
		IOutput output, 
		IOrganizationServiceRepository organizationServiceRepository) 
		: ICommandExecutor<AddPolyCommand>
	{
		private readonly IOutput output = output;
		private readonly IOrganizationServiceRepository organizationServiceRepository = organizationServiceRepository;



		public async Task<CommandResult> ExecuteAsync(AddPolyCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);



			this.output.Write($"Retrieving metadata for entity {command.ChildTable}...");
			EntityMetadata entityMetadata;
			try
			{
				var request = new RetrieveEntityRequest();
				request.LogicalName = command.ChildTable;
				request.EntityFilters = EntityFilters.Attributes | EntityFilters.Relationships;

				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);

				entityMetadata = response.EntityMetadata;
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				this.output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			string publisherPrefix;
			try
			{
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

				var pp = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
				if (string.IsNullOrWhiteSpace(pp))
				{
					return CommandResult.Fail("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.");
				}

				publisherPrefix = pp;
			}
			catch (Exception ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}

			




			try
			{

				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();


				var lookupColumn = entityMetadata.Attributes.OfType<LookupAttributeMetadata>().FirstOrDefault(x => x.LogicalName == command.LookupColumnName);
				if (lookupColumn == null)
				{
					return CommandResult.Fail($"There's no lookup column with the name provided ({command.LookupColumnName}).");
				}


				var relationshipList = Array.FindAll(entityMetadata.ManyToOneRelationships, x => x.ReferencingAttribute == command.LookupColumnName);
				if (relationshipList == null || relationshipList.Length == 0)
				{
					return CommandResult.Fail($"There's no relationship pointed by the lookup column provided ({command.LookupColumnName}).");
				}

				var existingRelationship = Array.Find(relationshipList, x => x.ReferencedEntity == command.ParentTable);
				if (existingRelationship != null)
				{
					return CommandResult.Fail($"The relationship between {command.ChildTable} and {command.ParentTable} already exists!");
				}



				await crm.CheckManyToOneEligibilityAsync(command.ParentTable, command.ChildTable);




				var request = new CreateOneToManyRequest
				{
					OneToManyRelationship = new OneToManyRelationshipMetadata
					{
						SchemaName = CreateRelationshipSchemaName(command, command.ParentTable, publisherPrefix, lookupColumn.DisplayName.UserLocalizedLabel.Label),
						ReferencedEntity = command.ParentTable,
						ReferencingEntity = command.ChildTable,
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
					}
				};
				request["Lookup"] = lookupColumn;




				this.output.Write("Executing request...");
				var response = await crm.ExecuteAsync(request);
				this.output.WriteLine("Done", ConsoleColor.Green);



				var result = CommandResult.Success();
				result["Lookup Column ID"] = response["AttributeId"];
				result["Relationship ID"] = response["RelationshipId"];
				return result;
			}
			catch(Exception ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}



		private static string CreateRelationshipSchemaName(AddPolyCommand command, string parentTable, string publisherPrefix, string lookupDisplayName)
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
				var suffix = lookupDisplayName.OnlyLettersNumbersOrUnderscore();

				sb.Append('_');
				sb.Append(suffix);
			}

			return sb.ToString();
		}
	}
}
