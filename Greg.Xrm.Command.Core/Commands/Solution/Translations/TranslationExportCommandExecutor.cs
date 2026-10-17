using System.IO.Compression;
using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	public class TranslationExportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<TranslationExportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TranslationExportCommand command, CancellationToken cancellationToken)
		{
			var solutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(solutionName))
				solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();

			if (string.IsNullOrWhiteSpace(solutionName))
				return CommandResult.Fail("No solution name was provided and no current default solution is configured.");

			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var outputPath = ResolveTargetPath(command.Path, solutionName);
			var outputDirectory = System.IO.Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrWhiteSpace(outputDirectory))
				Directory.CreateDirectory(outputDirectory);

			try
			{
				output.Write($"Exporting translations for solution '{solutionName}'...");
				var request = new ExportTranslationRequest
				{
					SolutionName = solutionName,
				};

				var response = (ExportTranslationResponse)await crm.ExecuteAsync(request, cancellationToken);
				var zipBytes = response.ExportTranslationFile ?? [];
				if (zipBytes.Length == 0)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail("The solution export returned an empty translation package.");
				}

				await File.WriteAllBytesAsync(outputPath, zipBytes, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["SolutionName"] = solutionName;
				result["Path"] = outputPath;
				result["Unpacked"] = command.Unpack;

				if (command.Unpack)
				{
					var extractPath = UnpackArchive(outputPath);
					output.WriteLine($"Extracted translation package to '{extractPath}'", ConsoleColor.Green);
					result["UnpackedTo"] = extractPath;
				}

				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static string ResolveTargetPath(string? requestedPath, string solutionName)
		{
			if (!string.IsNullOrWhiteSpace(requestedPath))
				return System.IO.Path.GetFullPath(requestedPath);

			return System.IO.Path.Combine(Environment.CurrentDirectory, $"{solutionName}_translations.zip");
		}

		private static string UnpackArchive(string zipPath)
		{
			var unpackRoot = System.IO.Path.ChangeExtension(zipPath, null);
			Directory.CreateDirectory(unpackRoot);

			using var archive = ZipFile.OpenRead(zipPath);
			foreach (var entry in archive.Entries)
			{
				// Directory entries have an empty name and only need the folder to be created.
				var destinationPath = System.IO.Path.Combine(unpackRoot, entry.FullName);
				var destinationDirectory = System.IO.Path.GetDirectoryName(destinationPath);
				if (!string.IsNullOrWhiteSpace(destinationDirectory))
					Directory.CreateDirectory(destinationDirectory);

				if (string.IsNullOrEmpty(entry.Name))
					continue;

				entry.ExtractToFile(destinationPath, overwrite: true);
			}

			return unpackRoot;
		}
	}
}
