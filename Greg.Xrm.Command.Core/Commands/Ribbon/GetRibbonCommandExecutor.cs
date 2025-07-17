using Autofac.Core;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.Diagnostics;
using System.IO.Packaging;
using System.Text;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	public class GetRibbonCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
	
	: ICommandExecutor<GetRibbonCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetRibbonCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			string text;
			try
			{
				byte[] compressedXml;
				if (string.IsNullOrEmpty(command.EntityName))
				{
					output.Write("Downloading application ribbon...");
					compressedXml = await DownloadApplicationRibbon(crm);
				}
				else
				{
					output.Write($"Downloading ribbon for entity: {command.EntityName}...");
					compressedXml = await DownloadEntityRibbon(crm, command.EntityName);
				}

				var decompressedXml = UnzipRibbonXml(compressedXml);


				text = Encoding.UTF8.GetString(decompressedXml);
				output.WriteLine("Done", ConsoleColor.Green);

				output.WriteLine(text);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			if (!string.IsNullOrWhiteSpace(command.FileName))
			{
				try
				{
					output.Write($"Saving ribbon XML to file: {command.FileName}...");
					await File.WriteAllTextAsync(command.FileName, text);
					output.WriteLine("Done", ConsoleColor.Green);

					if (command.AutoRun)
					{
						Process.Start(new ProcessStartInfo
						{
							FileName = command.FileName,
							UseShellExecute = true,
							CreateNoWindow = true
						});
					}
				}
				catch(Exception ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail(ex.Message, ex);
				}

			}

			return CommandResult.Success();
		}


		public static async Task<byte[]> DownloadApplicationRibbon(IOrganizationServiceAsync2 crm)
		{

			// Create the request to retrieve application ribbon
			var request = new RetrieveApplicationRibbonRequest();

			// Execute the request
			var response = (RetrieveApplicationRibbonResponse)await crm.ExecuteAsync(request);

			// Return the ribbon XML
			return response.CompressedApplicationRibbonXml;
		}

		public async Task<byte[]> DownloadEntityRibbon(IOrganizationServiceAsync2 crm, string entityName)
		{
			// Create the request to retrieve entity ribbon
			var request = new RetrieveEntityRibbonRequest
			{
				EntityName = entityName
			};

			// Execute the request
			var response = (RetrieveEntityRibbonResponse)await crm.ExecuteAsync(request);

			// Return the ribbon XML
			return response.CompressedEntityXml;
		}

		private byte[] UnzipRibbonXml(byte[] data)
		{
			
			var memStream = new MemoryStream();
			memStream.Write(data, 0, data.Length);
			var package = (ZipPackage)ZipPackage.Open(memStream, FileMode.Open);

			var part = (ZipPackagePart)package.GetPart(new Uri("/RibbonXml.xml", UriKind.Relative));
			using (Stream strm = part.GetStream())
			{
				long len = strm.Length;
				byte[] buff = new byte[len];
				strm.Read(buff, 0, (int)len);
				return buff;
			}
		}
	}
}
