using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Versioning;
using OfficeOpenXml.Packaging;
using System.IO;

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
            if (!string.IsNullOrWhiteSpace(command.Name))
            {
                return await DownloadPluginFromNuget(command, cancellationToken);
            }
            else
            {
                return await DownloadPluginFromFile(command, cancellationToken);
            }
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

        private static string GetFileRelativePath(string fileName, NuGetFramework? nuGetFramework)
        {
            string frameworkFolder =  (fileName.StartsWith("lib/") && nuGetFramework != null) ? $"lib/{nuGetFramework.GetShortFolderName()}/" : ".";

            var relativeFolder = Path.GetRelativePath(frameworkFolder, fileName);

            if (!relativeFolder.StartsWith(".."))
            {
                return Path.GetDirectoryName(relativeFolder) ?? string.Empty;
            }

            return string.Empty;
        }

        private static async Task<string[]> ExtractValidFilesFromPackage(PackageArchiveReader packageReader, NuGetFramework? bestSupportedFramework, CancellationToken cancellationToken)
        {
            var files = await packageReader.GetFilesAsync(cancellationToken);
            var validFiles = files.Where(f => 
                (bestSupportedFramework != null && f.StartsWith($"lib/{bestSupportedFramework.GetShortFolderName()}/"))
                || f.StartsWith("runtimes/")
            ).ToArray();
            return validFiles;
        }

        private bool ExtractPackageContentToFolder(PackageArchiveReader packageReader, string rootPackageId, NuGetFramework? bestSupportedFramework, string[] validFiles)
        {
            var nuspecReader = packageReader.NuspecReader;
            var packageId = nuspecReader.GetId();
            var packageVersion = nuspecReader.GetVersion().ToFullString();
            packageVersion = nuspecReader.GetVersion().ToString();

            if (rootPackageId.Equals(packageId)) {
                bool isValidRoot = ValidatePackage(packageReader, rootPackageId);
                if (!isValidRoot) return false;
            }

            output.Write($"Installing {packageId} under {rootPackageId}...");
            var extractedFileList = new List<string>();
            DirectoryInfo? currentPluginFolder = null;
            try
            {
                var rootStorageFolder = storage.GetOrCreateStorageFolder();
                var pluginStorageFolder = rootStorageFolder.CreateSubdirectory("Plugins");

                currentPluginFolder = pluginStorageFolder.GetDirectories().FirstOrDefault(d => d.Name.Equals(rootPackageId, StringComparison.OrdinalIgnoreCase));
                if (currentPluginFolder == null)
                {
                    throw new FileNotFoundException($"Cannot get {rootPackageId} plugin folder");
                }

                foreach (var file in validFiles)
                {
                    var relativeFolder = GetFileRelativePath(file, bestSupportedFramework);
                    var path = Path.Combine(currentPluginFolder.FullName, relativeFolder, Path.GetFileName(file));
                    packageReader.ExtractFile(file, path, logger);
                    extractedFileList.Add(path);
                }
                
                if (rootPackageId.Equals(packageId))
                {
                    var versionFile = Path.Combine(currentPluginFolder.FullName, ".version");
                    File.WriteAllText(versionFile, packageVersion);
                }

                output.WriteLine("Done", ConsoleColor.Green);


                return true;
            }
            catch (Exception ex)
            {
                output.WriteLine("Failed", ConsoleColor.Red);
                return false;
            }
        }

        private void PrepareDestinationFolder(string packageId)
        {
            var rootStorageFolder = storage.GetOrCreateStorageFolder();
            var pluginStorageFolder = rootStorageFolder.CreateSubdirectory("Plugins");

            DirectoryInfo? currentPluginFolder = pluginStorageFolder.GetDirectories().FirstOrDefault(d => d.Name.Equals(packageId, StringComparison.OrdinalIgnoreCase));
            currentPluginFolder?.Delete(true);

            currentPluginFolder = pluginStorageFolder.CreateSubdirectory(packageId);
        }

        private NuGetFramework? GetBestSupportedFramework(IEnumerable<NuGetFramework> frameworks)
        {
            var projectFramework = NuGetFramework.Parse(AppContext.TargetFrameworkName);

            // Filter compatible frameworks
            var compatibleFrameworks = frameworks
                .Where(f => !f.AllFrameworkVersions && !f.IsAny)
                .Where(f => DefaultCompatibilityProvider.Instance.IsCompatible(projectFramework, f))
                .ToList();

            // Select the most specific framework (highest version)
            var bestFramework = compatibleFrameworks
                .OrderByDescending(f => f.Version)
                .FirstOrDefault();

            if (bestFramework != null)
            {
                return bestFramework;
            }

            var result = frameworks
                .Where(f => !f.AllFrameworkVersions && !f.IsAny)
                .OrderByDescending(f => f.Version)
                .FirstOrDefault();

            if (result != null)
            {
                output.Write($"(no compatible framework found, using {result?.GetShortFolderName()})", ConsoleColor.Yellow);
            }

            return result;
        }

        private bool ValidatePackage(PackageArchiveReader packageReader, string rootPackageId)
        {
            var nuspecReader = packageReader.NuspecReader;
            var packageId = nuspecReader.GetId();

            var dependencies = nuspecReader.GetDependencyGroups()?.ToArray()
                .SelectMany(d => d.Packages)
                .OrderBy(p => p.Id)
                .ToList() ?? [];

            var isValid = dependencies.Exists(d => d.Id == "Greg.Xrm.Command.Interfaces");
            if (!isValid)
            {
                this.output.WriteLine($"Package <{rootPackageId}> is not a valid PACX plugin. A valid PACX plugin must have a dependency on <Greg.Xrm.Command.Interfaces>.", ConsoleColor.Red);
                return false;
            }

            return true;
        }
        private async Task<(bool, PackageIdentity? packageIdentity, bool)> ValidatePackage(SourceRepository repository, string packageId, string packageVersion, SourceCacheContext cacheContext, CancellationToken cancellationToken)
        {
            output.Write("Searching for package version...");

            var packageMetadataResource = await repository.GetResourceAsync<PackageMetadataResource>();

            var packageMetadata = (await packageMetadataResource.GetMetadataAsync(packageId, false, true, cacheContext, logger, cancellationToken))?.ToList() ?? [];
            if (packageMetadata.Count == 0)
            {
                output.WriteLine($"Package <{packageId}> not found on NuGet.", ConsoleColor.Red);
                return (false, null, false);
            }

            IPackageSearchMetadata? package;
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                package = packageMetadata
                    .OrderByDescending(p => p.Identity.Version, VersionComparer.VersionRelease)
                    .FirstOrDefault();

                if (package == null)
                {
                    output.WriteLine($"Package <{packageId}> doesn't have any version marked as \"release\", thus cannot be installed.", ConsoleColor.Red);
                    return (false, null, false);
                }
            }
            else
            {
                var canParseVersion = NuGetVersion.TryParse(packageVersion, out SemanticVersion? parsedVersion);
                if (!canParseVersion)
                {
                    output.WriteLine($"Invalid version <{packageVersion}> for package <{packageId}>.", ConsoleColor.Red);
                    return (false, null, false);
                }
                package = packageMetadata.Find(p =>p.Identity.Version.Equals(parsedVersion, VersionComparison.Version));

                if (package == null)
                {
                    var validVersions = string.Join(", ", packageMetadata.OrderByDescending(p => p.Identity.Version, VersionComparer.VersionRelease).Select(p => p.Identity.Version.Version.ToString()));
                    output.WriteLine($"Version <{packageVersion}> not found for package <{packageId}>. Valid versions are: " + validVersions, ConsoleColor.Red);

                    return (false, null, false);
                }
            }

            output.WriteLine("Done", ConsoleColor.Green);

            var dependencies = package.DependencySets
                .SelectMany(d => d.Packages)
                .OrderBy(p => p.Id)
                .ToList();

            var isValid = dependencies.Exists(d => d.Id == "Greg.Xrm.Command.Interfaces");
            return (isValid, package.Identity, dependencies.Count > 1);
        }

        private async Task<bool> ExtractPackageStream(Stream? packageStream,string packageId, string path, CancellationToken cancellationToken)
        {
            using (packageStream)
            using (var packageReader = new PackageArchiveReader(packageStream))
            {
                output.Write($"Reading package {packageId} contents...");

                var fwks = await packageReader.GetSupportedFrameworksAsync(cancellationToken);
                var bestSupportedFramework = GetBestSupportedFramework(fwks);
                if (bestSupportedFramework == null)
                {
                    output.WriteLine("No compatible framework, only runtimes will be extracted", ConsoleColor.Yellow);
                }
                var validFiles = await ExtractValidFilesFromPackage(packageReader, bestSupportedFramework, cancellationToken);

                if (validFiles.Length == 0 && packageId.Equals(path))
                {
                    output.WriteLine("No valid files found. A valid plugin package must contain files (dlls) under lib/net<> folder.", ConsoleColor.Red);
                    return false;
                }

                output.WriteLine("Done", ConsoleColor.Green);

                if (!packageId.Equals(path))
                {
                    validFiles = validFiles.Where(f => Path.GetExtension(f).ToLowerInvariant().EndsWith(".dll")).ToArray();
                }

                return ExtractPackageContentToFolder(packageReader, path, bestSupportedFramework, validFiles);
            }
        }

        private async Task<bool> DownloadPackage(PackageIdentity targetPackage, string path, SourceRepository repository, SourceCacheContext cacheContext, CancellationToken cancellationToken)
        {
            try
            {
                var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

                var packageStream = new MemoryStream();
                await resource.CopyNupkgToStreamAsync(
                    targetPackage.Id,
                    targetPackage.Version,
                    packageStream,
                    cacheContext,
                    logger,
                    cancellationToken);
                               
                if (packageStream == null)
                {
                    output.WriteLine("Failed", ConsoleColor.Red);
                    return false;
                }

                return await ExtractPackageStream(packageStream, targetPackage.Id, path, cancellationToken);

            }
            catch (Exception ex)
            {
                output.WriteLine($"Failed: {ex.Message}", ConsoleColor.Red);
                return false;
            }

        }

        private async Task<(bool, PackageIdentity?)> GetCorePackageIdentity(SourceRepository repository, SourceCacheContext cacheContext, CancellationToken cancellationToken)
        {
            var corePackageId = "Greg.Xrm.Command.Interfaces";
            var packageMetadataResource = await repository.GetResourceAsync<PackageMetadataResource>();

            var packageMetadata = (await packageMetadataResource.GetMetadataAsync(corePackageId, false, true, cacheContext, logger, cancellationToken))?.ToList() ?? [];
            if (packageMetadata.Count == 0)
            {
                output.WriteLine($"Package <{corePackageId}> not found on NuGet.", ConsoleColor.Red);
                return (false, null);
            }

            IPackageSearchMetadata? package;
            package = packageMetadata
                .OrderByDescending(p => p.Identity.Version, VersionComparer.VersionRelease)
                .FirstOrDefault();

            if (package == null)
            {
                output.WriteLine($"Package <{corePackageId}> doesn't have any version marked as \"release\", thus cannot be installed.", ConsoleColor.Red);
                return (false, null);
            }

            return (true, package.Identity);
        }

        private async Task<CommandResult> DownloadPluginFromNuget(InstallCommand command, CancellationToken cancellationToken)
        {
            var cacheContext = new SourceCacheContext();
            var source = string.IsNullOrWhiteSpace(command.Source) ? new PackageSource("https://api.nuget.org/v3/index.json") : new PackageSource(command.Source, "customsource")
            {
                Credentials = new PackageSourceCredential("customsource", Environment.UserName, command.PersonalAccessToken, true, string.Empty)
            };

            var repository = Repository.Factory.GetCoreV3(source);

            var (isValid, targetPackage, hasOtherDependencies) = await ValidatePackage(repository, command.Name, command.Version, cacheContext, cancellationToken);
            if (!isValid || targetPackage == null)
            {
                return CommandResult.Fail($"Package <{command.Name}> is not a valid PACX plugin. A valid PACX plugin must have a dependency on <Greg.Xrm.Command.Interfaces>.");
            }

            var packagesToInstall = new Dictionary<string, NuGetVersion>() { { targetPackage.Id, targetPackage.Version } };
            if (hasOtherDependencies)
            {
                var dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();
                var (foundCore, corePackage) = await GetCorePackageIdentity(repository, cacheContext, cancellationToken);
                if (!foundCore || corePackage == null)
                {
                    return CommandResult.Fail("Failed to get core package identity");
                }

                output.Write("Getting package dependencies...");

                packagesToInstall = await DependencyUtility.GetDeltaForPackage(
                   corePackage,
                   targetPackage,
                   dependencyInfoResource,
                   NuGetFramework.Parse(AppContext.TargetFrameworkName!), cacheContext, logger, cancellationToken);

                output.WriteLine("Done", ConsoleColor.Green);
            }

            PrepareDestinationFolder(targetPackage.Id);

            bool result = true;
            foreach (var currentPackage in packagesToInstall)
            {
                result &= await DownloadPackage(new PackageIdentity(currentPackage.Key, currentPackage.Value), targetPackage.Id, repository, new SourceCacheContext(), CancellationToken.None);
            }            

            if (!result)
            {
                return CommandResult.Fail("Failed to download plugin from NuGet.");
            }

            return CommandResult.Success();
        }
        private async Task<CommandResult> DownloadPluginFromFile(InstallCommand command, CancellationToken cancellationToken)
        {
            Stream? packageStream;
            var (result, stream) = TryCreateStreamFromLocalFile(command);
            if (!result.IsSuccess) return result;
            packageStream = stream!;

            var infoStream = new MemoryStream();
            packageStream.CopyTo(infoStream);

            string packageId;
            using (infoStream)
            using (var packageReader = new PackageArchiveReader(infoStream))
            {
                packageId = packageReader.NuspecReader.GetId();
            }

            PrepareDestinationFolder(packageId);
            bool extractResult = await ExtractPackageStream(packageStream, packageId, packageId,  cancellationToken);
            if (!extractResult)
            {
                return CommandResult.Fail("Failed to extract plugin from file.");
            }

            return CommandResult.Success();
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
