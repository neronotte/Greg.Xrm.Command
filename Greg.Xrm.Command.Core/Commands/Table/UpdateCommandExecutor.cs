using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table
{
    public class UpdateCommandExecutor(
		IOutput output, 
		IOrganizationServiceRepository organizationServiceRepository) 
		: ICommandExecutor<UpdateCommand>
	{
        public async Task<CommandResult> ExecuteAsync(UpdateCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			EntityMetadata table;
			try
			{
				output.Write($"Retrieving metadata for table '{command.SchemaName}'...");
				var request = new RetrieveEntityRequest
                {
                    LogicalName = command.SchemaName,
                    EntityFilters = EntityFilters.All
                };

                var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
				table = response.EntityMetadata;
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"An error occurred while updating the table '{command.SchemaName}': {ex.Message}", ex);
			}

			bool mustUpdate = false;

			if (command.DisplayName != null && command.DisplayName != table.DisplayName?.UserLocalizedLabel?.Label)
			{
				table.DisplayName = new Label(command.DisplayName, table.DisplayName!.UserLocalizedLabel.LanguageCode);
				mustUpdate = true;
			}

			if (command.DisplayCollectionName != null && command.DisplayCollectionName != table.DisplayCollectionName?.UserLocalizedLabel?.Label)
			{
				table.DisplayCollectionName = new Label(command.DisplayCollectionName, table.DisplayCollectionName!.UserLocalizedLabel.LanguageCode);
				mustUpdate = true;
			}


			if (command.HasNotes.HasValue && !table.HasNotes.GetValueOrDefault())
			{
				table.HasNotes = true;
				mustUpdate = true;
			}

			if (command.HasFeedback.HasValue && !table.HasFeedback.GetValueOrDefault())
			{
				table.HasFeedback = true;
				mustUpdate = true;
			}

			if (command.IsAuditEnabled.HasValue && command.IsAuditEnabled != table.IsAuditEnabled?.Value)
			{
				table.IsAuditEnabled = new BooleanManagedProperty(command.IsAuditEnabled.Value);
				mustUpdate = true;
			}

			if (command.ChangeTrackingEnabled.HasValue && command.ChangeTrackingEnabled != table.ChangeTrackingEnabled)
			{
				table.ChangeTrackingEnabled = command.ChangeTrackingEnabled;
				mustUpdate = true;
			}

			if (command.IsQuickCreateEnabled.HasValue && command.IsQuickCreateEnabled != table.IsQuickCreateEnabled)
			{
				table.IsQuickCreateEnabled = command.IsQuickCreateEnabled;
				mustUpdate = true;
			}

			if (command.HasEmailAddresses.HasValue && !table.HasEmailAddresses.GetValueOrDefault())
			{
				table.HasEmailAddresses = command.HasEmailAddresses;
				mustUpdate = true;
			}

			if (!mustUpdate)
			{
				output.WriteLine("No changes detected. Table is up to date.", ConsoleColor.Yellow);
				return CommandResult.Success();
			}


			try
			{
				output.Write($"Updating table '{command.SchemaName}'...");
				var updateRequest = new UpdateEntityRequest
				{
					Entity = table
				};
				await crm.ExecuteAsync(updateRequest);
				output.WriteLine("Done", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"An error occurred while updating the table '{command.SchemaName}': {ex.Message}", ex);
			}
		}
    }
}
