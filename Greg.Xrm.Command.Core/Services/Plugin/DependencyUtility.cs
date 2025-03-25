using Greg.Xrm.Command.Services.Output;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Services.Plugin
{
    public static class DependencyUtility
    {
        public static async Task<Dictionary<string, NuGetVersion>> GetDeltaForPackage(
            PackageIdentity baselinePackage, PackageIdentity targetPackage,
            DependencyInfoResource dependencyInfoResource,
            NuGetFramework framework, SourceCacheContext cacheContext, ILogger logger, CancellationToken cancellationToken)
        {
            var baselineDependencies = new Dictionary<string, NuGetVersion>();
            var resolvedPackageDependencies = new Dictionary<string, NuGetVersion>();
            var result = new Dictionary<string, NuGetVersion>();

            // Start resolving dependencies recursively
            await DependencyUtility.ResolveDependenciesRecursively(
                baselinePackage,
                dependencyInfoResource,
                baselineDependencies,
                framework, cacheContext, logger,  cancellationToken);


            resolvedPackageDependencies.AddRange(baselineDependencies);

            // Start resolving dependencies recursively
            await DependencyUtility.ResolveDependenciesRecursively(
                targetPackage,
                dependencyInfoResource,
                resolvedPackageDependencies,
                framework, cacheContext, logger,  cancellationToken);

            result.AddRange(resolvedPackageDependencies.Where(r => !baselineDependencies.Any(c => c.Key == r.Key && c.Value >= r.Value)));
            return result;
        }

        public static async Task ResolveDependenciesRecursively(
            PackageIdentity package,
            DependencyInfoResource dependencyInfoResource,
            Dictionary<string, NuGetVersion> resolvedDependencies,
            NuGetFramework framework, SourceCacheContext cacheContext, ILogger logger, CancellationToken cancellationToken)
        {
            // Check if the package is already resolved
            if (resolvedDependencies.TryGetValue(package.Id, out NuGetVersion? value))
            {
                var currentVersion = value;

                // If the current version satisfies the new package's version range
                if (currentVersion >= package.Version)
                {
                    return; // No need to update or recheck dependencies
                }

                // If the new version is higher and compatible, update and re-check dependencies
                if (!new VersionRange(currentVersion, true, package.Version, true).Satisfies(package.Version))
                {
                    // If versions are incompatible, throw a conflict exception
                    throw new InvalidOperationException($"Dependency conflict detected: {package.Id} requires {package.Version}, but an incompatible version ({currentVersion}) is already resolved.");
                }

            }

            // Add the package to the resolved dependencies
            resolvedDependencies[package.Id] = package.Version;


            // Fetch the dependency info for the package
            var dependencyInfoForNewPackage = await dependencyInfoResource.ResolvePackage(
                package, framework, cacheContext, logger, cancellationToken) ?? throw new InvalidOperationException($"Package {package.Id} not found on NuGet.");

            // Process each package dependency
            foreach (var dependency in dependencyInfoForNewPackage.Dependencies) // Fixed the issue by using 'Dependencies' instead of 'DependencyGroups'
            {
                var lowestVersion = dependency.VersionRange.MinVersion;
                var dependencyIdentity = new PackageIdentity(dependency.Id, lowestVersion);

                // Recursively resolve the dependency
                await ResolveDependenciesRecursively(
                    dependencyIdentity, dependencyInfoResource, resolvedDependencies, framework, cacheContext, logger, cancellationToken);
            }
        }
    }
}
