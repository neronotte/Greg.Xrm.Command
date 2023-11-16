using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Newtonsoft.Json;
using System.Diagnostics;
using System.ServiceModel;
using System.Text.Json;

namespace Greg.Xrm.Command.Commands.Column
{
	public class ExportMetadataCommandExecutor : ICommandExecutor<ExportMetadataCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public ExportMetadataCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}

        public async Task ExecuteAsync(ExportMetadataCommand command, CancellationToken cancellationToken)
		{

			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			string text;
			try
			{
				var request = new RetrieveAttributeRequest
				{
					EntityLogicalName = command.TableSchemaName,
					LogicalName = command.ColumnSchemaName
				};

				var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(request);

				text = JsonConvert.SerializeObject(response.AttributeMetadata, Formatting.Indented);
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine()
					.Write("Error: ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);

				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
				return;
			}

			var folder = command.OutputFilePath;
			if (string.IsNullOrWhiteSpace(folder))
			{
				folder = Environment.CurrentDirectory;
			}

			if (!Directory.Exists(folder))
			{
				output.WriteLine()
					.Write("Error: ", ConsoleColor.Red)
					.WriteLine($"The folder '{folder}' does not exist", ConsoleColor.Red);
				return;
			}

			var fileName = $"{command.TableSchemaName}.{command.ColumnSchemaName}.json";
			var filePath = Path.Combine(folder, fileName);

			try
			{
				File.WriteAllText(filePath, text);

				if (command.AutoOpenFile)
				{
					Process.Start(new ProcessStartInfo(filePath)
					{
						UseShellExecute = true
					});
				}
			}
			catch(Exception ex)
			{
				output.WriteLine()
					.Write("Error while trying to write on the generated file: ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);
			}

		}
	}
}
