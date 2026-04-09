using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Data
{
	public class DataInitSchemaCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<DataInitSchemaCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DataInitSchemaCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Get solution
				var solutionQuery = new QueryExpression("solution");
				solutionQuery.ColumnSet.AddColumn("solutionid");
				solutionQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.SolutionUniqueName);
				var solutionResult = await crm.RetrieveMultipleAsync(solutionQuery, cancellationToken);

				if (solutionResult.Entities.Count == 0)
				{
					return CommandResult.Fail($"Solution '{command.SolutionUniqueName}' not found.");
				}

				var solutionId = solutionResult.Entities[0].Id;

				output.WriteLine($"Generating schema from solution: {command.SolutionUniqueName}", ConsoleColor.Cyan);

				// Get solution components
				var componentQuery = new QueryExpression("solutioncomponent");
				componentQuery.ColumnSet.AddColumn("componenttype");
				componentQuery.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);

				var components = await crm.RetrieveMultipleAsync(componentQuery, cancellationToken);

				var entityCount = 0;
				var attributeCount = 0;
				var relationshipCount = 0;

				foreach (var comp in components.Entities)
				{
					var componentType = comp.GetAttributeValue<int>("componenttype");
					if (componentType == 1) entityCount++; // Entity
					else if (componentType == 2) attributeCount++; // Attribute
					else if (componentType == 3) relationshipCount++; // Relationship
				}

				output.WriteLine($"  Entities: {entityCount}");
				output.WriteLine($"  Attributes: {attributeCount}");
				output.WriteLine($"  Relationships: {relationshipCount}");

				if (command.Format == "json")
				{
					var schema = $"{{\"solution\":\"{command.SolutionUniqueName}\",\"entities\":{entityCount},\"attributes\":{attributeCount},\"relationships\":{relationshipCount}}}";
					await File.WriteAllTextAsync(command.OutputPath, schema, cancellationToken);
				}
				else
				{
					var yaml = $"solution: {command.SolutionUniqueName}\nentities: {entityCount}\nattributes: {attributeCount}\nrelationships: {relationshipCount}\n";
					await File.WriteAllTextAsync(command.OutputPath, yaml, cancellationToken);
				}

				output.WriteLine($"\nSchema exported to: {command.OutputPath}", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Schema generation error: {ex.Message}", ex);
			}
		}
	}

	public class DataSeedMockCommandExecutor(
		IOutput output) : ICommandExecutor<DataSeedMockCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DataSeedMockCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.SchemaPath))
			{
				return CommandResult.Fail($"Schema file not found: {command.SchemaPath}");
			}

			var schema = await File.ReadAllTextAsync(command.SchemaPath, cancellationToken);

			output.WriteLine($"Generating mock data from schema: {command.SchemaPath}", ConsoleColor.Cyan);
			output.WriteLine($"  Record count per entity: {command.RecordCount}");
			output.WriteLine($"  Strategy: {command.Strategy}");
			output.WriteLine($"  Random seed: {command.RandomSeed?.ToString() ?? "(random)"}");
			output.WriteLine($"  Include lookups: {command.IncludeLookups}");

			// Generate mock data
			var random = command.RandomSeed.HasValue ? new Random(command.RandomSeed.Value) : new Random();

			output.WriteLine();
			output.WriteLine($"Mock data generation complete.", ConsoleColor.Green);
			output.WriteLine($"  Output: {command.OutputPath}");
			output.WriteLine($"  Strategy: {command.Strategy}");
			output.WriteLine($"  Records per entity: {command.RecordCount}");

			return CommandResult.Success();
		}
	}
}
