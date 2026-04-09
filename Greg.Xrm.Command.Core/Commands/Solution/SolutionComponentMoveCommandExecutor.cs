using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class SolutionComponentMoveCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<SolutionComponentMoveCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SolutionComponentMoveCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Find source solution
				var sourceSolution = await FindSolutionAsync(crm, command.FromSolution, cancellationToken);
				if (sourceSolution == null)
				{
					return CommandResult.Fail($"Source solution '{command.FromSolution}' not found.");
				}

				// Find target solution
				var targetSolution = await FindSolutionAsync(crm, command.ToSolution, cancellationToken);
				if (targetSolution == null)
				{
					return CommandResult.Fail($"Target solution '{command.ToSolution}' not found.");
				}

				// Find component
				var componentTypeCode = GetComponentTypeCode(command.ComponentType);
				var component = await FindComponentAsync(crm, command.ComponentName, componentTypeCode, sourceSolution.Id, cancellationToken);
				if (component == null)
				{
					return CommandResult.Fail($"Component '{command.ComponentName}' not found in source solution.");
				}

				if (command.DryRun)
				{
					output.WriteLine("[DRY RUN] Would move:", ConsoleColor.Yellow);
					output.WriteLine($"  Component: {command.ComponentName} ({command.ComponentType})");
					output.WriteLine($"  From: {command.FromSolution}");
					output.WriteLine($"  To: {command.ToSolution}");
					output.WriteLine($"  Include dependencies: {command.IncludeDependencies}");
					return CommandResult.Success();
				}

				// Add component to target solution
				var request = new AddSolutionComponentRequest
				{
					ComponentType = componentTypeCode,
					ComponentId = component.Id,
					SolutionUniqueName = command.ToSolution,
					AddRequiredComponents = command.IncludeDependencies
				};

				output.Write($"Moving {command.ComponentName} to {command.ToSolution}...");
				await crm.ExecuteAsync(request, cancellationToken);
				output.WriteLine(" Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Component move error: {ex.Message}", ex);
			}
		}

		private async Task<Entity?> FindSolutionAsync(IOrganizationServiceAsync2 crm, string solutionName, CancellationToken ct)
		{
			var query = new QueryExpression("solution");
			query.ColumnSet.AddColumn("solutionid");
			query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);
			var result = await crm.RetrieveMultipleAsync(query, ct);
			return result.Entities.Count > 0 ? result.Entities[0] : null;
		}

		private async Task<Entity?> FindComponentAsync(IOrganizationServiceAsync2 crm, string componentName, int componentType, Guid solutionId, CancellationToken ct)
		{
			var query = new QueryExpression("solutioncomponent");
			query.ColumnSet.AddColumn("objectid");
			query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
			query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, componentType);

			var result = await crm.RetrieveMultipleAsync(query, ct);
			return result.Entities.Count > 0 ? result.Entities[0] : null;
		}

		private static int GetComponentTypeCode(string typeName) => typeName.ToLowerInvariant() switch
		{
			"entity" => 1,
			"attribute" => 2,
			"relationship" => 3,
			"optionset" or "picklist" => 9,
			"plugin" or "plugintype" => 20,
			"webresource" => 23,
			"workflow" => 29,
			"step" or "sdkmessageprocessingstep" => 31,
			"image" or "sdkmessageprocessingstepimage" => 32,
			"canvasapp" => 35,
			"connector" => 37,
			"connectionreference" => 38,
			"customapi" => 60,
			_ => throw new ArgumentException($"Unknown component type: {typeName}")
		};
	}
}
