using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.WebResources
{
    public class InitCommandExecutor : ICommandExecutor<InitCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISolutionRepository solutionRepository;
		private readonly IWebResourceRepository webResourceRepository;

		public InitCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			IWebResourceRepository webResourceRepository)
		{
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
			this.solutionRepository = solutionRepository;
			this.webResourceRepository = webResourceRepository ?? throw new ArgumentNullException(nameof(webResourceRepository));
		}


		public async Task<CommandResult> ExecuteAsync(InitCommand command, CancellationToken cancellationToken)
		{
			if (command.FromSolution)
			{
				return await InitRemoteAsync(command, cancellationToken);
			}

			return await InitLocalAsync(command);
		}



		private async Task<CommandResult> InitLocalAsync(InitCommand command)
		{
			if (!TryCheckFolder(command.Folder, out var folder, out var errorMessage))
			{
				return CommandResult.Fail(errorMessage);
			}



			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
				}
			}

			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			var (success, publisherPrefix) = await TryCheckSolution(crm, currentSolutionName);
			if (!success)
			{
				// in this case, publisherPrefix contains the error message
				return CommandResult.Fail(publisherPrefix);
			}

			try
			{

				this.output.Write("Creating the WebResources project file...");
				var projectFilePath = Path.Combine(folder.FullName, ".wr.pacx");
				File.Create(projectFilePath).Dispose();
				this.output.WriteLine("Done", ConsoleColor.Green);



				this.output.Write($"Creating subfolder <{publisherPrefix}_>...");
				folder = folder.CreateSubdirectory(publisherPrefix + "_");
				this.output.WriteLine("Done", ConsoleColor.Green);




				var directoryNames = new string[] { "images", "scripts", "pages" };
				foreach (var directoryName in directoryNames)
				{
					this.output.Write($"Creating subfolder <{directoryName}>...");
					folder.CreateSubdirectory(directoryName);
					this.output.WriteLine("Done", ConsoleColor.Green);
				}

				this.output.WriteLine($"Web resources project initialized in <{folder.FullName}>");

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);

				return CommandResult.Fail(ex.Message);
			}
		}





		private async Task<CommandResult> InitRemoteAsync(InitCommand command, CancellationToken cancellationToken)
		{
			if (!TryCheckFolder(command.Folder, out var folder, out var errorMessage))
			{
				return CommandResult.Fail(errorMessage);
			}

			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
				}
			}

			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			var (success, publisherPrefix) = await TryCheckSolution(crm, currentSolutionName);
			if (!success)
			{
				// in this case, publisherPrefix contains the error message
				return CommandResult.Fail(publisherPrefix);
			}

			var webResourceList = await this.webResourceRepository.GetBySolutionAsync(crm, currentSolutionName, true);

			int errorCount = 0;
			foreach (var webResource in webResourceList)
			{
				this.output.Write($"Downloading <{webResource.name}>...");
				try
				{
					var content = await webResource.GetContentAsync(crm);

					var webResourceFullPath = webResource.name.Split('/');
					var webResourceFileName = webResourceFullPath[webResourceFullPath.Length - 1];

					var webResourcePath = webResourceFullPath.Length > 1 ? webResourceFullPath.Take(webResourceFullPath.Length - 1).ToArray() : Array.Empty<string>();

					var webResourceFolder = FolderTree.CreateFolderTree(folder, webResourcePath);

					var file = webResourceFolder.GetFiles(webResourceFileName).FirstOrDefault();
					if (file == null)
					{
						file = new FileInfo(Path.Combine(webResourceFolder.FullName, webResourceFileName));
					}

					using var stream = file.OpenWrite();
					var bytes = Convert.FromBase64String(content);
					await stream.WriteAsync(bytes, cancellationToken);
					await stream.FlushAsync(cancellationToken);
					stream.Close();
					this.output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (Exception ex)
				{
					this.output.WriteLine("ERROR", ConsoleColor.Red);
					this.output.WriteLine($"Error downloading <{webResource.name}>: {ex.Message}", ConsoleColor.Red);
					errorCount++;
				}
			}

			if (errorCount == webResourceList.Count)
			{
				return CommandResult.Fail("No web resources were downloaded.");
			}


			this.output.Write("Creating the WebResources project file...");
			var projectFilePath = Path.Combine(folder.FullName, ".wr.pacx");
			File.Create(projectFilePath).Dispose();
			this.output.WriteLine("Done", ConsoleColor.Green);

			return CommandResult.Success();
		}







		private static bool TryCheckFolder(string? folderName, out DirectoryInfo folder, out string errorMessage)
		{
			errorMessage = string.Empty;
			if (string.IsNullOrWhiteSpace(folderName))
			{
				folder = new DirectoryInfo(Environment.CurrentDirectory);
			}
			else if (Path.IsPathRooted(folderName))
			{
				folder = new DirectoryInfo(folderName);
			}
			else
			{
				folder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, folderName));
			}

			if (!folder.Exists)
			{
				errorMessage = $"Directory <{folder.FullName}> does not exists!";
				return false;
			}

			if (folder.GetFiles().Length > 0 || folder.GetDirectories().Length > 0)
			{
				errorMessage = $"Directory <{folder.FullName}> is not empty!";
				return false;
			}
			return true;
		}




		private async Task<(bool, string)> TryCheckSolution(IOrganizationServiceAsync2 crm, string currentSolutionName)
		{
			output.WriteLine("Checking solution existence and retrieving publisher");

			var solution = await this.solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
			if (solution == null)
			{
				return (false, "Invalid solution name: " + currentSolutionName);
			}

			if (solution.ismanaged)
			{
				return (false, "The provided solution is managed. You must specify an unmanaged solution.");
			}

			var publisherPrefix = solution.PublisherCustomizationPrefix;
			if (string.IsNullOrWhiteSpace(publisherPrefix))
			{
				return (false, "Unable to retrieve the publisher. Please report a bug to the project GitHub page.");
			}

			return (true, publisherPrefix);
		}
	}
}
