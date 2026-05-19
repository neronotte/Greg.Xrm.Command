using System.ServiceModel;
using System.Text;
using System.Xml.Linq;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class SetEnvImageCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWebResourceRepository webResourceRepository,
		ISolutionRepository solutionRepository) : ICommandExecutor<SetEnvImageCommand>
	{
		private const string CustomThemeDefinitionSettingName = "CustomThemeDefinition";
		private const string ThemeFileName = "theme.xml";

		public async Task<CommandResult> ExecuteAsync(SetEnvImageCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			output.Write($"Retrieving the webresource called <{command.WebResourceUniqueName}>...");
			WebResource logo;
			try
			{
				var webResourceList = await webResourceRepository.GetByNameAsync(crm, new[] { command.WebResourceUniqueName }, false);

				if (webResourceList.Count == 0)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					return CommandResult.Fail("The webresource with the specified name does not exists");
				}

				logo = webResourceList[0];
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			if (logo.webresourcetype?.Value != (int)WebResourceType.ImagePng &&
				logo.webresourcetype?.Value != (int)WebResourceType.ImageGif &&
				logo.webresourcetype?.Value != (int)WebResourceType.ImageJpg)
			{
				return CommandResult.Fail($"The webresource type {logo.GetFormattedType()} is not supported for the logo");
			}


			AppContext? appContext = null;
			try
			{
				appContext = await ResolveAppContextAsync(crm, command);
				if (appContext == null && (!string.IsNullOrWhiteSpace(command.AppId) || !string.IsNullOrWhiteSpace(command.AppName)))
				{
					return CommandResult.Fail("Unable to find the target app specified by --appId or --appName.");
				}
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}


			output.Write("Retrieving CustomThemeDefinition setting definition...");
			Guid settingDefinitionId;
			try
			{
				var query = new QueryExpression("settingdefinition");
				query.ColumnSet.AddColumn("settingdefinitionid");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, CustomThemeDefinitionSettingName);
				query.TopCount = 1;
				var result = await crm.RetrieveMultipleAsync(query);
				var settingDefinition = result.Entities.FirstOrDefault();
				if (settingDefinition == null)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					return CommandResult.Fail($"Setting definition '{CustomThemeDefinitionSettingName}' was not found.");
				}

				settingDefinitionId = settingDefinition.Id;
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			output.Write("Retrieving current CustomThemeDefinition value...");
			string? currentThemeWebResourceName;
			try
			{
				currentThemeWebResourceName = await GetThemeWebResourceNameFromSettingAsync(crm, settingDefinitionId, appContext?.Id);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			WebResource themeWebResource;
			var currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
			if (string.IsNullOrWhiteSpace(currentThemeWebResourceName))
			{
				if (string.IsNullOrWhiteSpace(currentSolutionName))
				{
					return CommandResult.Fail("No current default solution found. Please set a default solution first.");
				}

				var solution = await solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
				if (solution == null || string.IsNullOrWhiteSpace(solution.PublisherCustomizationPrefix))
				{
					return CommandResult.Fail($"Unable to retrieve publisher prefix from solution '{currentSolutionName}'.");
				}

				string themeXml;
				string themeWebResourceName;
				if (!string.IsNullOrWhiteSpace(command.LocalThemeFile))
				{
					if (!File.Exists(command.LocalThemeFile))
					{
						return CommandResult.Fail($"Local theme file '{command.LocalThemeFile}' does not exist.");
					}

					themeXml = await File.ReadAllTextAsync(command.LocalThemeFile, cancellationToken);
					themeXml = UpdateThemeLogo(themeXml, command.WebResourceUniqueName);
					await SaveLocalThemeContentAsync(command.LocalThemeFile, themeXml, cancellationToken);

					themeWebResourceName = TryResolveWebResourceNameFromFile(command.LocalThemeFile, solution.PublisherCustomizationPrefix) ?? string.Empty;
					if (string.IsNullOrWhiteSpace(themeWebResourceName))
					{
						return CommandResult.Fail($"Unable to infer webresource unique name from '{command.LocalThemeFile}'.");
					}
				}
				else
				{
					var publisherFolderName = GetPublisherFolderName(solution.PublisherCustomizationPrefix);
					themeWebResourceName = $"{publisherFolderName}/themes/{ThemeFileName}";
					themeXml = CreateNewThemeXml(command.WebResourceUniqueName);
					await TrySaveLocalThemeFileAsync(solution.PublisherCustomizationPrefix, themeXml, cancellationToken);
				}

				themeWebResource = await UpsertThemeWebResourceAsync(crm, themeWebResourceName, themeXml);
				await AddThemeToSolutionAsync(crm, solution, themeWebResource);
				await SaveSettingValueAsync(crm, currentSolutionName, appContext?.UniqueName, themeWebResource.name);
			}
			else
			{
				output.Write($"Retrieving the current theme webresource <{currentThemeWebResourceName}>...");
				var themeWebResources = await webResourceRepository.GetByNameAsync(crm, [currentThemeWebResourceName], true);
				if (themeWebResources.Count == 0)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					return CommandResult.Fail($"Unable to find theme webresource '{currentThemeWebResourceName}'.");
				}

				themeWebResource = themeWebResources[0];
				output.WriteLine("Done", ConsoleColor.Green);

				var currentThemeXml = DecodeThemeWebResourceContent(themeWebResource);
				var updatedThemeXml = UpdateThemeLogo(currentThemeXml, command.WebResourceUniqueName);
				themeWebResource.content = Convert.ToBase64String(Encoding.UTF8.GetBytes(updatedThemeXml));
				await themeWebResource.SaveOrUpdateAsync(crm);

				if (!string.IsNullOrWhiteSpace(command.LocalThemeFile))
				{
					await SaveLocalThemeContentAsync(command.LocalThemeFile, updatedThemeXml, cancellationToken);
				}
			}

			output.Write("Publishing updated webresource...");
			try
			{
				var publish = new PublishXmlRequest
				{
					ParameterXml = $"<importexportxml><webresources><webresource>{themeWebResource.Id}</webresource></webresources></importexportxml>"
				};
				await crm.ExecuteAsync(publish);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			return CommandResult.Success();
		}

		private static string DecodeThemeWebResourceContent(WebResource webResource)
		{
			if (string.IsNullOrWhiteSpace(webResource.content))
			{
				return CreateNewThemeXml(string.Empty);
			}

			var bytes = Convert.FromBase64String(webResource.content);
			return Encoding.UTF8.GetString(bytes);
		}

		private static string CreateNewThemeXml(string logoWebResourceName)
		{
			var document = new XDocument(
				new XElement("CustomThemeDefinition",
					new XElement("Theme",
						new XElement("Logo", BuildLogoValue(logoWebResourceName)))));
			return document.ToString();
		}

		private static string UpdateThemeLogo(string themeXml, string logoWebResourceName)
		{
			var document = XDocument.Parse(themeXml, LoadOptions.PreserveWhitespace);
			var logoNode = document.Descendants()
				.FirstOrDefault(x =>
					string.Equals(x.Name.LocalName, "logo", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(x.Name.LocalName, "appheaderlogo", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(x.Name.LocalName, "applogo", StringComparison.OrdinalIgnoreCase))
				?? document.Descendants()
					.FirstOrDefault(x => x.Name.LocalName.Contains("logo", StringComparison.OrdinalIgnoreCase) && !x.HasElements);

			if (logoNode == null)
			{
				var parent = document.Descendants().FirstOrDefault(x => string.Equals(x.Name.LocalName, "Theme", StringComparison.OrdinalIgnoreCase))
					?? document.Root
					?? throw new InvalidOperationException("Invalid theme xml: root element not found.");

				logoNode = new XElement("Logo");
				parent.Add(logoNode);
			}

			logoNode.Value = BuildLogoValue(logoWebResourceName);
			return document.ToString();
		}

		private static string BuildLogoValue(string webResourceName)
		{
			if (string.IsNullOrWhiteSpace(webResourceName))
			{
				return "webresource:";
			}

			return $"webresource:{webResourceName}";
		}

		private static string? NormalizeThemeWebResourceName(string? settingValue)
		{
			if (string.IsNullOrWhiteSpace(settingValue))
			{
				return null;
			}

			settingValue = settingValue.Trim();
			return settingValue.StartsWith("webresource:", StringComparison.OrdinalIgnoreCase)
				? settingValue["webresource:".Length..].Trim()
				: settingValue;
		}

		private async Task<AppContext?> ResolveAppContextAsync(IOrganizationServiceAsync2 crm, SetEnvImageCommand command)
		{
			if (string.IsNullOrWhiteSpace(command.AppId) && string.IsNullOrWhiteSpace(command.AppName))
			{
				return null;
			}

			output.Write("Resolving target app...");

			var query = new QueryExpression("appmodule");
			query.ColumnSet.AddColumns("appmoduleid", "uniquename", "name");
			query.TopCount = 1;
			if (!string.IsNullOrWhiteSpace(command.AppId))
			{
				query.Criteria.AddCondition("appmoduleid", ConditionOperator.Equal, Guid.Parse(command.AppId));
			}
			else
			{
				var appName = command.AppName!;
				var filter = new FilterExpression(LogicalOperator.Or);
				filter.AddCondition("name", ConditionOperator.Equal, appName);
				filter.AddCondition("uniquename", ConditionOperator.Equal, appName);
				query.Criteria.AddFilter(filter);
			}

			var result = await crm.RetrieveMultipleAsync(query);
			var app = result.Entities.FirstOrDefault();
			if (app == null)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return null;
			}

			var uniqueName = app.GetAttributeValue<string>("uniquename");
			if (string.IsNullOrWhiteSpace(uniqueName))
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return null;
			}

			output.WriteLine("Done", ConsoleColor.Green);
			return new AppContext(app.Id, uniqueName);
		}

		private async Task<string?> GetThemeWebResourceNameFromSettingAsync(IOrganizationServiceAsync2 crm, Guid settingDefinitionId, Guid? appId)
		{
			if (appId.HasValue)
			{
				var appQuery = new QueryExpression("appsetting");
				appQuery.ColumnSet.AddColumn("value");
				appQuery.Criteria.AddCondition("settingdefinitionid", ConditionOperator.Equal, settingDefinitionId);
				appQuery.Criteria.AddCondition("parentappmoduleid", ConditionOperator.Equal, appId.Value);
				appQuery.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(appQuery);
				return NormalizeThemeWebResourceName(result.Entities.FirstOrDefault()?.GetAttributeValue<string>("value"));
			}
			else
			{
				var orgQuery = new QueryExpression("organizationsetting");
				orgQuery.ColumnSet.AddColumn("value");
				orgQuery.Criteria.AddCondition("settingdefinitionid", ConditionOperator.Equal, settingDefinitionId);
				orgQuery.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(orgQuery);
				return NormalizeThemeWebResourceName(result.Entities.FirstOrDefault()?.GetAttributeValue<string>("value"));
			}
		}

		private async Task<WebResource> UpsertThemeWebResourceAsync(IOrganizationServiceAsync2 crm, string themeWebResourceName, string themeXml)
		{
			output.Write($"Saving theme webresource <{themeWebResourceName}>...");
			var webResources = await webResourceRepository.GetByNameAsync(crm, [themeWebResourceName], true);
			var themeWebResource = webResources.FirstOrDefault()
				?? new WebResource
				{
					name = themeWebResourceName,
					displayname = themeWebResourceName,
					webresourcetype = new OptionSetValue((int)WebResourceType.Data)
				};

			themeWebResource.content = Convert.ToBase64String(Encoding.UTF8.GetBytes(themeXml));
			await themeWebResource.SaveOrUpdateAsync(crm);
			output.WriteLine("Done", ConsoleColor.Green);
			return themeWebResource;
		}

		private async Task AddThemeToSolutionAsync(IOrganizationServiceAsync2 crm, Greg.Xrm.Command.Model.Solution solution, WebResource webResource)
		{
			output.Write("Adding theme webresource to solution...");
			await solution.UpsertSolutionComponentsAsync(crm, [webResource], ComponentType.WebResource);
			output.WriteLine("Done", ConsoleColor.Green);
		}

		private async Task SaveSettingValueAsync(IOrganizationServiceAsync2 crm, string solutionName, string? appUniqueName, string themeWebResourceName)
		{
			output.Write("Saving CustomThemeDefinition setting value...");
			var request = new OrganizationRequest("SaveSettingValue");
			request["SettingName"] = CustomThemeDefinitionSettingName;
			request["Value"] = themeWebResourceName;
			request["SolutionUniqueName"] = solutionName;
			if (!string.IsNullOrWhiteSpace(appUniqueName))
			{
				request["AppUniqueName"] = appUniqueName;
			}

			await crm.ExecuteAsync(request);
			output.WriteLine("Done", ConsoleColor.Green);
		}

		private static async Task SaveLocalThemeContentAsync(string filePath, string content, CancellationToken cancellationToken)
		{
			var fullPath = Path.GetFullPath(filePath);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
			await File.WriteAllTextAsync(fullPath, content, cancellationToken);
		}

		private async Task TrySaveLocalThemeFileAsync(string publisherPrefix, string content, CancellationToken cancellationToken)
		{
			var root = FolderTree.RecurseBackFolderContainingFile(".wr.pacx");
			if (root == null)
			{
				return;
			}

			var filePath = Path.Combine(root.FullName, GetPublisherFolderName(publisherPrefix), "themes", ThemeFileName);
			Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
			await SaveLocalThemeContentAsync(filePath, content, cancellationToken);
		}

		private static string? TryResolveWebResourceNameFromFile(string filePath, string publisherPrefix)
		{
			var fullPath = Path.GetFullPath(filePath);
			var normalized = fullPath.Replace('\\', '/');
			var prefixSegment = $"{GetPublisherFolderName(publisherPrefix)}/";

			var root = FolderTree.RecurseBackFolderContainingFile(".wr.pacx", new DirectoryInfo(Path.GetDirectoryName(fullPath)!));
			if (root != null)
			{
				var relative = Path.GetRelativePath(root.FullName, fullPath).Replace('\\', '/');
				if (!string.IsNullOrWhiteSpace(relative))
				{
					return relative;
				}
			}

			var index = normalized.IndexOf(prefixSegment, StringComparison.OrdinalIgnoreCase);
			if (index >= 0)
			{
				return normalized[index..];
			}

			return null;
		}

		private static string GetPublisherFolderName(string publisherPrefix)
		{
			return publisherPrefix.Trim().TrimEnd('_') + "_";
		}

		private sealed record AppContext(Guid Id, string UniqueName);
	}
}
