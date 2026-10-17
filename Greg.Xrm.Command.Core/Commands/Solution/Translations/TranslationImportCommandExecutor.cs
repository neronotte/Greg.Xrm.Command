using System.IO.Compression;
using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	public class TranslationImportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<TranslationImportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TranslationImportCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.Path))
				return CommandResult.Fail("The path to the translation file or folder is required.");

			var inputPath = System.IO.Path.GetFullPath(command.Path);
			var isFolder = Directory.Exists(inputPath);

			if (isFolder && !command.Repack)
				return CommandResult.Fail($"The path '{inputPath}' is a folder. Re-run the command with --repack to create the zip archive before importing it.");

			if (!isFolder && !File.Exists(inputPath))
				return CommandResult.Fail($"The translation file '{inputPath}' does not exist.");

			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var sourcePath = inputPath;
				if (isFolder)
				{
					output.Write($"Repacking folder '{inputPath}'...");
					sourcePath = RepackFolder(inputPath);
					output.WriteLine("Done", ConsoleColor.Green);
				}

				var zipBytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
				if (zipBytes.Length == 0)
					return CommandResult.Fail($"The translation file '{sourcePath}' is empty.");

				output.Write("Importing translation package...");
				var request = new ImportTranslationRequest
				{
					ImportJobId = Guid.NewGuid(),
					TranslationFile = zipBytes,
				};

				await crm.ExecuteAsync(request, cancellationToken);
				output.WriteLine("Started", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["Path"] = sourcePath;
				result["Repacked"] = isFolder;
				result["ImportJobId"] = request.ImportJobId;
				result["WaitedForCompletion"] = !command.NoWait;

				if (command.NoWait)
				{
					output.WriteLine($"Import job {request.ImportJobId} is running in Dataverse.");
					return result;
				}

				var progress = await WaitForImportJobAsync(crm, request.ImportJobId, cancellationToken);
				result["Progress"] = progress;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
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

		private async Task<double> WaitForImportJobAsync(
			IOrganizationServiceAsync2 crm,
			Guid importJobId,
			CancellationToken cancellationToken)
		{
			output.WriteLine($"Waiting for import job {importJobId} to complete...");

			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var importJob = await crm.RetrieveAsync(
					"importjob",
					importJobId,
					new ColumnSet("progress", "completedon"),
					cancellationToken);

				var progress = importJob.GetAttributeValue<double>("progress");
				var completedOn = importJob.GetAttributeValue<DateTime?>("completedon");
				output.WriteLine($"Import progress: {progress:0.##}%");

				if (completedOn.HasValue || progress >= 100d)
				{
					output.WriteLine("Translation import completed", ConsoleColor.Green);
					return progress;
				}

				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
			}
		}

		private static string RepackFolder(string folderPath)
		{
			var zipPath = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(folderPath) ?? Environment.CurrentDirectory,
				System.IO.Path.GetFileName(folderPath) + ".zip");

			if (File.Exists(zipPath))
				File.Delete(zipPath);

			ZipFile.CreateFromDirectory(folderPath, zipPath);
			return zipPath;
		}
	}
}
