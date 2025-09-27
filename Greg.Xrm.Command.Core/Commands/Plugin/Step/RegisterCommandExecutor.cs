using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.Text;

namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	public class RegisterCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		ISdkMessageRepository sdkMessageRepository,
		IPluginTypeRepository pluginTypeRepository
	): ICommandExecutor<RegisterCommand>
	{
		public async Task<CommandResult> ExecuteAsync(RegisterCommand command, CancellationToken cancellationToken)
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






			output.Write($"Validating message name ({command.MessageName})...");
			var sdkMessage = await sdkMessageRepository.GetByNameAsync(crm, command.MessageName);
			if (sdkMessage == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Invalid message name: " + command.MessageName);
			}
			output.WriteLine("Done", ConsoleColor.Green);


			SdkMessageFilter? sdkMessageFilter = null;
			if (string.IsNullOrWhiteSpace(command.PrimaryEntityName))
			{
				if (sdkMessage.Filters.Length > 0)
				{
					var sb = new StringBuilder();
					sb.Append("Message '").Append(sdkMessage.name).Append("' cannot be registered without indicating a table.");
					if (sdkMessage.Filters.Length > 0)
					{
						sb.Append(" Valid tables for this message are: ").Append(sdkMessage.Filters.Select(x => x.primaryobjecttypecode).Join(", "));
					}
					else
					{
						sb.Append(" You must not specify any table name.");
					}

					return CommandResult.Fail(sb.ToString());
				}
			}
			else
			{
				sdkMessageFilter = sdkMessage.GetFilter(command.PrimaryEntityName);
				if (sdkMessageFilter == null)
				{
					var sb = new StringBuilder();
					sb.Append("Message '").Append(sdkMessage.name).Append("' cannot be registered for table '").Append(command.PrimaryEntityName).Append("'.");
					if (sdkMessage.Filters.Length > 0)
					{
						sb.Append(" Valid tables for this message are: ").Append(sdkMessage.Filters.Select(x => x.primaryobjecttypecode).Join(", "));
					}
					else
					{
						sb.Append(" You must not specify any table name.");
					}

					return CommandResult.Fail(sb.ToString());
				}
			}

			var filteringAttributes = command.FilteringAttributes?
					.Split(',')
					.Select(a => a.Trim())
					.Where(a => !string.IsNullOrWhiteSpace(a))
					.OrderBy(x => x)
					.ToArray() ?? [];


			output.Write($"Retrieving plugin type ({command.PluginTypeName})...");
			var pluginTypeList = await pluginTypeRepository.FuzzySearchAsync(crm, command.PluginTypeName, cancellationToken);
			if (pluginTypeList == null || pluginTypeList.Length == 0)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Plugin type not found: " + command.PluginTypeName);
			}
			if (pluginTypeList.Length > 1)
			{
				output.WriteLine("Failed", ConsoleColor.Red);

				output.WriteLine("Multiple plugin types found matching the provided name. Please be more specific. Matching plugin types:", ConsoleColor.Yellow);
				foreach (var pt in pluginTypeList)
				{
					output.WriteLine($"- {pt.name} (Id: {pt.Id}, Assembly: {pt.pluginassemblyid?.Name})", ConsoleColor.Yellow);
				}

				return CommandResult.Fail("Multiple plugin types found matching the provided name. Please be more specific.");
			}
			output.WriteLine("Done", ConsoleColor.Green);

			var pluginType = pluginTypeList[0];
			if (!string.Equals(command.PluginTypeName, pluginType.name, StringComparison.OrdinalIgnoreCase))
			{
				output.WriteLine($"Actual plugin type: '{pluginType.name}' (Id: {pluginType.Id}, Assembly: {pluginType.pluginassemblyid?.Name})", ConsoleColor.DarkGray);
			}


			Entity? step = null;
			try
			{
				output.WriteLine($"Registering plugin step...");
				var toolkit = new PluginRegistrationToolkit(crm, output);
				step = toolkit.RegisterPluginStep(
					pluginType,
					sdkMessage,
					sdkMessageFilter,
					command.Stage,
					command.Mode,
					command.Deployment,
					filteringAttributes,
					command.ExecutionOrder,
					command.Description,
					command.UnsecureConfiguration,
					command.SecureConfiguration,
					command.PreImage,
					command.PostImage,
					command.PreImageName,
					command.PostImageName);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message);
			}

			


			try
			{
				output.Write($"Adding plugin step to solution {solution.uniquename}...");
				var request = new AddSolutionComponentRequest
				{
					SolutionUniqueName = solution.uniquename,
					ComponentType = (int)ComponentType.SDKMessageProcessingStep,
					ComponentId = step.Id
				};

				await crm.ExecuteAsync(request);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
			}

			var result = CommandResult.Success();
			result["StepId"] = step.Id;
			return result;
		}
	}
}
