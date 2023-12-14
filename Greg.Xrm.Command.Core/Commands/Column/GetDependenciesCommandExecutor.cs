using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column
{
    public class GetDependenciesCommandExecutor : ICommandExecutor<GetDependenciesCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IDependencyRepository dependencyRepository;

		public GetDependenciesCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			IDependencyRepository dependencyRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.dependencyRepository = dependencyRepository;
		}


		public async Task<CommandResult> ExecuteAsync(GetDependenciesCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				this.output.Write($"Retrieving metadata for column {command.TableName}.{command.ColumnName}...");
				var request = new RetrieveAttributeRequest
				{
					EntityLogicalName = command.TableName,
					LogicalName = command.ColumnName
				};

				var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(request);
				this.output.WriteLine("Done", ConsoleColor.Green);


				var attribute = response.AttributeMetadata;
				if (!attribute.MetadataId.HasValue)
				{
					return CommandResult.Fail("The attribute has no metadata id");
				}


				var dependencies = await this.dependencyRepository.GetDependenciesAsync(crm, ComponentType.Attribute, attribute.MetadataId.GetValueOrDefault(), command.ForDelete);
				if (dependencies.Count == 0)
				{
					this.output.WriteLine("No dependencies found!", ConsoleColor.Cyan);
					return CommandResult.Success();
				}

				dependencies.WriteTo(output);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
