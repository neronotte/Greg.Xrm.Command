using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentAddTableCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository
		) : ICommandExecutor<ComponentAddTableCommand>
	{

		public async Task<CommandResult> ExecuteAsync(ComponentAddTableCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			var solutionName = command.SolutionUniqueName;
			if (string.IsNullOrWhiteSpace(solutionName))
			{
				solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
			}
			if (string.IsNullOrWhiteSpace(solutionName))
			{
				return CommandResult.Fail("No solution specified and no default solution found.");
			}


			output.Write($"Retrieving solution {solutionName}...");
			var solution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
			if (solution == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Solution with unique name '{solutionName}' not found.");
			}
			output.WriteLine("Done", ConsoleColor.Green);
			if (solution.ismanaged)
			{
				return CommandResult.Fail($"Solution '{solutionName}' is managed. Cannot add components to a managed solution.");
			}



			output.Write($"Looking for table {command.TableName}...");
			Guid? componentId = null;
			try
			{
				var query = new EntityQueryExpression
				{
					Properties = new MetadataPropertiesExpression("MetadataId", "SchemaName", "LogicalName", "DisplayName"),
					Criteria = new MetadataFilterExpression
					{
						Conditions =
						{
							new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, command.TableName)
						}
					}
				};

				var request = new RetrieveMetadataChangesRequest
				{
					Query = query
				};

				var response = (RetrieveMetadataChangesResponse)await crm.ExecuteAsync(request, cancellationToken);

				var entity = response.EntityMetadata.FirstOrDefault();
				if (entity == null)
				{
					output.Write("failed via logical name, trying with display name...", ConsoleColor.Yellow);

					var request2 = new RetrieveAllEntitiesRequest
					{
						EntityFilters = EntityFilters.Entity
					};

					var response2  = (RetrieveAllEntitiesResponse)await crm.ExecuteAsync(request2, cancellationToken);

					entity = response2.EntityMetadata
						.FirstOrDefault(e => string.Equals( e.DisplayName?.UserLocalizedLabel?.Label, command.TableName, StringComparison.OrdinalIgnoreCase));
				}

				componentId = entity?.MetadataId;
			}
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			if (!componentId.HasValue)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Table '{command.TableName}' not found.");
			}


			output.WriteLine("Done", ConsoleColor.Green);
			output.WriteLine("MetadataId: " + componentId.Value, ConsoleColor.Cyan);


			output.Write($"Adding component to solution {solutionName}...");
			try
			{
				var request = new AddSolutionComponentRequest
				{
					SolutionUniqueName = solutionName,
					ComponentType = (int)ComponentType.Entity,
					ComponentId = componentId.Value,
					AddRequiredComponents = command.AddRequiredComponents,
					DoNotIncludeSubcomponents = !command.IncludeSubcomponents
				};

				await crm.ExecuteAsync(request, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
