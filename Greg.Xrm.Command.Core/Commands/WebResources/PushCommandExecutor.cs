using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.Text;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class PushCommandExecutor : ICommandExecutor<PushCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISolutionRepository solutionRepository;
		private readonly IFolderResolver folderResolver;
		private readonly IWebResourceFilesResolver webResourceFilesResolver;
		private readonly IWebResourceRepository webResourceRepository;
		private readonly IPublishXmlBuilder publishXmlBuilder;

		public PushCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			IFolderResolver folderResolver,
			IWebResourceFilesResolver webResourceFilesResolver,
			IWebResourceRepository webResourceRepository,
			IPublishXmlBuilder publishXmlBuilder)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.solutionRepository = solutionRepository;
			this.folderResolver = folderResolver;
			this.webResourceFilesResolver = webResourceFilesResolver;
			this.webResourceRepository = webResourceRepository;
			this.publishXmlBuilder = publishXmlBuilder;
		}


		public async Task<CommandResult> ExecuteAsync(PushCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);



			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
				}
			}




			this.output.Write("Retreving current solution publisher...");
			var solution = await solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
			this.output.WriteLine("Done", ConsoleColor.Green);
			if (solution == null)
			{
				return CommandResult.Fail($"Solution '{currentSolutionName}' not found.");
			}





			this.output.Write("Identifying the list of files to push...");
			IReadOnlyList<WebResourceFile> files;
			try
			{
				var folders = folderResolver.ResolveFrom(command.Folder, solution.PublisherCustomizationPrefix ?? string.Empty);
				files = webResourceFilesResolver.ResolveFiles(folders);
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while identifying the list of files to push: {ex.Message}");
			}

			if (files.Count == 0)
			{
				this.output.Write($"Found ").Write(files.Count, ConsoleColor.Yellow).WriteLine(" webresources to push. Nothing to do here.");
				return CommandResult.Success();
			}

			if (command.Verbose || command.NoAction)
			{
				this.output.WriteLine();
				this.output.WriteTable(files, () => new[] { "Type", "Name" }, f => new[] { f.Type.ToString(), f.RemotePath }, (i, row) => ConsoleColor.DarkGray);
			}
			else
			{
				this.output.Write($"Found ").Write(files.Count, ConsoleColor.Green).WriteLine(" webresources to push.");
			}


			this.output.Write("Fetching webresources from dataverse...");
			List<WebResource> currentWebResources;
			try
			{
				var fileNames = files.Select(f => f.RemotePath).ToArray();
				currentWebResources = await this.webResourceRepository.GetByNameAsync(crm, fileNames, true);
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while fetching webresources from dataverse: {ex.Message}");
			}




			await UpdateRemoteFromLocalAsync(crm, files, currentWebResources, command.NoAction);
			var publishXmlRequest = this.publishXmlBuilder.Build();
			if (publishXmlRequest == null)
			{
				this.output.WriteLine("Everything is up to date.", ConsoleColor.Green);
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
			IReadOnlyList<WebResourceFile> files,
			List<WebResource> currentWebResources,
			bool noAction)
		{
			this.output.WriteLine("Updating webresources...");
			foreach (var file in files)
			{
				this.output.Write("   - " + file.RemotePath + "...");
				try
				{
					var fileContent = await File.ReadAllBytesAsync(file.LocalPath);

					var webResource = currentWebResources.Find(w => string.Equals(w.name, file.RemotePath, StringComparison.OrdinalIgnoreCase));
					if (webResource == null)
					{
						this.output.Write("to be created...");

						webResource = new WebResource
						{
							name = file.RemotePath,
							displayname = file.RemotePath,
							webresourcetype = new OptionSetValue((int)file.Type),
							content = Convert.ToBase64String(fileContent)
						};

						if (!noAction)
						{
							await webResource.SaveOrUpdateAsync(crm);
							this.publishXmlBuilder.AddWebResource(webResource.Id);
						}

						this.output.WriteLine("Done", ConsoleColor.Green);
						currentWebResources.Add(webResource);
					}
					else
					{
						webResource.content = Convert.ToBase64String(fileContent);
						if (webResource.IsDirty)
						{
							this.output.Write("to be updated...");
							if (!noAction)
							{
								await webResource.SaveOrUpdateAsync(crm);
								this.publishXmlBuilder.AddWebResource(webResource.Id);
							}
							this.output.WriteLine("Done", ConsoleColor.Green);
						}
						else
						{
							this.output.WriteLine("...no changes", ConsoleColor.DarkGray);
						}
					}
				}
				catch (Exception ex)
				{
					this.output.WriteLine("ERROR: " + ex.Message, ConsoleColor.Red);
				}
			}
		}






		private async Task<CommandResult> PublishUpdatedWebResourcesAsync(PushCommand command, IOrganizationServiceAsync2 crm, PublishXmlRequest request)
		{
			this.output.Write("Publishing updated webresources...");
			try
			{
				if (!command.NoAction)
				{
					await crm.ExecuteAsync(request);
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while publishing webresources: {ex.Message}");
			}
		}





		private async Task<CommandResult> AddWebResourcesToSolutionAsync(IOrganizationServiceAsync2 crm, Model.Solution solution, List<WebResource> currentWebResources, bool verbose)
		{
			if (currentWebResources.Count == 0)
				return CommandResult.Success();

			try
			{
				this.output.Write("Adding webresources to solution...");
				var result = await solution.UpsertSolutionComponentsAsync(crm, currentWebResources, ComponentType.WebResource);
				this.output.WriteLine("Done", ConsoleColor.Green);

				if (result.ComponentsWithErrors.Count == 0 && result.ComponentsAlreadyThere.Count == 0)
				{
					this.output.Write("All ").Write(currentWebResources.Count, ConsoleColor.Green).WriteLine(" webresources have been added to the solution.");
					return CommandResult.Success();
				}
				if (result.ComponentsAlreadyThere.Count == currentWebResources.Count)
				{
					this.output.Write("Nothing to do, all ").Write(result.ComponentsAlreadyThere.Count, ConsoleColor.Cyan).Write(" webresources are already in the solution.");
					return CommandResult.Success();
				}

				if (result.ComponentsAdded.Count > 0 && result.ComponentsAlreadyThere.Count > 0 && result.ComponentsWithErrors.Count == 0) 
				{
					this.output
						.Write(result.ComponentsAdded.Count, ConsoleColor.Green).Write(" webresources added, ")
						.Write(result.ComponentsAlreadyThere.Count, ConsoleColor.Cyan).Write(" webresources were already there.")
						.WriteLine();

					return CommandResult.Success();
				}


				this.output
					.Write(result.ComponentsAdded.Count, ConsoleColor.Green).Write(" webresources added, ")
					.Write(result.ComponentsWithErrors.Count, ConsoleColor.Red).Write(" got an error, ")
					.Write(result.ComponentsAlreadyThere.Count, ConsoleColor.Cyan).Write(" webresources were already there.");



				foreach (var childResponse in result.ComponentsWithErrors)
				{
					var component = (WebResource)childResponse.Component;

					if (verbose)
					{
						this.output.WriteLine(JsonConvert.SerializeObject(childResponse, Formatting.Indented), ConsoleColor.DarkGray);
						this.output.WriteLine();
					}
					
					this.output.WriteLine($"ERROR while adding '{component.name}' to '{solution.uniquename}': {childResponse.Fault.Message}", ConsoleColor.Red);
				}


				return CommandResult.Success();

			}
			catch (Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while adding components to solution: {ex.Message}");
			}
		}

	}
}
