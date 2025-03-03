using Greg.Xrm.Command.Commands.WebResources.ProjectFile;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class PushCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			IFolderResolver folderResolver,
			IWebResourceFilesResolver webResourceFilesResolver,
			IWebResourceRepository webResourceRepository,
			IPublishXmlBuilder publishXmlBuilder,
			IWebResourceProjectFileRepository webResourceProjectFileRepository) 
			: ICommandExecutor<PushCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PushCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);



			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
				}
			}




			output.Write("Retreving current solution publisher...");
			var solution = await solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
			output.WriteLine("Done", ConsoleColor.Green);
			if (solution == null)
			{
				return CommandResult.Fail($"Solution '{currentSolutionName}' not found.");
			}



			var files = new List<WebResourceMap> ();
			try
			{
				var folders = folderResolver.ResolveFrom(command.Folder, solution.PublisherCustomizationPrefix ?? string.Empty);


				if (command.Reference != null)
				{
					var rootDirectory = new DirectoryInfo(folders.ProjectRootPath);
					var (result, projectFile) = await webResourceProjectFileRepository.TryReadAsync(rootDirectory);
					if (!result)
					{
						return CommandResult.Fail($"Error while reading the project file: {projectFile}");
					}

					if (command.IsReferenceAll || command.IsReferenceOnly)
					{
						files.AddRange(projectFile.ExternalReferences);
					}
					else
					{
						var references = command.Reference.Split([','], StringSplitOptions.RemoveEmptyEntries);
						foreach (var reference in references)
						{
							var map = projectFile.ExternalReferences.FirstOrDefault(m => string.Equals(m.Target, reference, StringComparison.OrdinalIgnoreCase));
							if (map == null)
							{
								return CommandResult.Fail($"WebResource '{reference}' not found in the project file.");
							}
							files.Add(map);
						}
					}

					var missingFiles = string.Join(", ", files.Where(f => !File.Exists(f.Source)));
					if (!string.IsNullOrWhiteSpace(missingFiles))
					{
						return CommandResult.Fail($"File(s) '{missingFiles}' not found.");
					}
				}


				if (command.Reference == null || command.IsReferenceAll)
				{
					output.Write("Identifying the list of files to push...");
					var temp = webResourceFilesResolver.ResolveFiles(folders).Select(x => new WebResourceMap(x.LocalPath, x.RemotePath, x.Type));
					files.AddRange(temp);
					output.WriteLine("Done", ConsoleColor.Green);
				}
			}
			catch (Exception ex)
			{
				output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while identifying the list of files to push: {ex.Message}");
			}

			if (files.Count == 0)
			{
				output.Write($"Found ").Write(files.Count, ConsoleColor.Yellow).WriteLine(" webresources to push. Nothing to do here.");
				return CommandResult.Success();
			}

			if (command.Verbose || command.NoAction)
			{
				output.WriteLine();
				output.WriteTable(files, () => ["Type", "Name"], f => [f.Type.ToString(), f.Target], (i, row) => ConsoleColor.DarkGray);
			}
			else
			{
				output.Write($"Found ").Write(files.Count, ConsoleColor.Green).WriteLine(" webresources to push.");
			}


			output.Write("Fetching webresources from dataverse...");
			List<WebResource> currentWebResources;
			try
			{
				var fileNames = files.Select(f => f.Target).ToArray();
				currentWebResources = await webResourceRepository.GetByNameAsync(crm, fileNames, true);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while fetching webresources from dataverse: {ex.Message}");
			}




			await UpdateRemoteFromLocalAsync(crm, files, currentWebResources, command.NoAction);
			var publishXmlRequest = publishXmlBuilder.Build();
			if (publishXmlRequest == null)
			{
				output.WriteLine("Everything is up to date.", ConsoleColor.Green);
				return CommandResult.Success();
			}


			if (!command.NoPublish)
			{
				var result = await PublishUpdatedWebResourcesAsync(command, crm, publishXmlRequest);
				if (!result.IsSuccess) return result;
			}



			var result1 = await AddWebResourcesToSolutionAsync(crm, solution, currentWebResources, command.Verbose);
			if (!result1.IsSuccess) return result1;


			return CommandResult.Success();
		}



		/// <summary>
		/// Takes as input the list of files to push, the list of webresources already present in the environment.
		/// Compares the content of the files with the content of the webresources and updates the webresources if needed.
		/// If the noAction flag is set to true, no changes are made to the environment: it just simulates the action.
		/// Returns a tuple with a boolean indicating if the webresources have been updated and the xml to publish the changes.
		/// </summary>
		/// <param name="crm">The organization service instance</param>
		/// <param name="files">The local files to push</param>
		/// <param name="currentWebResources">The web resources that are currently in the target environment</param>
		/// <param name="noAction">A flag indicating whether to perform any action on the environment</param>
		private async Task UpdateRemoteFromLocalAsync(
			IOrganizationServiceAsync2 crm,
			IReadOnlyList<WebResourceMap> files,
			List<WebResource> currentWebResources,
			bool noAction)
		{
			output.WriteLine("Updating webresources...");
			foreach (var file in files)
			{
				output.Write("   - " + file.Target + "...");
				try
				{
					var fileContent = await File.ReadAllBytesAsync(file.Source);

					var webResource = currentWebResources.Find(w => string.Equals(w.name, file.Target, StringComparison.OrdinalIgnoreCase));
					if (webResource == null)
					{
						output.Write("to be created...");

						webResource = new WebResource
						{
							name = file.Target,
							displayname = file.Target,
							webresourcetype = new OptionSetValue((int)file.Type),
							content = Convert.ToBase64String(fileContent)
						};

						if (!noAction)
						{
							await webResource.SaveOrUpdateAsync(crm);
							publishXmlBuilder.AddWebResource(webResource.Id);
						}

						output.WriteLine("Done", ConsoleColor.Green);
						currentWebResources.Add(webResource);
					}
					else
					{
						webResource.content = Convert.ToBase64String(fileContent);
						if (webResource.IsDirty)
						{
							output.Write("to be updated...");
							if (!noAction)
							{
								await webResource.SaveOrUpdateAsync(crm);
								publishXmlBuilder.AddWebResource(webResource.Id);
							}
							output.WriteLine("Done", ConsoleColor.Green);
						}
						else
						{
							output.WriteLine("...no changes", ConsoleColor.DarkGray);
						}
					}
				}
				catch (Exception ex)
				{
					output.WriteLine("ERROR: " + ex.Message, ConsoleColor.Red);
				}
			}
		}






		private async Task<CommandResult> PublishUpdatedWebResourcesAsync(PushCommand command, IOrganizationServiceAsync2 crm, PublishXmlRequest request)
		{
			output.Write("Publishing updated webresources...");
			try
			{
				if (!command.NoAction)
				{
					await crm.ExecuteAsync(request);
				}

				output.WriteLine("Done", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while publishing webresources: {ex.Message}");
			}
		}





		private async Task<CommandResult> AddWebResourcesToSolutionAsync(IOrganizationServiceAsync2 crm, Model.Solution solution, List<WebResource> currentWebResources, bool verbose)
		{
			if (currentWebResources.Count == 0)
				return CommandResult.Success();

			try
			{
				output.Write("Adding webresources to solution...");
				var result = await solution.UpsertSolutionComponentsAsync(crm, currentWebResources, ComponentType.WebResource);
				output.WriteLine("Done", ConsoleColor.Green);

				if (result.ComponentsWithErrors.Count == 0 && result.ComponentsAlreadyThere.Count == 0)
				{
					output.Write("All ").Write(currentWebResources.Count, ConsoleColor.Green).WriteLine(" webresources have been added to the solution.");
					return CommandResult.Success();
				}
				if (result.ComponentsAlreadyThere.Count == currentWebResources.Count)
				{
					output.Write("Nothing to do, all ").Write(result.ComponentsAlreadyThere.Count, ConsoleColor.Cyan).Write(" webresources are already in the solution.");
					return CommandResult.Success();
				}

				if (result.ComponentsAdded.Count > 0 && result.ComponentsAlreadyThere.Count > 0 && result.ComponentsWithErrors.Count == 0) 
				{
					output
						.Write(result.ComponentsAdded.Count, ConsoleColor.Green).Write(" webresources added, ")
						.Write(result.ComponentsAlreadyThere.Count, ConsoleColor.Cyan).Write(" webresources were already there.")
						.WriteLine();

					return CommandResult.Success();
				}


				output
					.Write(result.ComponentsAdded.Count, ConsoleColor.Green).Write(" webresources added, ")
					.Write(result.ComponentsWithErrors.Count, ConsoleColor.Red).Write(" got an error, ")
					.Write(result.ComponentsAlreadyThere.Count, ConsoleColor.Cyan).Write(" webresources were already there.");



				foreach (var childResponse in result.ComponentsWithErrors)
				{
					var component = (WebResource)childResponse.Component;

					if (verbose)
					{
						output.WriteLine(JsonConvert.SerializeObject(childResponse, Formatting.Indented), ConsoleColor.DarkGray);
						output.WriteLine();
					}
					
					output.WriteLine($"ERROR while adding '{component.name}' to '{solution.uniquename}': {childResponse.Fault.Message}", ConsoleColor.Red);
				}


				return CommandResult.Success();

			}
			catch (Exception ex)
			{
				output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while adding components to solution: {ex.Message}");
			}
		}

	}
}
