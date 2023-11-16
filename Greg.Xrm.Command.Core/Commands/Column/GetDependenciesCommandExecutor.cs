using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.ComponentResolvers;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Newtonsoft.Json;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column
{
	public class GetDependenciesCommandExecutor : ICommandExecutor<GetDependenciesCommand>
	{
		private readonly ILogger<GetDependenciesCommand> log;
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public GetDependenciesCommandExecutor(
			ILogger<GetDependenciesCommand> log,
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.log = log;
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}


		public async Task ExecuteAsync(GetDependenciesCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			var componentResolverFactory = new ComponentResolverFactory(crm, this.log);

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
					this.output.WriteLine("Error: the attribute has no metadata id", ConsoleColor.Red);
					return;
				}


				var request1 = new RetrieveDependentComponentsRequest();
				request1.ObjectId = attribute.MetadataId.Value;
				request1.ComponentType =  (int)ComponentType.Attribute; 

				var response1 = (RetrieveDependentComponentsResponse)await crm.ExecuteAsync(request1);

				var dependencies = response1.EntityCollection.Entities.Select(x =>new Dependency(x))
					.OrderBy(x => x.DependentComponentTypeFormatted)
					.ThenBy(x => x.dependentcomponentobjectid)
					.ToArray();

				if (dependencies.Length == 0)
				{
					this.output.WriteLine("No dependencies found!", ConsoleColor.Cyan);
					return;
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
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine()
					.Write("Error: ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);

				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
			}
		}
	}
}
