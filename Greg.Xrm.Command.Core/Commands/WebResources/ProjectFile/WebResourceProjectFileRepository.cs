using Greg.Xrm.Command.Services.Output;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Greg.Xrm.Command.Commands.WebResources.ProjectFile
{
	public class WebResourceProjectFileRepository(IOutput output) : IWebResourceProjectFileRepository
	{
		const string FileName = ".wr.pacx";

		private static ProjectFileV1 CreateProjectFile()
		{
			return new ProjectFileV1();
		}


		public async Task<(bool, ProjectFileV1)> TryReadAsync(DirectoryInfo folder)
		{
			var projectFileName = Path.Combine(folder.FullName, FileName);

			if (!File.Exists(projectFileName))
			{
				output.WriteLine($"The project file <{projectFileName}> does not exist, creating a new one.", ConsoleColor.Yellow);
				return (true, CreateProjectFile());
			}


			output.Write($"Reading project file {FileName}...");
			string fileContent;
			try
			{
				fileContent = await File.ReadAllTextAsync(projectFileName);
				output.WriteLine("Done", ConsoleColor.Green);
				if (string.IsNullOrWhiteSpace(fileContent))
				{
					return (true, CreateProjectFile());
				}
			}
			catch(Exception ex)
			{
				output.WriteLine($"Failed: {ex.Message}", ConsoleColor.Red);
				return (false, CreateProjectFile());
			}

			var contentObject = JsonConvert.DeserializeObject(fileContent) as JObject;
			if (contentObject == null)
			{
				output.WriteLine("The project file is not in a recognized format.", ConsoleColor.Yellow);
				return (false, CreateProjectFile());
			}

			var version = contentObject["Version"]?.Value<string>();

			if (version == "1")
			{
				return (true, JsonConvert.DeserializeObject<ProjectFileV1>(fileContent) ?? CreateProjectFile());
			}

			output.WriteLine($"The project file version ({version}) is not supported.", ConsoleColor.Yellow);
			return (false, CreateProjectFile());
		}


		public async Task SaveAsync(DirectoryInfo folder, string publisherPrefix, ProjectFileV1? projectFile = null)
		{
			projectFile = projectFile ?? CreateProjectFile();

			var projectFilePath = Path.Combine(folder.FullName, FileName);

			if (File.Exists(projectFilePath))
			{
				output.Write("Updating the WebResources project file...");
			}
			else
			{
				output.Write("Creating the WebResources project file...");
			}


			await File.WriteAllTextAsync(projectFilePath, JsonConvert.SerializeObject(projectFile, Formatting.Indented));
			output.WriteLine("Done", ConsoleColor.Green);

			var subdirectoryName = publisherPrefix + "_";
			var childFolder = folder.GetDirectories().FirstOrDefault(x => x.Name == subdirectoryName);
			if (childFolder == null)
			{
				output.Write($"Creating subfolder <{subdirectoryName}>...");
				folder = folder.CreateSubdirectory(subdirectoryName);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			else
			{
				folder = childFolder;
			}



			var directoryNames = new string[] { "images", "scripts", "pages" };
			foreach (var directoryName in directoryNames)
			{
				var childFolder2 = folder.GetDirectories().FirstOrDefault(x => x.Name == directoryName);
				if (childFolder2 != null)
				{
					continue;
				}

				output.Write($"Creating subfolder <{directoryName}>...");
				folder.CreateSubdirectory(directoryName);
				output.WriteLine("Done", ConsoleColor.Green);
			}

			output.WriteLine($"Web resources project initialized in <{folder.FullName}>");
		}
	}
}
