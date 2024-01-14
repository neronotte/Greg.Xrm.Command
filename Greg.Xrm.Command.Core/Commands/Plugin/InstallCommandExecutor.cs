using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Greg.Xrm.Command.Commands.Plugin
{
    public class InstallCommandExecutor : ICommandExecutor<InstallCommand>
	{
		private readonly IOutput output;
		private readonly IStorage storage;
		private readonly Logger logger;

		public InstallCommandExecutor(
			ILogger<InstallCommandExecutor> log, 
			IOutput output,
			IStorage storage)
		{
			this.output = output;
			this.storage = storage;
			this.logger = new Logger(output);
		}


		public async Task<CommandResult> ExecuteAsync(InstallCommand command, CancellationToken cancellationToken)
		{
			this.output.Write("Creating NuGet repository...");

			var cache = new SourceCacheContext();
			var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			var packageMetadataResource = await repository.GetResourceAsync<PackageMetadataResource>();

			this.output.WriteLine("Done", ConsoleColor.Green);





			this.output.Write("Searching for package version...");
			var packageMetadata = (await packageMetadataResource.GetMetadataAsync(command.Name, false, true, cache, logger, cancellationToken))?.ToList() ?? new List<IPackageSearchMetadata>();
			if (packageMetadata.Count == 0)
			{
				this.output.WriteLine("Failed.", ConsoleColor.Red);
				return CommandResult.Fail($"Package <{command.Name}> not found on NuGet.");
			}

			IPackageSearchMetadata? package;
			if (string.IsNullOrWhiteSpace(command.Version))
			{
				package = packageMetadata
					.OrderByDescending(p => p.Identity.Version, VersionComparer.VersionRelease)
					.FirstOrDefault();

				if (package == null)
				{
					this.output.WriteLine("Failed.", ConsoleColor.Red);
					return CommandResult.Fail($"Package <{command.Name}> doesn't have any version marked as \"release\", thus cannot be installed.");
				}
			}
			else
			{
				package = packageMetadata.Find(p => string.Equals(p.Identity.Version.Version.ToString(), command.Version, StringComparison.OrdinalIgnoreCase));
				if (package == null)
				{
					this.output.WriteLine("Failed.", ConsoleColor.Red);
					var validVersions = string.Join(", ", packageMetadata.OrderByDescending(p => p.Identity.Version, VersionComparer.VersionRelease).Select(p => p.Identity.Version.Version.ToString()));
					return CommandResult.Fail($"Version <{command.Version}> not found for package <{command.Name}>. Valid versions are: " + validVersions);
				}
			}
			this.output.WriteLine("Done", ConsoleColor.Green);




			this.output.Write("Validating package...");
			var dependencies = package.DependencySets
				.SelectMany(d => d.Packages)
				.OrderBy(p => p.Id)
				.ToList();

			var isValid = dependencies.Exists(d => d.Id == "Greg.Xrm.Command.Interfaces");
			if (!isValid)
			{
				this.output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Package <{command.Name}> is not a valid PACX plugin. A valid PACX plugin must have a dependency on <Greg.Xrm.Command.Interfaces>.");
			}
			this.output.WriteLine("Done", ConsoleColor.Green);




			this.output.Write($"Downloading Package {package.Identity.Id} {package.Identity.Version}...");
			var resource = await repository.GetResourceAsync<FindPackageByIdResource>();
			using var packageStream = new MemoryStream();

			await resource.CopyNupkgToStreamAsync(
				package.Identity.Id,
				package.Identity.Version,
				packageStream,
				cache,
				logger,
				cancellationToken);
			this.output.WriteLine("Done", ConsoleColor.Green);




			using (var packageReader = new PackageArchiveReader(packageStream))
			{
				this.output.Write($"Reading package contents...");

				var validFiles = await ExtractValidFilesFromPackage(packageReader, cancellationToken);

				if (validFiles.Length == 0)
				{
					this.output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail("No valid files found. A valid plugin package must contain files (dlls) under lib/net7.0/ or lib/net6.0/ folder.");
				}

				this.output.WriteLine("Done", ConsoleColor.Green);


				return ExtractPlugin(package, packageReader, validFiles);
			}
		}





		private static async Task<string[]> ExtractValidFilesFromPackage(PackageArchiveReader packageReader, CancellationToken cancellationToken)
		{
			var files = await packageReader.GetFilesAsync(cancellationToken);

			var validFiles = files.Where(f => f.StartsWith("lib/net7.0/")).ToArray();
			if (validFiles.Length == 0)
			{
				validFiles = files.Where(f => f.StartsWith("lib/net6.0/")).ToArray();
			}

			return validFiles;
		}





		private CommandResult ExtractPlugin(IPackageSearchMetadata plugin, PackageArchiveReader packageReader, string[] validFiles)
		{
			this.output.Write($"Installing plugin...");
			var extractedFileList = new List<string>();
			DirectoryInfo? currentPluginFolder = null;
			try
			{
				var rootStorageFolder = storage.GetOrCreateStorageFolder();
				var pluginStorageFolder = rootStorageFolder.CreateSubdirectory("Plugins");

				currentPluginFolder = pluginStorageFolder.GetDirectories().FirstOrDefault(d => d.Name.Equals(plugin.Identity.Id, StringComparison.OrdinalIgnoreCase));
				if (currentPluginFolder != null)
				{
					this.output.Write($"Deleting existing plugin folder...");
					currentPluginFolder.Delete(true);
					this.output.WriteLine("Done", ConsoleColor.Green);
				}

				currentPluginFolder = pluginStorageFolder.CreateSubdirectory(plugin.Identity.Id);

				foreach (var file in validFiles)
				{
					var path = Path.Combine(currentPluginFolder.FullName, Path.GetFileName(file));
					packageReader.ExtractFile(file, path, logger);
					extractedFileList.Add(path);
				}

				var versionFile = Path.Combine(currentPluginFolder.FullName, ".version");
				File.WriteAllText(versionFile, plugin.Identity.Version.ToString());

				this.output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["extractedFiles"] = string.Join(Environment.NewLine, extractedFileList);
				return result;
			}
			catch (Exception ex)
			{
				this.output.WriteLine("Failed", ConsoleColor.Red);


				// Cleanup
				foreach (var file in extractedFileList)
				{
					File.Delete(file);
				}

				if (currentPluginFolder != null)
				{
					currentPluginFolder.Refresh();

					if (currentPluginFolder.Exists)
						currentPluginFolder.Delete(true);
				}


				return CommandResult.Fail(ex.Message);
			}
		}



		class Logger : LoggerBase
		{
			private readonly IOutput output;

			public Logger(IOutput output)
			{
				this.output = output;
			}

			public override void Log(ILogMessage message)
			{
				var color = message.Level switch
				{
					NuGet.Common.LogLevel.Debug => ConsoleColor.Gray,
					NuGet.Common.LogLevel.Error => ConsoleColor.Red,
					NuGet.Common.LogLevel.Information => ConsoleColor.Cyan,
					NuGet.Common.LogLevel.Minimal => ConsoleColor.Gray,
					NuGet.Common.LogLevel.Verbose => ConsoleColor.Gray,
					NuGet.Common.LogLevel.Warning => ConsoleColor.Yellow,
					_ => ConsoleColor.White,
				};

				this.output.WriteLine(message.Message, color);
			}

			public override Task LogAsync(ILogMessage message)
			{
				Log(message);
				return Task.CompletedTask;
			}
		}
	}
}
