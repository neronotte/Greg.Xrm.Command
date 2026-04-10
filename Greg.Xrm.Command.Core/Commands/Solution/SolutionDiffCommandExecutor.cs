using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class SolutionDiffCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<SolutionDiffCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SolutionDiffCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var sourceComponents = await GetSolutionComponentsAsync(crm, command.Source, command.ComponentType, cancellationToken);
				var targetComponents = await GetSolutionComponentsAsync(crm, command.Target, command.ComponentType, cancellationToken);

				var sourceSet = new HashSet<string>(sourceComponents.Select(c => $"{c.Type}:{c.Name}"));
				var targetSet = new HashSet<string>(targetComponents.Select(c => $"{c.Type}:{c.Name}"));

				var added = targetComponents.Where(c => !sourceSet.Contains($"{c.Type}:{c.Name}")).ToList();
				var removed = sourceComponents.Where(c => !targetSet.Contains($"{c.Type}:{c.Name}")).ToList();
				var common = sourceComponents.Where(c => targetSet.Contains($"{c.Type}:{c.Name}")).ToList();

				var changes = new List<SolutionChange>();
				foreach (var c in added) changes.Add(new SolutionChange { Type = "Added", ComponentType = c.Type, Name = c.Name });
				foreach (var c in removed) changes.Add(new SolutionChange { Type = "Removed", ComponentType = c.Type, Name = c.Name });

				if (changes.Count == 0)
				{
					output.WriteLine("No differences found between the two solutions.", ConsoleColor.Green);
					return CommandResult.Success();
				}

				if (command.Format == "json")
				{
					var json = Newtonsoft.Json.JsonConvert.SerializeObject(
						new { Source = command.Source, Target = command.Target, Changes = changes },
						Newtonsoft.Json.Formatting.Indented);
					output.WriteLine(json);
				}
				else
				{
					output.WriteLine($"Diff: {command.Source} vs {command.Target}", ConsoleColor.Cyan);
					output.WriteLine($"  Added: {added.Count}, Removed: {removed.Count}, Unchanged: {common.Count}");
					output.WriteLine();
					output.WriteTable(changes,
						() => new[] { "Change", "Type", "Name" },
						c => new[] { c.Type, c.ComponentType, c.Name },
						c => c.Type == "Added" ? ConsoleColor.Green : ConsoleColor.Red
					);
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Solution diff error: {ex.Message}", ex);
			}
		}

		private async Task<List<(string Type, string Name)>> GetSolutionComponentsAsync(
			IOrganizationServiceAsync2 crm, string solutionName, string? componentTypeFilter, CancellationToken ct)
		{
			var components = new List<(string Type, string Name)>();

			var solutionQuery = new QueryExpression("solution");
			solutionQuery.ColumnSet.AddColumn("solutionid");
			solutionQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);
			var solutionResult = await crm.RetrieveMultipleAsync(solutionQuery, ct);

			if (solutionResult.Entities.Count == 0)
			{
				return components;
			}

			var solutionId = solutionResult.Entities[0].Id;

			var componentQuery = new QueryExpression("solutioncomponent");
			componentQuery.ColumnSet.AddColumns("componenttype", "objectid");
			componentQuery.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);

			var componentResult = await crm.RetrieveMultipleAsync(componentQuery, ct);

			foreach (var comp in componentResult.Entities)
			{
				var componentType = comp.GetAttributeValue<int>("componenttype");
				var objectId = comp.GetAttributeValue<Guid>("objectid");
				var typeName = GetComponentTypeName(componentType);

				if (!string.IsNullOrEmpty(componentTypeFilter) && !typeName.Equals(componentTypeFilter, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				// Resolve actual component name from the target entity
				var componentName = await ResolveComponentNameAsync(crm, componentType, objectId, ct);
				components.Add((typeName, componentName));
			}

			return components;
		}

		private async Task<string> ResolveComponentNameAsync(IOrganizationServiceAsync2 crm, int componentType, Guid objectId, CancellationToken ct)
		{
			try
			{
				var entityName = GetComponentEntityName(componentType);
				if (string.IsNullOrEmpty(entityName))
				{
					return $"Component_{componentType}_{objectId}";
				}

				var query = new QueryExpression(entityName);
				query.ColumnSet.AddColumn("logicalname");
				query.Criteria.AddCondition($"{entityName}id", ConditionOperator.Equal, objectId);
				query.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(query, ct);
				if (result.Entities.Count > 0)
				{
					return result.Entities[0].GetAttributeValue<string>("logicalname") ?? $"Component_{componentType}";
				}
			}
			catch
			{
				// If we can't resolve the name, fall back to component type
			}

			return $"Component_{componentType}_{objectId}";
		}

		private static string GetComponentEntityName(int typeCode) => typeCode switch
		{
			1 => "entity",
			2 => "attribute",
			3 => "relationship",
			9 => "optionset",
			20 => "plugintype",
			23 => "webresource",
			29 => "workflow",
			31 => "sdkmessageprocessingstep",
			32 => "sdkmessageprocessingstepimage",
			60 => "customapi",
			_ => null
		};

		private static string GetComponentTypeName(int typeCode) => typeCode switch
		{
			1 => "Entity",
			2 => "Attribute",
			3 => "Relationship",
			4 => "AttributePicklistValue",
			5 => "AttributeLookupValue",
			6 => "ViewAttribute",
			7 => "LocalizedLabel",
			8 => "RelationshipExtraCondition",
			9 => "OptionSet",
			10 => "EntityRelationship",
			11 => "EntityRelationshipRole",
			12 => "EntityRelationshipRelationships",
			20 => "PluginType",
			23 => "WebResource",
			29 => "Workflow",
			31 => "SdkMessageProcessingStep",
			32 => "SdkMessageProcessingStepImage",
			33 => "ServiceEndpoint",
			35 => "CanvasApp",
			37 => "Connector",
			38 => "ConnectionReference",
			60 => "CustomAPI",
			61 => "CustomAPIRequestParameter",
			62 => "CustomAPIResponseProperty",
			_ => $"Unknown({typeCode})"
		};

		private class SolutionChange
		{
			public string Type { get; set; } = "";
			public string ComponentType { get; set; } = "";
			public string Name { get; set; } = "";
		}
	}
}
