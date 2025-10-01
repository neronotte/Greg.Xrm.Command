using Greg.Xrm.Command.Services.Settings;
using Microsoft.Xrm.Sdk;
using System.Globalization;
using System.IO.Packaging;
using System.Reflection;
using System.Xml;

namespace Greg.Xrm.Command.Services.Plugin
{
	public class PluginPackageReader(
		ISettingsRepository settingsRepository
	): IPluginPackageReader
	{




		public PluginPackageReadResult ReadPackageFile(string filePath)
		{
			// Step 1: Validate input parameters
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return PluginPackageReadResult.Error($"File name not provided");
			}
			if (!File.Exists(filePath))
			{
				return PluginPackageReadResult.Error($"File <{filePath}> does not exist");
			}

			string? packageId = null;
			string? packageVersion = null;

			// Step 2: Open the package file (NuGet package is essentially a ZIP file)
			using (var package = Package.Open(filePath, FileMode.Open, FileAccess.Read))
			{
				// Step 3: Locate the .nuspec file within the package
				// The .nuspec file contains package metadata including ID and version
				var part = package.GetParts().FirstOrDefault(part => part.Uri.ToString().EndsWith(".nuspec"));
				if (part == null)
				{
					return PluginPackageReadResult.Error($"File <{filePath}> does not contain a .nuspec file");
				}

				// Step 4: Read and parse the .nuspec XML content
				using var stream = part.GetStream();

				var reader = new XmlTextReader(stream);

				var xmlDocument = new XmlDocument();
				xmlDocument.Load(reader);

				// Step 5: Set up XML namespace manager to handle namespaced XML elements
				var nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
				nsmgr.AddNamespace("ns", xmlDocument.DocumentElement!.NamespaceURI);

				// Step 6: Extract package metadata from the XML document
				var xmlNode = xmlDocument.SelectSingleNode("ns:package/ns:metadata", nsmgr);
				if (xmlNode == null)
				{
					return PluginPackageReadResult.Error($"Package metadata not found: Could not find the package/metadata node in {part.Uri}");
				}

				// Step 7: Extract package ID from metadata
				packageId = xmlNode.SelectSingleNode("ns:id", nsmgr)?.InnerText;
				if (packageId == null)
				{
					return PluginPackageReadResult.Error($"Package metadata not found: Could not find the package/metadata/id node in {part.Uri}");
				}

				// Step 8: Extract package version from metadata
				packageVersion = xmlNode.SelectSingleNode("ns:version", nsmgr)?.InnerText;
				if (packageVersion == null)
				{
					return PluginPackageReadResult.Error($"Package metadata not found: Could not find the package/metadata/version node in {part.Uri}");
				}
			}

			// Step 9: Read the entire package file content and convert to Base64
			// This allows the package content to be stored/transmitted as a string
			using var fileStream = new FileStream(filePath, FileMode.Open);
			using var destination = new MemoryStream();
			fileStream.CopyTo(destination);
			var content = Convert.ToBase64String(destination.ToArray());

			// Step 10: Return successful result with extracted package information
			return PluginPackageReadResult.Success(packageId, packageVersion, content);
		}




		public async Task<PluginAssemblyReadResult> ReadAssemblyFileAsync(string filePath)
		{
			// Step 1: Validate input parameters
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return PluginAssemblyReadResult.Error($"File name not provided");
			}
			if (!File.Exists(filePath))
			{
				return PluginAssemblyReadResult.Error($"File <{filePath}> does not exist");
			}

			AssemblyName? assemblyName;
			string[] pluginTypes;

			try
			{
				var frameworkPath = await settingsRepository.GetAsync<string>(Constants.FrameworkPathKey);
				if (string.IsNullOrWhiteSpace(frameworkPath))
				{
					frameworkPath = Constants.DefaultFrameworkPath;
				}
				if (!Directory.Exists(frameworkPath))
				{
					return PluginAssemblyReadResult.Error($"Cannot find .NET Framework 4.6.2. reference assemblies at <{frameworkPath}>.");
				}

				var currentAssemblyFile = new FileInfo(Assembly.GetExecutingAssembly().Location);

				var sdkLibrary = Path.Combine(currentAssemblyFile.DirectoryName!, "Microsoft.Xrm.Sdk.dll");
				if (!File.Exists(sdkLibrary))
				{
					return PluginAssemblyReadResult.Error($"Cannot find Microsoft.Xrm.Sdk.dll at <{sdkLibrary}>.");
				}

				var referenceAssemblies = new List<string> { filePath };
				referenceAssemblies.AddRange(Directory.GetFiles(frameworkPath, "*.dll"));
				referenceAssemblies.Add(sdkLibrary);

				var resolver = new PathAssemblyResolver(referenceAssemblies);
				using var mlc = new MetadataLoadContext(resolver, coreAssemblyName: "mscorlib");

				var assembly = mlc.LoadFromAssemblyPath(filePath);
				assemblyName = assembly.GetName();

				var allTypes = assembly.GetTypes();

				// Followed the guide at: https://learn.microsoft.com/en-us/dotnet/standard/assembly/inspect-contents-using-metadataloadcontext
				var sdkAssembly = mlc.LoadFromAssemblyPath(sdkLibrary);
				var pluginInterfaceType = sdkAssembly.GetType(typeof(IPlugin).FullName!)!;

				pluginTypes = allTypes
					.Where(t => t.IsClass && !t.IsAbstract && pluginInterfaceType.IsAssignableFrom(t))
					.Select(x => x.FullName!)
					.ToArray();
			}
			catch(Exception ex)
			{
				return PluginAssemblyReadResult.Error($"Error reading assembly file <{filePath}>: {ex.Message}");
			}


			var assemblyProperties = RetrieveAssemblyProperties(assemblyName);

			// Step 9: Read the entire package file content and convert to Base64
			// This allows the package content to be stored/transmitted as a string
			using var fileStream = new FileStream(filePath, FileMode.Open);
			using var destination = new MemoryStream();
			fileStream.CopyTo(destination);
			var content = Convert.ToBase64String(destination.ToArray());


			return PluginAssemblyReadResult.Success(assemblyProperties, content, pluginTypes);
		}


		private static AssemblyProperties RetrieveAssemblyProperties(AssemblyName name)
		{
			ArgumentNullException.ThrowIfNull(name);

			var culture = name.CultureInfo!.LCID != CultureInfo.InvariantCulture.LCID ? name.CultureInfo.Name : "neutral";

			byte[] publicKeyToken = name.GetPublicKeyToken() ?? [];
			var tokenString = publicKeyToken.Length == 0 ? null : string.Join(string.Empty, publicKeyToken.Select(b => b.ToString("X2")));


			return new AssemblyProperties(name.Name!, name.Version!, culture, tokenString);
		}
	}
}
