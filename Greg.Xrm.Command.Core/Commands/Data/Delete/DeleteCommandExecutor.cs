using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.Delete
{
	public class DeleteCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
	) : ICommandExecutor<DeleteCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			// 1. Connect
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			// 2. Retrieve entity metadata
			output.Write($"Retrieving metadata for table '{command.Table}'...");
			EntityMetadata entityMetadata;
			try
			{
				var request = new RetrieveEntityRequest
				{
					LogicalName = command.Table!,
					EntityFilters = EntityFilters.Entity
				};
				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request, cancellationToken);
				entityMetadata = response.EntityMetadata;
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Table '{command.Table}' not found or inaccessible: {ex.Message}", ex);
			}

			// 3. Retrieve the record (always, to confirm it exists; used for dry-run display too)
			var primaryNameAttribute = entityMetadata.PrimaryNameAttribute;
			output.Write($"Retrieving record {command.Id}...");
			Entity record;
			try
			{
				var columns = string.IsNullOrWhiteSpace(primaryNameAttribute)
					? new ColumnSet(false)
					: new ColumnSet(primaryNameAttribute);

				record = await crm.RetrieveAsync(command.Table!, command.Id, columns, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Record {command.Id} not found or inaccessible: {ex.Message}", ex);
			}

			// 4. Dry-run
			if (command.DryRun)
			{
				output.WriteLine();
				output.WriteLine("Dry-run mode: the following record would be deleted:", ConsoleColor.Cyan);
				output.Write("  Table : ").WriteLine(command.Table, ConsoleColor.Cyan);
				output.Write("  Id    : ").WriteLine(command.Id.ToString(), ConsoleColor.Cyan);

				if (!string.IsNullOrWhiteSpace(primaryNameAttribute) && record.Contains(primaryNameAttribute))
				{
					output.Write("  Name  : ").WriteLine(record[primaryNameAttribute]?.ToString(), ConsoleColor.Cyan);
				}

				return CommandResult.Success();
			}

			// 5. Delete the record
			output.Write($"Deleting record {command.Id}...");
			try
			{
				await crm.DeleteAsync(command.Table!, command.Id, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Error deleting record: {ex.Message}", ex);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Unexpected error deleting record: {ex.Message}", ex);
			}

			// 6. Output result
			output.WriteLine();
			output.Write("Record deleted successfully.  Table: ").WriteLine(command.Table, ConsoleColor.Cyan);

			var result = CommandResult.Success();
			result["Id"] = command.Id;
			return result;
		}
	}
}
