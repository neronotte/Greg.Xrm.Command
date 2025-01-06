using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Services.Project
{
	public class PacxProjectRepository(ILogger<PacxProjectRepository> logger) : IPacxProjectRepository
	{
		private readonly ILogger<PacxProjectRepository>? logger = logger;

		public async Task<PacxProjectDefinition?> GetCurrentProjectAsync()
		{
			var projectFolder = FolderTree.RecurseBackFolderContainingFile(PacxProject.FileName);
			if (projectFolder == null) return null;

			var projectFile = projectFolder.GetFiles(PacxProject.FileName).FirstOrDefault();
			if (projectFile == null) return null;

			try
			{
				var text = await File.ReadAllTextAsync(projectFile.FullName);

				var projectDefinition = JsonConvert.DeserializeObject<PacxProjectDefinition>(text);
				if (projectDefinition == null)
				{
					logger?.LogError("Error while reading the project file {ProjectFile}, the file content cannot be deserialized", projectFile.FullName);
					return null;
				}

				var validationResults = new List<ValidationResult>();
				if (!Validator.TryValidateObject(projectDefinition, new ValidationContext(projectDefinition), validationResults))
				{
					foreach (var validationResult in validationResults)
					{
						logger?.LogError("Error while reading the project file {ProjectFile}, the file content is not valid: {ErrorMessage}", projectFile.FullName, validationResult.ErrorMessage);
					}
					return null;
				}

				return projectDefinition;
			}
			catch (Exception ex)
			{
				this.logger?.LogError(ex, "Error while reading the project file {ProjectFile}", projectFile.FullName);
				return null;
			}
		}




		public async Task SaveAsync(PacxProjectDefinition projectDefinition, string folder, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(projectDefinition);


			Validator.ValidateObject(projectDefinition, new ValidationContext(projectDefinition));

			var file = Path.Combine(folder, PacxProject.FileName);
			var projectString = JsonConvert.SerializeObject(projectDefinition, Formatting.Indented);
			await File.WriteAllTextAsync(file, projectString, cancellationToken);
		}
	}
}
