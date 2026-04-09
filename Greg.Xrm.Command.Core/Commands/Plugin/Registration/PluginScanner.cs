using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Plugin
{
    /// <summary>
    /// Scans compiled plugin DLLs for [CrmPluginStep], [CrmPluginImage], and [CrmWebhook] attributes.
    /// Uses MetadataLoadContext to load assemblies without executing them.
    /// </summary>
    public static class PluginScanner
    {
        /// <summary>
        /// Scans a directory of plugin DLLs and returns all plugin metadata found.
        /// </summary>
        public static IList<PluginAssemblyMetadata> ScanDirectory(string directoryPath)
        {
            var results = new List<PluginAssemblyMetadata>();
            var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    var assemblyMetadata = ScanAssembly(dllPath);
                    if (assemblyMetadata != null)
                    {
                        results.Add(assemblyMetadata);
                    }
                }
                catch (Exception)
                {
                    // Skip assemblies that can't be loaded (native DLLs, non-.NET, etc.)
                }
            }

            return results;
        }

        /// <summary>
        /// Scans a single plugin DLL and returns the plugin metadata.
        /// Returns null if no plugin attributes are found.
        /// </summary>
        public static PluginAssemblyMetadata? ScanAssembly(string dllPath)
        {
            var pathResolver = new PathAssemblyResolver(new[]
            {
                typeof(object).Assembly.Location,
                typeof(Attribute).Assembly.Location,
                // Add System.Runtime for .NET 8
                Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"),
            });

            // Add the target DLL
            var assemblies = new List<string>
            {
                typeof(object).Assembly.Location,
                typeof(Attribute).Assembly.Location,
            };

            // Get runtime directory for core assemblies
            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (runtimeDir != null)
            {
                assemblies.Add(Path.Combine(runtimeDir, "System.Runtime.dll"));
                assemblies.Add(Path.Combine(runtimeDir, "System.Collections.dll"));
            }

            // Add common Dataverse SDK assemblies that plugins reference
            var dataverseClientPath = FindDataverseClientAssembly();
            if (dataverseClientPath != null)
            {
                assemblies.Add(dataverseClientPath);
            }

            var resolver = new PathAssemblyResolver(assemblies);
            var context = new MetadataLoadContext(resolver);

            try
            {
                var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

                var assemblyMetadata = new PluginAssemblyMetadata
                {
                    AssemblyName = Path.GetFileNameWithoutExtension(dllPath),
                    AssemblyPath = dllPath,
                };

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface) continue;

                    var pluginType = ScanPluginType(type);
                    if (pluginType != null)
                    {
                        assemblyMetadata.PluginTypes.Add(pluginType);
                    }
                }

                return assemblyMetadata.PluginTypes.Count > 0 ? assemblyMetadata : null;
            }
            finally
            {
                context.Unload();
            }
        }

        private static PluginTypeMetadata? ScanPluginType(TypeInfo type)
        {
            var stepAttributes = type.GetCustomAttributesData()
                .Where(attr => attr.AttributeType.Name == "CrmPluginStepAttribute")
                .ToList();

            var imageAttributes = type.GetCustomAttributesData()
                .Where(attr => attr.AttributeType.Name == "CrmPluginImageAttribute")
                .ToList();

            var webhookAttributes = type.GetCustomAttributesData()
                .Where(attr => attr.AttributeType.Name == "CrmWebhookAttribute")
                .ToList();

            if (stepAttributes.Count == 0 && webhookAttributes.Count == 0)
                return null;

            var pluginType = new PluginTypeMetadata
            {
                TypeName = type.FullName!,
                TypeNameWithoutNamespace = type.Name,
                IsWorkflowActivity = type.GetInterfaces().Any(i => i.Name == "IWorkflowActivity"),
            };

            foreach (var stepAttr in stepAttributes)
            {
                var step = new PluginStepMetadata
                {
                    Message = GetNamedArg(stepAttr, "message", 0) ?? "*",
                    Entity = GetNamedArg(stepAttr, "entity", 1) ?? "*",
                    Stage = GetNamedIntArg(stepAttr, "Stage", 40),
                    ExecutionMode = GetNamedIntArg(stepAttr, "ExecutionMode", 0),
                    Deployment = GetNamedIntArg(stepAttr, "Deployment", 0),
                    Rank = GetNamedIntArg(stepAttr, "Rank", 1),
                    FilteringAttributes = GetNamedArrayArg(stepAttr, "FilteringAttributes"),
                    SecureConfiguration = GetNamedArg(stepAttr, "SecureConfiguration"),
                    UnsecureConfiguration = GetNamedArg(stepAttr, "UnsecureConfiguration"),
                    Name = GetNamedArg(stepAttr, "Name"),
                };
                pluginType.Steps.Add(step);
            }

            foreach (var imageAttr in imageAttributes)
            {
                var image = new PluginImageMetadata
                {
                    Name = GetNamedArg(imageAttr, "name", 0) ?? "Image",
                    EntityAlias = GetNamedArg(imageAttr, "entityAlias", 1) ?? "Image",
                    ImageType = GetNamedIntArg(imageAttr, "ImageType", 0),
                    Attributes = GetNamedArg(imageAttr, "Attributes"),
                    Message = GetNamedArg(imageAttr, "Message"),
                };
                pluginType.Images.Add(image);
            }

            foreach (var webhookAttr in webhookAttributes)
            {
                var webhook = new PluginWebhookMetadata
                {
                    Url = GetNamedArg(webhookAttr, "url", 0) ?? "",
                    Method = GetNamedIntArg(webhookAttr, "Method", 0),
                    AuthType = GetNamedIntArg(webhookAttr, "AuthType", 0),
                    AuthHeaderValue = GetNamedArg(webhookAttr, "AuthHeaderValue"),
                    Timeout = GetNamedIntArg(webhookAttr, "Timeout", 30),
                    IncludeEntityImage = GetNamedBoolArg(webhookAttr, "IncludeEntityImage", false),
                };
                pluginType.Webhooks.Add(webhook);
            }

            return pluginType;
        }

        private static string? GetNamedArg(CustomAttributeData attr, string name, int? positionalIndex = null)
        {
            var namedArg = attr.NamedArguments.FirstOrDefault(a => a.MemberName == name);
            if (namedArg.MemberName != null)
            {
                return namedArg.TypedValue.Value?.ToString();
            }

            if (positionalIndex.HasValue && positionalIndex.Value < attr.ConstructorArguments.Count)
            {
                return attr.ConstructorArguments[positionalIndex.Value].Value?.ToString();
            }

            return null;
        }

        private static int GetNamedIntArg(CustomAttributeData attr, string name, int defaultValue)
        {
            var namedArg = attr.NamedArguments.FirstOrDefault(a => a.MemberName == name);
            if (namedArg.MemberName != null && namedArg.TypedValue.Value != null)
            {
                return Convert.ToInt32(namedArg.TypedValue.Value);
            }
            return defaultValue;
        }

        private static bool GetNamedBoolArg(CustomAttributeData attr, string name, bool defaultValue)
        {
            var namedArg = attr.NamedArguments.FirstOrDefault(a => a.MemberName == name);
            if (namedArg.MemberName != null && namedArg.TypedValue.Value != null)
            {
                return Convert.ToBoolean(namedArg.TypedValue.Value);
            }
            return defaultValue;
        }

        private static string[]? GetNamedArrayArg(CustomAttributeData attr, string name)
        {
            var namedArg = attr.NamedArguments.FirstOrDefault(a => a.MemberName == name);
            if (namedArg.MemberName != null && namedArg.TypedValue.Value is IList<CustomAttributeTypedArgument> args)
            {
                return args.Select(a => a.Value?.ToString()!).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            }
            return null;
        }

        private static string? FindDataverseClientAssembly()
        {
            // Look for Microsoft.PowerPlatform.Dataverse.Client in the NuGet cache
            var nugetCache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages", "microsoft.powerplatform.dataverse.client");

            if (Directory.Exists(nugetCache))
            {
                var versions = Directory.GetDirectories(nugetCache).OrderByDescending(d => d).FirstOrDefault();
                if (versions != null)
                {
                    var dllPath = Path.Combine(versions, "lib", "net8.0", "Microsoft.PowerPlatform.Dataverse.Client.dll");
                    if (File.Exists(dllPath))
                        return dllPath;
                }
            }

            return null;
        }
    }

    // === Metadata Models ===

    public class PluginAssemblyMetadata
    {
        public string AssemblyName { get; set; } = "";
        public string AssemblyPath { get; set; } = "";
        public List<PluginTypeMetadata> PluginTypes { get; set; } = new();
    }

    public class PluginTypeMetadata
    {
        public string TypeName { get; set; } = "";
        public string TypeNameWithoutNamespace { get; set; } = "";
        public bool IsWorkflowActivity { get; set; }
        public List<PluginStepMetadata> Steps { get; set; } = new();
        public List<PluginImageMetadata> Images { get; set; } = new();
        public List<PluginWebhookMetadata> Webhooks { get; set; } = new();
    }

    public class PluginStepMetadata
    {
        public string Message { get; set; } = "";
        public string Entity { get; set; } = "";
        public int Stage { get; set; } = 40;
        public int ExecutionMode { get; set; } = 0;
        public int Deployment { get; set; } = 0;
        public int Rank { get; set; } = 1;
        public string[]? FilteringAttributes { get; set; }
        public string? SecureConfiguration { get; set; }
        public string? UnsecureConfiguration { get; set; }
        public string? Name { get; set; }
    }

    public class PluginImageMetadata
    {
        public string Name { get; set; } = "";
        public string EntityAlias { get; set; } = "";
        public int ImageType { get; set; } = 0;
        public string? Attributes { get; set; }
        public string? Message { get; set; }
    }

    public class PluginWebhookMetadata
    {
        public string Url { get; set; } = "";
        public int Method { get; set; } = 0;
        public int AuthType { get; set; } = 0;
        public string? AuthHeaderValue { get; set; }
        public int Timeout { get; set; } = 30;
        public bool IncludeEntityImage { get; set; }
    }
}
