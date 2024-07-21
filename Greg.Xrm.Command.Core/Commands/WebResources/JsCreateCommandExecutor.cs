using Greg.Xrm.Command.Commands.WebResources.Templates;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Greg.Xrm.Command.Model;

namespace Greg.Xrm.Command.Commands.WebResources
{
    public class JsCreateCommandExecutor : ICommandExecutor<JsCreateCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISolutionRepository solutionRepository;
		private readonly IJsTemplateManager jsTemplateManager;

		public JsCreateCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			IJsTemplateManager jsTemplateManager)
        {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
			this.solutionRepository = solutionRepository ?? throw new ArgumentNullException(nameof(solutionRepository));
			this.jsTemplateManager = jsTemplateManager ?? throw new ArgumentNullException(nameof(jsTemplateManager));
		}

        public async Task<CommandResult> ExecuteAsync(JsCreateCommand command, CancellationToken cancellationToken)
		{
			// devo navigare l'albero delle cartelle per trovare la cartella radice del mio pacchetto di webresource
			// (è la cartella che contiene il file .wr.pacx). 
			// Se non la trovo, creo il file nella cartella locale.
			// Se la trovo, navigo la sottocartella che si chiama come il prefix del publisher della solution di default, entro poi nella sottocartella scripts e lo creo lì.

			// recupero la connessione all'ambiente, ed il nome della solution da considerare
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();

			var root = FolderTree.RecurseBackFolderContainingFile(".wr.pacx");
			if (root == null)
			{
				var directory = new DirectoryInfo(Environment.CurrentDirectory);
				return await CreateFileAsync(command, crm, directory, cancellationToken);
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

			// recupero la solution
			var solution = await this.solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
			if (solution == null)
			{
				return CommandResult.Fail($"The solution <{currentSolutionName}> is not present in the current environment.");
			}

			// identifico la cartella del publisher
			var publisherSubFolder = root.GetDirectories(solution.PublisherCustomizationPrefix + "_").FirstOrDefault();
			if (publisherSubFolder == null)
			{
				this.output.WriteLine($"There's no subfolder called '{solution.PublisherCustomizationPrefix}_' under '{root.FullName}'. The file will be created in the current folder.");

				var directory = new DirectoryInfo(Environment.CurrentDirectory);
				return await CreateFileAsync(command, crm, directory, cancellationToken);
			}

			// identifico la cartella scripts
			var scriptsFolder = publisherSubFolder.GetDirectories("scripts").FirstOrDefault();
			if (scriptsFolder == null)
			{
				this.output.WriteLine($"There's no subfolder called 'scripts' under '{publisherSubFolder.FullName}'. The file will be created in the current folder.");

				var directory = new DirectoryInfo(Environment.CurrentDirectory);
				return await CreateFileAsync(command, crm, directory, cancellationToken);
			}

			return await CreateFileAsync(command, crm, scriptsFolder, cancellationToken);
		}




		private async Task<CommandResult> CreateFileAsync(JsCreateCommand command, IOrganizationServiceAsync2 crm, DirectoryInfo directory, CancellationToken cancellationToken)
		{
			if (command.Type == JavascriptWebResourceType.Form && string.IsNullOrWhiteSpace(command.TableName))
			{
				return CommandResult.Fail("Table name is required for form scripts");
			}



			var ns = command.Namespace;
			if (string.IsNullOrWhiteSpace(ns))
			{
				var currentSolutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(currentSolutionName))
				{
					currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (currentSolutionName == null)
					{
						return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
					}
				}

				var (success, uniqueName) = await TryCheckSolution(crm, currentSolutionName);
				if (!success)
				{
					// in this case, uniqueName contains the error message
					return CommandResult.Fail(uniqueName);
				}

				ns = uniqueName;
			}


			var fileName = Path.Combine(directory.FullName, CreateFileName(command));
			if (File.Exists(fileName))
			{
				return CommandResult.Fail("The file already exists: " + fileName);
			}



			var template = await this.jsTemplateManager.GetTemplateForAsync(command.Type, string.IsNullOrWhiteSpace(command.TableName));
			if (string.IsNullOrWhiteSpace(template))
			{
				return CommandResult.Fail("No template found for the specified type");
			}

			var fileContent = template.Replace("%NAMESPACE%", ns).Replace("%TABLE%", command.TableName ?? string.Empty);

			await File.WriteAllTextAsync(fileName, fileContent, cancellationToken);

			var result = CommandResult.Success();
			result["File"] = fileName;
			return result;
		}



		private static string CreateFileName(JsCreateCommand command)
		{
			if (command.Type == JavascriptWebResourceType.Form)
			{
				return $"{command.TableName}.js";
			}
			if (command.Type == JavascriptWebResourceType.Ribbon && !string.IsNullOrWhiteSpace(command.TableName))
			{
				return $"{command.TableName}.Ribbon.js";
			}
			if (command.Type == JavascriptWebResourceType.Ribbon && string.IsNullOrWhiteSpace(command.TableName))
			{
				return $"Global.Ribbon.js";
			}
			return "Common.js";
		}




		private async Task<(bool, string)> TryCheckSolution(IOrganizationServiceAsync2 crm, string currentSolutionName)
		{
			output.WriteLine("Checking solution existence and retrieving publisher uniquename");

			var solution = await this.solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
			if (solution == null)
			{
				return (false, "Invalid solution name: " + currentSolutionName);
			}

			if (solution.ismanaged)
			{
				return (false, "The provided solution is managed. You must specify an unmanaged solution.");
			}

			var uniqueName = solution.PublisherUniqueName;
			if (string.IsNullOrWhiteSpace(uniqueName))
			{
				return (false, "Unable to retrieve the publisher unique name. Please report a bug to the project GitHub page.");
			}

			return (true, uniqueName);
		}
	}
}
