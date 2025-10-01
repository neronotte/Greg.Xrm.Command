using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System.ServiceModel;
using System.Text;

namespace Greg.Xrm.Command.Commands.Plugin
{

	public class PushCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		IPluginPackageReader pluginPackageReader,
		IPluginPackageRepository pluginPackageRepository,
		IPluginAssemblyRepository pluginAssemblyRepository,
		IPluginTypeRepository pluginTypeRepository
	) : ICommandExecutor<PushCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PushCommand command, CancellationToken cancellationToken)
		{
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


			output.WriteLine("Checking solution existence and retrieving publisher prefix");
			var solution = await solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
			if (solution == null)
			{
				return CommandResult.Fail("Invalid solution name: " + currentSolutionName);
			}
			if (solution.ismanaged)
			{
				return CommandResult.Fail("The provided solution is managed. You must specify an unmanaged solution.");
			}



			/* 1. Detect the type of package (dll or nupkg)
			 * 2. If nupkg
			 *  a. Read the plugin id and version. 
			 *  b. Check if the plugin with the same id exists
			 *		1. If exists, update it
			 *		2. If not, create it
			 *	c. after creation, retrieve and show the assemblies associated with the package
			 * 3. If dll
			 *	a. Check if the assembly with the same name exists
			 *		1. If exists, update it
			 *		2. If not, create it
			 *  b. Create or update the plugin types (you have to do it because, when you update an assembly, the plugin types are not created automatically. This happens automatically if you use plugin packages)
			 */


			try
			{
				if (!File.Exists(command.Path))
				{
					return CommandResult.Fail($"File {command.Path} does not exist");
				}
				var fileInfo = new FileInfo(command.Path);
				if (string.Equals(fileInfo.Extension, ".nupkg", StringComparison.OrdinalIgnoreCase))
				{
					var result = await ManagePackageRegistrationAsync(command, crm, solution, cancellationToken);
					return result;
				}

				if (string.Equals(fileInfo.Extension, ".dll", StringComparison.OrdinalIgnoreCase))
				{
					var result = await ManageAssemblyRegistrationAsync(command, crm, solution, cancellationToken);
					return result;
				}

				return CommandResult.Fail("File type not supported");
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);

				var sb = new StringBuilder();
				sb.Append("Exception of type FaultException<OrganizationServiceFault> occurred. ");
				if (!string.IsNullOrWhiteSpace(ex.Message))
				{
					sb.AppendLine().Append("Message: ").Append(ex.Message).Append(". ");
					sb.AppendLine().Append("Details: ").Append(JsonConvert.SerializeObject(ex)).Append(". ");
				}
				if (ex.InnerException != null)
				{
					sb.AppendLine()
						.Append("Inner exception of type ")
						.Append(ex.InnerException.GetType().FullName)
						.Append(": ")
						.Append(ex.InnerException.Message)
						.Append(". ");
				}

				return CommandResult.Fail(sb.ToString(), ex);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);

				var sb = new StringBuilder();
				sb.Append("Exception of type ").Append(ex.GetType().FullName).Append(" occurred. ");
				if (!string.IsNullOrWhiteSpace(ex.Message))
				{
					sb.AppendLine().Append("Message: ").Append(ex.Message).Append(". ");
				}
				if (ex.InnerException != null)
				{
					sb.AppendLine()
						.Append("Inner exception of type ")
						.Append(ex.InnerException.GetType().FullName)
						.Append(": ")
						.Append(ex.InnerException.Message)
						.Append(". ");
				}


				return CommandResult.Fail(sb.ToString(), ex);
			}
		}

		private async Task<CommandResult> ManagePackageRegistrationAsync(PushCommand command, IOrganizationServiceAsync2 crm, Model.Solution solution, CancellationToken cancellationToken)
		{
			output.Write($"Reading plugin package from {command.Path}...");
			var packageData = pluginPackageReader.ReadPackageFile(command.Path);
			if (packageData.HasError)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Failed to read plugin package from {command.Path}: {packageData.ErrorMessage}");
			}
			output.WriteLine("Done", ConsoleColor.Green);


			var packageId = $"{solution.PublisherCustomizationPrefix}_{packageData.PackageId}";

			output.Write($"Checking if package {packageData.PackageId} exists...");
			var pluginPackage = await pluginPackageRepository.GetByIdAsync(crm, packageId, cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);

			if (pluginPackage == null)
			{
				output.Write($"Creating package {packageData.PackageId} ({packageData.PackageVersion})...");
				pluginPackage = new PluginPackage
				{
					name = packageId,
					uniquename = packageId,
					version = packageData.PackageVersion!,
					content = packageData.Content!,
					solutionid = solution.ToEntityReference()
				};
			}
			else
			{
				if (pluginPackage.ismanaged)
				{
					return CommandResult.Fail($"The package {packageData.PackageId} is managed. You cannot update a managed package.");
				}

				output.Write($"Updating package {packageData.PackageId} ({packageData.PackageVersion})...");
				pluginPackage.content = packageData.Content!;
				pluginPackage.version = packageData.PackageVersion!;
			}
			await pluginPackage.SaveOrUpdateAsync(crm);
			output.WriteLine("Done", ConsoleColor.Green);


			output.Write($"Retrieving assemblies associated with package {packageData.PackageId}...");
			output.WriteLine("Done", ConsoleColor.Green);
			var pluginAssemblyList = await pluginAssemblyRepository.GetByPackageIdAsync(crm, pluginPackage.Id, cancellationToken);
			if (pluginAssemblyList.Length == 0)
			{
				output.WriteLine($"No assemblies found for package {packageData.PackageId}", ConsoleColor.Yellow);
			}
			else
			{
				output.WriteLine($"Assemblies associated with package {packageData.PackageId}:", ConsoleColor.Cyan);
				foreach (var assembly in pluginAssemblyList)
				{
					output.WriteLine($"- {assembly.name} (Version: {assembly.version}, Id: {assembly.Id})");
				}
			}

			return CommandResult.Success();
		}







		private async Task<CommandResult> ManageAssemblyRegistrationAsync(PushCommand command, IOrganizationServiceAsync2 crm, Model.Solution solution, CancellationToken cancellationToken)
		{
			output.Write($"Reading plugin package from {command.Path}...");
			var packageData = await pluginPackageReader.ReadAssemblyFileAsync(command.Path);
			if (packageData.HasError)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Failed to read plugin assembly from {command.Path}: {packageData.ErrorMessage}");
			}
			output.WriteLine("Done", ConsoleColor.Green);

			var data = packageData.AssemblyProperties!;




			output.Write($"Checking if plugin assembly {data.Name} exists...");
			var pluginAssembly = await pluginAssemblyRepository.GetByNameAsync(crm, data.Name, cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);

			bool isNew = true;
			if (pluginAssembly == null)
			{
				output.Write($"Creating assembly {data.Name} ({data.Version})...");
				pluginAssembly = new PluginAssembly
				{
					name = data.Name,
					version = data.Version.ToString(),
					culture = data.Culture ?? string.Empty,
					publickeytoken = data.PublicKeyToken,
					ismanaged = false,
					content = packageData.Content!,
					solutionid = solution.ToEntityReference()
				};
			}
			else
			{
				if (pluginAssembly.packageid != null)
				{
					return CommandResult.Fail($"The assembly {data.Name} is associated with package {pluginAssembly.packageid.Name} (id:{pluginAssembly.packageid.Id}). You must update the package.");
				}
				if (pluginAssembly.ismanaged)
				{
					return CommandResult.Fail($"The assembly {data.Name} is managed. You cannot update a managed assembly.");
				}

				isNew = false;

				output.Write($"Updating assembly {data.Name} ({data.Version})...");
				pluginAssembly.content = packageData.Content!;
				pluginAssembly.version = data.Version.ToString();
			}

			try
			{
				await pluginAssembly.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message);
			}




			if (isNew)
			{
				try
				{
					output.Write($"Adding assembly {data.Name} ({data.Version}) to solution {solution.uniquename}...");

					var request = new AddSolutionComponentRequest
					{
						SolutionUniqueName = solution.uniquename,
						ComponentId = pluginAssembly.Id,
						ComponentType = (int)ComponentType.PluginAssembly
					};

					await crm.ExecuteAsync(request);
					output.WriteLine("Done", ConsoleColor.Green);

				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail(ex.Message);
				}
			}






			// now I have to check the plugin types and, eventually, register them manually

			PluginType[] existingTypes = [];
			if (!isNew)
			{
				output.Write($"Retrieving plugin types associated with assembly {data.Name}...");
				existingTypes = await pluginTypeRepository.GetByAssemblyId(crm, pluginAssembly.Id, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				output.WriteLine($"Found {existingTypes.Length} types.");
			}

			foreach (var name in packageData.PluginTypes)
			{
				var pluginType = existingTypes.FirstOrDefault(x => string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));
				if (pluginType == null)
				{
					output.Write($"Creating plugin type {name}...");
					pluginType = new PluginType
					{
						name = name,
						typename = name,
						culture = pluginAssembly.culture,
						ismanaged = false,
						isworkflowactivity = false,
						//plugintypeexportkey = type.PluginTypeExportKey ?? string.Empty,
						publickeytoken = pluginAssembly.publickeytoken,
						version = pluginAssembly.version,
						pluginassemblyid = pluginAssembly.ToEntityReference()
					};
				}
				else
				{
					output.Write($"Updating plugin type {name}...");
					pluginType.publickeytoken = pluginAssembly.publickeytoken;
					pluginType.version = pluginAssembly.version;
				}

				try
				{
					await pluginType.SaveOrUpdateAsync(crm);
					output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					output.WriteLine(ex.Message, ConsoleColor.Red);
				}
			}




			return CommandResult.Success();
		}
	}
}
