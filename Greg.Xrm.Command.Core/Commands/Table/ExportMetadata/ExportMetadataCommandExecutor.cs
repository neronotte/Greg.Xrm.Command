using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Diagnostics;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
	public class ExportMetadataCommandExecutor : ICommandExecutor<ExportMetadataCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IExportMetadataStrategyFactory exportMetadataStrategyFactory;

		public ExportMetadataCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			IExportMetadataStrategyFactory exportMetadataStrategyFactory)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.exportMetadataStrategyFactory = exportMetadataStrategyFactory;
		}

		public async Task<CommandResult> ExecuteAsync(ExportMetadataCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);



			EntityMetadata entityMetadata;
			try
			{
				var request = new RetrieveEntityRequest
				{
					EntityFilters = EntityFilters.All,
					LogicalName = command.TableSchemaName,
				};

				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
				entityMetadata = response.EntityMetadata;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}

			var folder = command.OutputFilePath;
			if (string.IsNullOrWhiteSpace(folder))
			{
				folder = Environment.CurrentDirectory;
			}


			if (!Directory.Exists(folder))
			{
				return CommandResult.Fail($"The folder '{folder}' does not exist");
			}

			var strategy = exportMetadataStrategyFactory.Create(command.Format);

			output.Write($"Exporting metadata for table '{entityMetadata.SchemaName}'...");

			var filePath = await strategy.ExportAsync(entityMetadata, folder);
			if (filePath == null) return CommandResult.Fail("Unable to export metadata");

			output.WriteLine("Done", ConsoleColor.Green);



			if (command.AutoOpenFile)
			{
				output.Write($"Opening file {filePath}...");
				Process.Start(new ProcessStartInfo(filePath)
				{
					UseShellExecute = true
				});
				output.WriteLine("Done", ConsoleColor.Green);
			}


			var result = CommandResult.Success();
			result["Generated File"] = filePath;
			return result;
		}
	}
}
