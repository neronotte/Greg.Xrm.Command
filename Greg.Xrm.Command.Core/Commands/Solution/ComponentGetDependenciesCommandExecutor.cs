using Greg.Xrm.Command.Commands.Column;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.ComponentResolvers;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentGetDependenciesCommandExecutor : ICommandExecutor<ComponentGetDependenciesCommand>
	{
		private readonly ILogger<GetDependenciesCommand> log;
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public ComponentGetDependenciesCommandExecutor(
			ILogger<GetDependenciesCommand> log,
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.log = log;
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}


		public async Task<CommandResult> ExecuteAsync(ComponentGetDependenciesCommand command, CancellationToken cancellationToken)
		{
			if (command.ComponentId == Guid.Empty)
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "The component id is required");
			}
			if (command.ComponentType == null)
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, "The component type is required");
			}


			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			var componentResolverFactory = new ComponentResolverFactory(crm, this.log);

			try
			{
				var request = new RetrieveDependentComponentsRequest
				{
					ObjectId = command.ComponentId,
					ComponentType = (int)command.ComponentType
				};

				var response = (RetrieveDependentComponentsResponse)await crm.ExecuteAsync(request);

				var dependencies = response.EntityCollection.Entities.Select(x => new Dependency(x))
					.OrderBy(x => x.DependentComponentTypeFormatted)
					.ThenBy(x => x.dependentcomponentobjectid)
					.ToArray();

				if (dependencies.Length == 0)
				{
					this.output.WriteLine("No dependencies found!", ConsoleColor.Cyan);
					return CommandResult.Success();
				}

				var dependencyGroups = dependencies.GroupBy(x => x.dependentcomponenttype.Value).ToArray();

				foreach (var dependencyGroup in dependencyGroups)
				{
					var componentType = dependencyGroup.Key;
					var componentTypeName = dependencyGroup.First().DependentComponentTypeFormatted;
					var resolver = componentResolverFactory.GetComponentResolverFor(componentType);


					this.output.WriteLine()
						.Write(componentTypeName, ConsoleColor.Cyan)
						.WriteLine();

					if (resolver == null)
					{
						foreach (var dependency in dependencyGroup)
						{
							output
								.Write(dependency.dependentcomponentobjectid, ConsoleColor.Yellow)
								.Write(" (", ConsoleColor.DarkGray)
								.Write(dependency.DependencyTypeFormatted, ConsoleColor.DarkGray)
								.Write(")", ConsoleColor.DarkGray);
							output.WriteLine();
						}
					}
					else
					{
						var componentIds = dependencyGroup.Select(x => x.dependentcomponentobjectid).ToArray();
						var componentNames = await resolver.GetNamesAsync(componentIds);

						var values = componentNames.OrderBy(x => x.Value).ToArray();
						foreach (var value in values)
						{
							var dependencyType = dependencyGroup.FirstOrDefault(x => x.dependentcomponentobjectid == x.Id)?.DependencyTypeFormatted;

							output
								.Write(value.Key, ConsoleColor.Yellow)
								.Write(" | ")
								.Write(value.Value);

							if (!string.IsNullOrWhiteSpace(dependencyType))
							{
								output
									.Write(" (", ConsoleColor.DarkGray)
									.Write(dependencyType, ConsoleColor.DarkGray)
									.Write(")", ConsoleColor.DarkGray);
							}
							output.WriteLine();
						}
					}
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
