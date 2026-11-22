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
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			bool tableUpdated = false;
			bool imageUpdated = false;
			try
			{
				output.Write($"Retrieving metadata for table '{command.SchemaName}'...");
				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
				{
					LogicalName = command.SchemaName,
					EntityFilters = EntityFilters.Entity,
					RetrieveAsIfPublished = true
				}, cancellationToken);
				var table = response.EntityMetadata;
				output.WriteLine("Done", ConsoleColor.Green);

				if ((command.ChangeTrackingEnabled == false && table.ChangeTrackingEnabled == true) ||
					(command.Connections == false && table.IsConnectionsEnabled?.Value == true) ||
					(command.Queue == false && table.IsValidForQueue?.Value == true))
					return CommandResult.Fail("Change tracking, connections and queues cannot be disabled once enabled.");

				if (command.AutoRouteToOwnerQueue == true && !(command.Queue ?? table.IsValidForQueue?.Value ?? false))
					return CommandResult.Fail("Automatic routing requires queues to be enabled for the table.");

				ImageAttributeMetadata? image = null;
				if (command.PrimaryImage != null)
				{
					output.Write($"Retrieving image column '{command.PrimaryImage}'...");
					var attributeResponse = (RetrieveAttributeResponse)await crm.ExecuteAsync(new RetrieveAttributeRequest
					{
						EntityLogicalName = command.SchemaName,
						LogicalName = command.PrimaryImage,
						RetrieveAsIfPublished = true
					}, cancellationToken);
					if (attributeResponse.AttributeMetadata is not ImageAttributeMetadata imageAttribute)
						return CommandResult.Fail($"Column '{command.PrimaryImage}' is not an image column.");
					image = imageAttribute;
					output.WriteLine("Done", ConsoleColor.Green);
				}

				bool mustUpdate = false;
				if (command.DisplayName != null && command.DisplayName != table.DisplayName?.UserLocalizedLabel?.Label)
				{
					if (table.DisplayName?.UserLocalizedLabel is not LocalizedLabel label)
						return CommandResult.Fail("The current table display name has no user-localized label.");
					table.DisplayName = new Label(command.DisplayName, label.LanguageCode);
					mustUpdate = true;
				}
				if (command.DisplayCollectionName != null && command.DisplayCollectionName != table.DisplayCollectionName?.UserLocalizedLabel?.Label)
				{
					if (table.DisplayCollectionName?.UserLocalizedLabel is not LocalizedLabel label)
						return CommandResult.Fail("The current table plural name has no user-localized label.");
					table.DisplayCollectionName = new Label(command.DisplayCollectionName, label.LanguageCode);
					mustUpdate = true;
				}

				Set(command.IsAuditEnabled, table.IsAuditEnabled?.Value, value => table.IsAuditEnabled = new BooleanManagedProperty(value));
				Set(command.ChangeTrackingEnabled, table.ChangeTrackingEnabled, value => table.ChangeTrackingEnabled = value);
				Set(command.IsQuickCreateEnabled, table.IsQuickCreateEnabled, value => table.IsQuickCreateEnabled = value);
				Set(command.HasEmailAddresses, table.HasEmailAddresses, value => table.HasEmailAddresses = value);
				Set(command.DuplicateDetection, table.IsDuplicateDetectionEnabled?.Value, value => table.IsDuplicateDetectionEnabled = new BooleanManagedProperty(value));
				Set(command.CustomHelp, table.EntityHelpUrlEnabled, value => table.EntityHelpUrlEnabled = value);
				Set(command.Retention, table.IsRetentionEnabled, value => table.IsRetentionEnabled = value);
				Set(command.MailMerge, table.IsMailMergeEnabled?.Value, value => table.IsMailMergeEnabled = new BooleanManagedProperty(value));
				Set(command.DocumentManagement, table.IsDocumentManagementEnabled, value => table.IsDocumentManagementEnabled = value);
				Set(command.Connections, table.IsConnectionsEnabled?.Value, value => table.IsConnectionsEnabled = new BooleanManagedProperty(value));
				Set(command.AccessTeams, table.AutoCreateAccessTeams, value => table.AutoCreateAccessTeams = value);
				Set(command.Offline, table.IsAvailableOffline, value => table.IsAvailableOffline = value);
				Set(command.Queue, table.IsValidForQueue?.Value, value => table.IsValidForQueue = new BooleanManagedProperty(value));
				Set(command.AutoRouteToOwnerQueue, table.AutoRouteToOwnerQueue, value => table.AutoRouteToOwnerQueue = value);

				if (command.Color != null && !string.Equals(command.Color, table.EntityColor, StringComparison.OrdinalIgnoreCase))
				{
					table.EntityColor = command.Color;
					mustUpdate = true;
				}
				if (command.HelpUrl != null && command.HelpUrl != table.EntityHelpUrl)
				{
					table.EntityHelpUrl = command.HelpUrl;
					mustUpdate = true;
				}

				bool enableNotes = command.HasNotes == true && table.HasNotes != true;
				bool enableFeedback = command.HasFeedback == true && table.HasFeedback != true;
				bool enableActivities = command.Activities == true && table.HasActivities != true;
				if (mustUpdate || enableNotes || enableFeedback || enableActivities)
				{
					output.Write($"Updating table '{command.SchemaName}'...");
					var update = new UpdateEntityRequest
					{
						Entity = table,
						MergeLabels = true
					};
					if (command.SolutionName != null) update.SolutionUniqueName = command.SolutionName;
					if (enableNotes) update.HasNotes = true;
					if (enableFeedback) update.HasFeedback = true;
					if (enableActivities) update.HasActivities = true;
					await crm.ExecuteAsync(update, cancellationToken);
					tableUpdated = true;
					output.WriteLine("Done", ConsoleColor.Green);
				}

				if (image != null && image.IsPrimaryImage != true)
				{
					output.Write($"Updating primary image column '{command.PrimaryImage}'...");
					image.IsPrimaryImage = true;
					var updateImage = new UpdateAttributeRequest
					{
						EntityName = command.SchemaName,
						Attribute = image,
						MergeLabels = true
					};
					if (command.SolutionName != null) updateImage.SolutionUniqueName = command.SolutionName;
					await crm.ExecuteAsync(updateImage, cancellationToken);
					imageUpdated = true;
					output.WriteLine("Done", ConsoleColor.Green);
				}
				else if (!tableUpdated)
				{
					output.WriteLine("No changes detected. Table is up to date.", ConsoleColor.Yellow);
				}

				if (tableUpdated || imageUpdated)
					output.WriteLine("Publish the customizations to make the changes visible in model-driven apps.", ConsoleColor.Yellow);

				return CommandResult.Success();

				void Set(bool? requested, bool? current, Action<bool> assign)
				{
					if (requested.HasValue && requested != current)
					{
						assign(requested.Value);
						mustUpdate = true;
					}
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				if (tableUpdated)
					output.WriteLine("The table was updated before the operation was cancelled.", ConsoleColor.Yellow);
				throw;
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				var partial = tableUpdated ? " The table was updated, but a subsequent operation failed." : string.Empty;
				return CommandResult.Fail($"An error occurred while updating the table '{command.SchemaName}': {ex.Message}{partial}", ex);
			}
		}
	}
}
