using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column
{
    public class DeleteCommandExecutor : ICommandExecutor<DeleteCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IDependencyRepository dependencyRepository;
		private readonly IAttributeDeletionService attributeDeletionService;

		public DeleteCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			IDependencyRepository dependencyRepository,
			IAttributeDeletionService attributeDeletionService)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.dependencyRepository = dependencyRepository;
			this.attributeDeletionService = attributeDeletionService;
		}


		public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			var columnMetadata = await RetrieveColumnMetadataAsync(crm, command);
			if (columnMetadata == null)
				return CommandResult.Fail($"Column {command.TableName}.{command.SchemaName} not found.");

			if (columnMetadata.IsManaged.GetValueOrDefault())
				return CommandResult.Fail($"Column {command.TableName}.{command.SchemaName} is managed and cannot be deleted.");

			if (columnMetadata.IsPrimaryId.GetValueOrDefault())
				return CommandResult.Fail($"Column {command.TableName}.{command.SchemaName} is the primary id and cannot be deleted.");

			if (columnMetadata.IsPrimaryName.GetValueOrDefault())
				return CommandResult.Fail($"Column {command.TableName}.{command.SchemaName} is the primary column and cannot be deleted.");

			var dependencies = await RetrieveDependenciesAsync(crm, command, columnMetadata);
			if (dependencies == null)
				return CommandResult.Fail($"Error while retrieving dependencies for column {command.TableName}.{command.SchemaName}.");

			try
			{
				this.output.WriteLine();

				if (dependencies.Count == 0)
				{
					this.output.WriteLine("No dependency found.");
				}
				else if (dependencies.Count > 0)
				{
					this.output.WriteLine($"Column {command.TableName}.{command.SchemaName} has {dependencies.Count} dependencies.", ConsoleColor.Yellow);

					dependencies.WriteTo(this.output);


					if (!command.Force)
					{
						this.output.WriteLine($"Use the --force option to delete it anyway.", ConsoleColor.Yellow);
						return CommandResult.Fail($"Column {command.TableName}.{command.SchemaName} has {dependencies.Count} dependencies.");
					}
					else
					{
						// Force delete
						await this.attributeDeletionService.DeleteAttributeAsync(crm, columnMetadata, dependencies);

						// now re-check the dependencies to see if everything has been deleted
						dependencies = await RetrieveDependenciesAsync(crm, command, columnMetadata);
						if (dependencies is not null && dependencies.Count > 0)
						{
							this.output.WriteLine($"After dependency pruning, column {command.TableName}.{command.SchemaName} has still {dependencies.Count} dependencies.", ConsoleColor.Yellow);

							dependencies.WriteTo(this.output);

							this.output.WriteLine($"These dependencies cannot be removed via pacx, you need to remove it manually to delete the column.", ConsoleColor.Yellow);
							return CommandResult.Fail($"Column {command.TableName}.{command.SchemaName} has {dependencies.Count} dependencies.");
						}
						else
						{
							this.output.WriteLine($"After dependency pruning, column {command.TableName}.{command.SchemaName} has no more dependencies, and can be deleted.");
						}
					}
				}


				this.output.Write("Deleting column ")
					.Write(command.SchemaName, ConsoleColor.Yellow)
					.Write(".", ConsoleColor.Yellow)
					.Write(command.SchemaName, ConsoleColor.Yellow)
					.Write("...");


				var request = new DeleteAttributeRequest
				{
					EntityLogicalName = command.TableName,
					LogicalName = command.SchemaName
				};
				await crm.ExecuteAsync(request, cancellationToken);

				this.output.WriteLine(" Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private async Task<DependencyList?> RetrieveDependenciesAsync(IOrganizationServiceAsync2 crm, DeleteCommand command, AttributeMetadata columnMetadata)
		{
			try
			{
				this.output.Write("Retrieving dependencies for column ")
					.Write(command.SchemaName, ConsoleColor.Yellow)
					.Write(".", ConsoleColor.Yellow)
					.Write(command.SchemaName, ConsoleColor.Yellow)
					.Write("...");


				var dependencyList = await this.dependencyRepository.GetDependenciesAsync(crm, ComponentType.Attribute, columnMetadata.MetadataId.GetValueOrDefault(), true);

				this.output.WriteLine(" Done", ConsoleColor.Green);

				return dependencyList;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine(" Failed: " + ex.Message, ConsoleColor.Red);
				return null;
			}
		}

		private async Task<AttributeMetadata?> RetrieveColumnMetadataAsync(IOrganizationServiceAsync2 crm, DeleteCommand command)
		{
			try
			{
				this.output.Write("Retrieving metadata for column ")
					.Write(command.SchemaName, ConsoleColor.Yellow)
					.Write(".", ConsoleColor.Yellow)
					.Write(command.SchemaName, ConsoleColor.Yellow)
					.Write("...");


				var request = new RetrieveAttributeRequest
				{
					EntityLogicalName = command.TableName,
					LogicalName = command.SchemaName,
					RetrieveAsIfPublished = false,
				};

				var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(request);

				this.output.WriteLine(" Done", ConsoleColor.Green);

				return response.AttributeMetadata;
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine(" Failed: " + ex.Message, ConsoleColor.Red);
				return null;
			}
		}
	}
}
