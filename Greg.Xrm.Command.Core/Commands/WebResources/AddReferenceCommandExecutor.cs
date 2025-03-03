using Greg.Xrm.Command.Commands.WebResources.ProjectFile;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using System.Text.RegularExpressions;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class AddReferenceCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		IWebResourceProjectFileRepository webResourceProjectFileRepository)
		: ICommandExecutor<AddReferenceCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AddReferenceCommand command, CancellationToken cancellationToken)
		{
			DirectoryInfo? root;
			if (command.Path != null)
			{
				root = new DirectoryInfo(command.Path);
				if (!root.Exists)
				{
					return CommandResult.Fail($"The specified path '{command.Path}' does not exist.");
				}
				if (root.GetFiles(".wr.pacx").Length == 0)
				{
					return CommandResult.Fail($"The specified path '{command.Path}' does not contain a .wr.pacx project file.");
				}
			}
			else
			{
				root = FolderTree.RecurseBackFolderContainingFile(".wr.pacx");
			}

			if (root == null)
			{
				return CommandResult.Fail("Unable to locate .wr.pacx project file.");
			}


			var (succeeded, projectFile) = await webResourceProjectFileRepository.TryReadAsync(root);
			if (!succeeded)
			{
				return CommandResult.Fail("Failed to read the project file.");
			}







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


			var file = new FileInfo(command.Source);
			var fileType = WebResource.GetTypeFromExtension(file.FullName);
			if (fileType == null)
			{
				return CommandResult.Fail($"The file '{file.FullName}' is not a recognized web resource type.");
			}



			if (!TryValidateTarget(command.Target, solution.PublisherCustomizationPrefix))
			{
				return CommandResult.Fail($"Invalid target path: {command.Target}.");
			}



			var map = new WebResourceMap(command.Source, command.Target, fileType.Value);
			if (projectFile.ContainsExternalReference(map))
			{
				return CommandResult.Fail($"The web resource '{command.Source}' is already referenced in the project file.");
			}

			projectFile.ExternalReferences.Add(map);
			projectFile.ExternalReferences.Sort((a, b) => string.Compare(a.Target, b.Target, StringComparison.Ordinal));

			await webResourceProjectFileRepository.SaveAsync(root, solution.PublisherCustomizationPrefix!, projectFile);

			return CommandResult.Success();
		}





		private bool TryValidateTarget(string target, string? prefix)
		{
			if (string.IsNullOrEmpty(target))
			{
				output.WriteLine("The target path cannot be empty.", ConsoleColor.Red);
				return false;
			}

			if (string.IsNullOrWhiteSpace(prefix))
			{
				output.WriteLine("The publisher customization prefix is not set in the current solution.", ConsoleColor.Red);
				return false;
			}

			if (!target.StartsWith(prefix + "_"))
			{
				output.WriteLine("The target path must start with the publisher customization prefix.", ConsoleColor.Red);
				return false;
			}


			/*
			Explanation:
				•	^ asserts the position at the start of the string.
				•	(?!.*\.\.) is a negative lookahead that ensures the string does not contain two consecutive dots.
				•	(?!.*[._-]$) is a negative lookahead that ensures the string does not end with a dot, underscore, or hyphen.
				•	[\w.-]+ matches one or more word characters (letters, digits, and underscores), dots, or hyphens.
				•	$ asserts the position at the end of the string.
			*/

			var regex = new Regex(@"^(?!.*\.\.)(?!.*[._-]$)[\w.-]+$");

			var parts = target[(prefix.Length + 1)..].Split('/');

			for (int i = 0; i < parts.Length; i++)
			{
				var part = parts[i];
				if (string.IsNullOrWhiteSpace(part))
				{
					output.WriteLine("The target path contains empty parts.", ConsoleColor.Red);
					return false;
				}

				if (!regex.IsMatch(part))
				{
					output.WriteLine($"The target path part '{part}' is not valid.", ConsoleColor.Red);
					return false;
				}
			}

			return true;
		}
	}
}
