using Greg.Xrm.Command.Commands.Solution.Extensions;
using Greg.Xrm.Command.Commands.Solution.Writers;
using Greg.Xrm.Command.Commands.Solution.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Concurrent;

namespace Greg.Xrm.Command.Commands.Solution.Service
{
	public class ConstantsGeneratorService : IConstantsGeneratorService
	{
		private readonly IOutput output;

		public ConstantsGeneratorService(IOutput output)
		{
			this.output = output;
		}
		private static readonly string[] EntityCommonFields = new string[]
		{
			"createdby", "createdon", "createdonbehalfby", "importsequencenumber",
			"modifiedby", "modifiedon", "modifiedonbehalfby", "overriddencreatedon",
			"ownerid", "owningbusinessunit", "owningteam", "owninguser",
			"statecode", "statuscode", "timezoneruleversionnumber",
			"utcconversiontimezonecode", "versionnumber"
		};

		public async Task<(int csFiles, int jsFiles)> GenerateAsync(
			IOrganizationServiceAsync2 crm,
			ConstantsOutputRequest request,
			CancellationToken cancellationToken)
		{
			output.WriteLine("Reading info from Dataverse...", ConsoleColor.Gray);

			// 1. Retrieve all entity metadata (entities + attributes + relationships)
			output.WriteLine("  Reading entities...", ConsoleColor.Gray);
			var entitiesMetadata = ((RetrieveAllEntitiesResponse)await crm.ExecuteAsync(new RetrieveAllEntitiesRequest
			{
				EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships
			})).EntityMetadata;

			// 2. Retrieve all global option sets
			output.WriteLine("  Reading global option sets...", ConsoleColor.Gray);
			var optionSetsResponse = (RetrieveAllOptionSetsResponse)await crm.ExecuteAsync(new RetrieveAllOptionSetsRequest());

			var globalOptionSetsMetadata = optionSetsResponse.OptionSetMetadata
				.Where(os => os is OptionSetMetadata osm && osm.Options.Count > 0)
				.Select(os => GetGlobalOptionSetsMetadataManager(os))
				.OrderBy(os => os.LogicalName)
				.ToList();

			var globalBooleanOptionSetsMetadata = optionSetsResponse.OptionSetMetadata
				.Where(os => os is BooleanOptionSetMetadata)
				.Select(os => GetGlobalOptionSetsMetadataManager(os))
				.OrderBy(os => os.LogicalName)
				.ToList();

			// 3. Get entity GUIDs from solution
			output.WriteLine("  Getting entity metadata from solution...", ConsoleColor.Gray);
			var solutionEntityGuids = await GetEntityGuidsFromSolutionAsync(crm, request.SolutionName);

			// 4. Filter entities to those in the solution, add activitypointer if any activity entity is present
			output.WriteLine("  Checking for activity entities...", ConsoleColor.Gray);
			var accountMetadata = entitiesMetadata.FirstOrDefault(e => e.LogicalName == "account");
			var activityPointerMetadata = entitiesMetadata.FirstOrDefault(e => e.LogicalName == "activitypointer");

			var filteredEntities = entitiesMetadata
				.Where(e => solutionEntityGuids.Contains(e.MetadataId ?? Guid.Empty))
				.ToList();

			if (filteredEntities.Any(e => e.IsActivity == true) &&
				!filteredEntities.Any(e => e.LogicalName == "activitypointer") &&
				activityPointerMetadata != null)
			{
				filteredEntities.Add(activityPointerMetadata);
			}

			// 5. Build EntityMetadataManager list (parallel, same as original MaxDegreeOfParallelism=1)
			output.WriteLine("  Transforming metadata...", ConsoleColor.Gray);
			var entityConcurrentData = new ConcurrentBag<EntityMetadataManager>();
			var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 };

			Parallel.ForEach(filteredEntities, parallelOptions, entMetadata =>
			{
				if (entMetadata.DisplayName.LocalizedLabels.Count <= 0)
					return;

				var label = entMetadata.DisplayName.LocalizedLabels.FirstOrDefault()?.Label ?? string.Empty;
				output.WriteLine($"    {label}", ConsoleColor.Gray);
				var entityManager = new EntityMetadataManager(label, entMetadata.LogicalName, entMetadata.IsActivity == true, EntityCommonFields.ToList());

				foreach (var attribute in entMetadata.Attributes)
				{
					if (attribute.DisplayName.LocalizedLabels.Count > 0)
					{
						var attrManager = GetAttributeElementFromMetadata(attribute, entMetadata.LogicalName);
						entityManager.AddAttribute(attrManager);
					}
				}
				entityManager.OptionSetAttributes = entityManager.OptionSetAttributes
					.OrderBy(opt => opt.LogicalNameConstant).ToList();
				entityConcurrentData.Add(entityManager);
			});

			var entityData = entityConcurrentData.OrderBy(e => e.EntityLogicalName).ToList();

			// 6. Build EntityGenericConstants (common fields from account entity)
			var activityPointerManagerData = entityData.FirstOrDefault(e => e.EntityLogicalName == "activitypointer");

			EntityMetadataManager entityCommonAttributes;
			if (accountMetadata != null)
			{
				var commonAttrList = accountMetadata.Attributes
					.Where(a => EntityCommonFields.Contains(a.LogicalName))
					.ToList();

				entityCommonAttributes = new EntityMetadataManager("Entity Generic", "EntityGenericConstants", false, new List<string>());
				foreach (var attr in commonAttrList)
				{
					if (attr.DisplayName.LocalizedLabels.Count <= 0)
						continue;
					entityCommonAttributes.AddAttribute(GetAttributeElementFromMetadata(attr, null));
				}
			}
			else
			{
				entityCommonAttributes = new EntityMetadataManager("Entity Generic", "EntityGenericConstants", false, new List<string>());
			}
			entityData.Insert(0, entityCommonAttributes);

			// 7. Filter activity attributes (remove activitypointer attrs from activity entities)
			if (activityPointerManagerData != null)
			{
				var activityPointerAttributeNames = activityPointerManagerData.Attributes
					.Select(a => a.LogicalNameConstant)
					.OrderBy(n => n)
					.ToList();

				foreach (var entityManager in entityData)
				{
					if (!entityManager.IsActivity) continue;
					entityManager.Attributes = entityManager.Attributes
						.Where(a => !activityPointerAttributeNames.Contains(a.LogicalNameConstant))
						.ToList();
				}
			}

			int csFiles = 0;
			int jsFiles = 0;

			// 8. Write C# files
			if (!string.IsNullOrWhiteSpace(request.OutputCs) && !string.IsNullOrWhiteSpace(request.NamespaceCs))
			{
				output.WriteLine("Writing C# files...", ConsoleColor.Gray);
				var writer = new WriteConstantsToFileCs(
					request.OutputCs!,
					request.NamespaceCs!,
					entityData,
					activityPointerManagerData,
					globalOptionSetsMetadata,
					globalBooleanOptionSetsMetadata,
					request.WithTypes,
					request.WithDescriptions,
					output);
				csFiles = writer.WriteConstantsToFile();
			}

			// 9. Write JS files
			if (!string.IsNullOrWhiteSpace(request.OutputJs) && !string.IsNullOrWhiteSpace(request.NamespaceJs))
			{
				output.WriteLine("Writing JS files...", ConsoleColor.Gray);
				var writer = new WriteConstantsToFileJs(
					request.OutputJs!,
					request.NamespaceJs!,
					request.JsHeader ?? string.Empty,
					entityData,
					globalOptionSetsMetadata,
					globalBooleanOptionSetsMetadata,
					output);
				jsFiles = writer.WriteConstantsToFile();
			}

			return (csFiles, jsFiles);
		}

		private static async Task<HashSet<Guid>> GetEntityGuidsFromSolutionAsync(IOrganizationServiceAsync2 crm, string solutionName)
		{
			var solutionQuery = new QueryExpression("solution");
			solutionQuery.Criteria.AddCondition(new ConditionExpression("uniquename", ConditionOperator.Equal, solutionName));
			var solutions = await crm.RetrieveMultipleAsync(solutionQuery);
			var solution = solutions.Entities.FirstOrDefault()
				?? throw new InvalidOperationException("Invalid solution name: " + solutionName);

			var componentQuery = new QueryExpression("solutioncomponent");
			componentQuery.ColumnSet.AllColumns = true;
			componentQuery.Criteria.AddCondition(new ConditionExpression("solutionid", ConditionOperator.Equal, solution.Id));
			componentQuery.Criteria.AddCondition(new ConditionExpression("componenttype", ConditionOperator.Equal, 1)); // Entity

			var components = await crm.RetrieveMultipleAsync(componentQuery);
			return components.Entities
				.Where(x => x.Contains("objectid"))
				.Select(x => x.GetAttributeValue<Guid>("objectid"))
				.ToHashSet();
		}

		private static GlobalOptionSetsMetadataManager GetGlobalOptionSetsMetadataManager(OptionSetMetadataBase opt)
		{
			var label = opt.DisplayName.LocalizedLabels.FirstOrDefault();
			var displayName = label?.Label ?? string.Empty;
			return new GlobalOptionSetsMetadataManager(displayName, opt.Name, GetOptionSetValuesFromMetadata(opt));
		}

		private static Dictionary<int, string> GetOptionSetValuesFromMetadata(OptionSetMetadataBase optionSets)
		{
			var result = new Dictionary<int, string>();

			if (optionSets is OptionSetMetadata options)
			{
				foreach (var option in options.Options)
				{
					var value = option.Value;
					if (value == null) continue;
					var label = option.Label.LocalizedLabels[0].Label;
					result[value.Value] = label;
				}
			}
			else if (optionSets is BooleanOptionSetMetadata boolOptions)
			{
				var trueOption = boolOptions.TrueOption;
				var falseOption = boolOptions.FalseOption;
				if (trueOption?.Value == null || falseOption?.Value == null)
					return result;

				result.Add(trueOption.Value.Value, trueOption.Label.LocalizedLabels[0].Label);
				result.Add(falseOption.Value.Value, falseOption.Label.LocalizedLabels[0].Label);
			}

			return result;
		}

		private static List<Tuple<int, int, string>> GetStatusReasonValuesFromMetadata(OptionSetMetadata optionSets)
		{
			var result = new List<Tuple<int, int, string>>();
			foreach (var option in optionSets.Options.OfType<StatusOptionMetadata>())
			{
				result.Add(new Tuple<int, int, string>(
					option.Value!.Value,
					option.State!.Value,
					option.Label.LocalizedLabels[0].Label));
			}
			return result;
		}

		private static AttributeMetadataManager GetAttributeElementFromMetadata(
            AttributeMetadata attribute,
			string? entityLogicalName)
		{
			var label = attribute.DisplayName.LocalizedLabels.FirstOrDefault()?.Label;

			var description = string.Empty;
			if (attribute.Description.LocalizedLabels.Count != 0)
				description = attribute.Description.LocalizedLabels[0].Label
					.Replace("\n", string.Empty)
					.Replace("\t", string.Empty);

			if (attribute.AttributeType == AttributeTypeCode.Lookup)
			{
				var targets = ((LookupAttributeMetadata)attribute).Targets.ToList();
				return new AttributeMetadataManagerForLookup(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description, targets);
			}

			if (attribute.AttributeType == AttributeTypeCode.Picklist)
			{
				var optionSet = ((EnumAttributeMetadata)attribute).OptionSet;
				var globalOptionSetName = optionSet.GetGlobalOptionSetName();
				var values = GetOptionSetValuesFromMetadata(optionSet);
				return new AttributeMetadataManagerForPicklist(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description, values, globalOptionSetName);
			}

			if (attribute.AttributeType == AttributeTypeCode.Virtual)
			{
				if (attribute is MultiSelectPicklistAttributeMetadata)
				{
					var optionSet = ((EnumAttributeMetadata)attribute).OptionSet;
					var globalOptionSetName = optionSet.GetGlobalOptionSetName();
					var values = GetOptionSetValuesFromMetadata(optionSet);
					return new AttributeMetadataManagerForPicklist(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description, values, globalOptionSetName);
				}
				return new AttributeMetadataManager(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description);
			}

			if (attribute.AttributeType == AttributeTypeCode.Boolean)
			{
				var optionSet = ((BooleanAttributeMetadata)attribute).OptionSet;
				var values = GetOptionSetValuesFromMetadata(optionSet);
				var globalOptionSetName = optionSet.GetGlobalOptionSetName();
				return new AttributeMetadataManagerForPicklist(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description, values, globalOptionSetName);
			}

			if (attribute.AttributeType == AttributeTypeCode.State)
			{
				var values = GetOptionSetValuesFromMetadata(((EnumAttributeMetadata)attribute).OptionSet);
				return new AttributeMetadataManagerForStatus(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description, values);
			}

			if (attribute.AttributeType == AttributeTypeCode.Status)
			{
				var values = GetStatusReasonValuesFromMetadata((OptionSetMetadata)((EnumAttributeMetadata)attribute).OptionSet);
				return new AttributeMetadataManagerForStatusReason(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description, values);
			}

			return new AttributeMetadataManager(entityLogicalName, label, attribute.LogicalName, attribute.AttributeType.ToString(), description);
		}
	}
}
