using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Versioning;
using OfficeOpenXml.Packaging;

namespace Greg.Xrm.Command.Commands.Plugin
{
    public class InstallCommandExecutor : ICommandExecutor<InstallCommand>
    {
        private readonly IOutput output;
        private readonly IStorage storage;
        private readonly Logger logger;
        private static readonly string[] validFilesExtractionOrder = ["lib/net8.0/", "lib/net7.0/", "lib/net6.0/"];

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
            Stream packageStream;
            if (!string.IsNullOrWhiteSpace(command.Name))
            {
                var (result, stream) = await TryGetPackageFromNugetAsync(command, cancellationToken);
                if (!result.IsSuccess) return result;
                packageStream = stream!;
            }
            else
            {
                var (result, stream) = TryCreateStreamFromLocalFile(command);
                if (!result.IsSuccess) return result;
                packageStream = stream!;
            }


            using (packageStream)
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


                return ExtractPlugin(packageReader, validFiles);
            }
        }

        private async Task<(CommandResult, Stream?)> TryGetPackageFromNugetAsync(InstallCommand command, CancellationToken cancellationToken)
        {
            this.output.Write("Creating NuGet repository...");

            var cache = new SourceCacheContext();
            var source = string.IsNullOrWhiteSpace(command.Source) ? new PackageSource("https://api.nuget.org/v3/index.json") : new PackageSource(command.Source, "customsource")
            {
                Credentials = new PackageSourceCredential("customsource", Environment.UserName, command.PersonalAccessToken, true, string.Empty)
            };


            var repository = Repository.Factory.GetCoreV3(source);
            var packageMetadataResource = await repository.GetResourceAsync<PackageMetadataResource>();

            this.output.WriteLine("Done", ConsoleColor.Green);


            this.output.Write("Searching for package version...");
            var packageMetadata = (await packageMetadataResource.GetMetadataAsync(command.Name, false, true, cache, logger, cancellationToken))?.ToList() ?? new List<IPackageSearchMetadata>();
            if (packageMetadata.Count == 0)
            {
                this.output.WriteLine("Failed.", ConsoleColor.Red);
                return (CommandResult.Fail($"Package <{command.Name}> not found on NuGet."), null);
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
                    return (CommandResult.Fail($"Package <{command.Name}> doesn't have any version marked as \"release\", thus cannot be installed."), null);
                }
            }
            else
            {
                package = packageMetadata.Find(p => string.Equals(p.Identity.Version.Version.ToString(), command.Version, StringComparison.OrdinalIgnoreCase));
                if (package == null)
                {
                    this.output.WriteLine("Failed.", ConsoleColor.Red);
                    var validVersions = string.Join(", ", packageMetadata.OrderByDescending(p => p.Identity.Version, VersionComparer.VersionRelease).Select(p => p.Identity.Version.Version.ToString()));
                    return (CommandResult.Fail($"Version <{command.Version}> not found for package <{command.Name}>. Valid versions are: " + validVersions), null);
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
                return (CommandResult.Fail($"Package <{command.Name}> is not a valid PACX plugin. A valid PACX plugin must have a dependency on <Greg.Xrm.Command.Interfaces>."), null);
            }
            this.output.WriteLine("Done", ConsoleColor.Green);


            this.output.Write($"Downloading Package {package.Identity.Id} {package.Identity.Version}...");
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            var packageStream = new MemoryStream();
            await resource.CopyNupkgToStreamAsync(
                package.Identity.Id,
                package.Identity.Version,
                packageStream,
                cache,
                logger,
                cancellationToken);
            this.output.WriteLine("Done", ConsoleColor.Green);

            return (CommandResult.Success(), packageStream);
        }

        private static (CommandResult, Stream?) TryCreateStreamFromLocalFile(InstallCommand command)
        {
            var file = new FileInfo(command.FileName);
            if (!file.Exists)
            {
                return (CommandResult.Fail($"The specified file <{command.FileName}> does not exist."), null);
            }
            if (!file.Extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                return (CommandResult.Fail($"The specified file <{command.FileName}> is not a valid NuGet package file."), null);
            }

            var packageStream = File.OpenRead(command.FileName);
            return (CommandResult.Success(), packageStream);
        }

        private static string GetFileRelativePath(string fileName)
        {
            foreach (var folder in validFilesExtractionOrder)
            {
                var relativeFolder = Path.GetRelativePath(folder, fileName);

                if (!relativeFolder.StartsWith(".."))
                {
                    return Path.GetDirectoryName(relativeFolder) ?? string.Empty;
                }
            }
            return string.Empty;
        }

        private static async Task<string[]> ExtractValidFilesFromPackage(PackageArchiveReader packageReader, CancellationToken cancellationToken)
        {
            var files = await packageReader.GetFilesAsync(cancellationToken);

            foreach (var folder in validFilesExtractionOrder)
            {
                var validFiles = files.Where(f => f.StartsWith(folder)).ToArray();
                if (validFiles.Length > 0)
                {
                    return validFiles;
                }
            }
            return [];
        }


        private CommandResult ExtractPlugin(PackageArchiveReader packageReader, string[] validFiles)
        {
            var nuspecReader = packageReader.NuspecReader;
            var packageId = nuspecReader.GetId();
            var packageVersion = nuspecReader.GetVersion().ToFullString();
            packageVersion = nuspecReader.GetVersion().ToString();




            this.output.Write("Validating package...");
            var dependencies = nuspecReader.GetDependencyGroups()?.ToArray()
                .SelectMany(d => d.Packages)
                .OrderBy(p => p.Id)
                .ToList() ?? [];

            var isValid = dependencies.Exists(d => d.Id == "Greg.Xrm.Command.Interfaces");
            if (!isValid)
            {
                this.output.WriteLine("Failed", ConsoleColor.Red);
                return CommandResult.Fail($"Package <{packageId}> is not a valid PACX plugin. A valid PACX plugin must have a dependency on <Greg.Xrm.Command.Interfaces>.");
            }
            this.output.WriteLine("Done", ConsoleColor.Green);




            this.output.Write($"Installing plugin...");
            var extractedFileList = new List<string>();
            DirectoryInfo? currentPluginFolder = null;
            try
            {
                var rootStorageFolder = storage.GetOrCreateStorageFolder();
                var pluginStorageFolder = rootStorageFolder.CreateSubdirectory("Plugins");

                currentPluginFolder = pluginStorageFolder.GetDirectories().FirstOrDefault(d => d.Name.Equals(packageId, StringComparison.OrdinalIgnoreCase));
                if (currentPluginFolder != null)
                {
                    this.output.Write($"Deleting existing plugin folder...");
                    currentPluginFolder.Delete(true);
                    this.output.WriteLine("Done", ConsoleColor.Green);
                }

                currentPluginFolder = pluginStorageFolder.CreateSubdirectory(packageId);

                foreach (var file in validFiles)
                {
                    var relativeFolder = GetFileRelativePath(file);
                    var path = Path.Combine(currentPluginFolder.FullName, relativeFolder, Path.GetFileName(file));
                    packageReader.ExtractFile(file, path, logger);
                    extractedFileList.Add(path);
                }

                var versionFile = Path.Combine(currentPluginFolder.FullName, ".version");
                File.WriteAllText(versionFile, packageVersion);

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
