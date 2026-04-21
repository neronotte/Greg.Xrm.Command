using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Catalog
{
	public class CatalogPublishCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<CatalogPublishCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CatalogPublishCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync(cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would publish:", ConsoleColor.Yellow);
				output.WriteLine($"  Name: {command.Name}");
				output.WriteLine($"  Type: {command.Type}");
				output.WriteLine($"  Version: {command.Version}");
				output.WriteLine($"  Description: {command.Description ?? "(none)"}");
				return CommandResult.Success();
			}

			try
			{
				var item = new Entity("catalogitem");
				item["uniquename"] = command.Name;
				item["displayname"] = command.Name;
				item["description"] = command.Description ?? "";
				item["version"] = command.Version ?? "1.0.0";
				
				// Set to Published state (1), not Draft (0)
				item["statecode"] = new OptionSetValue(1);

				if (command.Type == "BusinessEvent")
				{
					item["catalogitemtype"] = new OptionSetValue(1);
				}
				else if (command.Type == "ApiDefinition")
				{
					item["catalogitemtype"] = new OptionSetValue(2);
				}

				output.Write($"Creating catalog item '{command.Name}'...");
				var itemId = await crm.CreateAsync(item, cancellationToken);
				output.WriteLine(" Done", ConsoleColor.Green);

				output.Write($"Publishing catalog item '{command.Name}'...");
				var request = new OrganizationRequest("PublishCatalogItem")
				{
					["CatalogItemId"] = itemId
				};
				await crm.ExecuteAsync(request, cancellationToken);
				output.WriteLine(" Done", ConsoleColor.Green);
				output.WriteLine($"Catalog item published with ID: {itemId}", ConsoleColor.Green);
				output.WriteLine($"Type: {command.Type}", ConsoleColor.Cyan);
				output.WriteLine($"State: Published", ConsoleColor.Cyan);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Failed to publish catalog item: {ex.Message}", ex);
			}
		}
	}
}
