using System.ServiceModel;
using System.Text;
using System.Xml.Linq;
using Greg.Xrm.Command.Commands.Settings.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
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
		ISettingDefinitionRepository settingDefinitionRepository,
		IAppSettingRepository appSettingRepository,
		IOrganizationSettingRepository organizationSettingRepository,
		IPublishXmlBuilder publishXmlBuilder,
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
				var webResourceList = await webResourceRepository.GetByNameAsync(crm, [command.WebResourceUniqueName], false);

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
			SettingDefinition? settingDefinition = null;
			try
			{
				settingDefinition = await settingDefinitionRepository.GetByUniqueNameAsync(crm, CustomThemeDefinitionSettingName);
				if (settingDefinition == null)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					return CommandResult.Fail($"Setting definition '{CustomThemeDefinitionSettingName}' was not found.");
				}

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
				currentThemeWebResourceName = await GetThemeWebResourceNameFromSettingAsync(crm, appSettingRepository, organizationSettingRepository, settingDefinition, appContext?.Id);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			WebResource themeWebResource;
			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
			}
			if (string.IsNullOrWhiteSpace(currentThemeWebResourceName))
			{
				if (string.IsNullOrWhiteSpace(currentSolutionName))
				{
					return CommandResult.Fail("No solution name provided and no current default solution found. Please set a default solution or use --solution.");
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
					}

					themeWebResource = await UpsertThemeWebResourceAsync(crm, themeWebResourceName, themeXml);
					await AddThemeToSolutionAsync(crm, solution, themeWebResource);
					await SaveSettingValueAsync(crm, currentSolutionName, appContext?.UniqueName, themeWebResource.name);

					if (!string.IsNullOrWhiteSpace(command.LocalThemeFile))
					{
						await SaveLocalThemeContentAsync(command.LocalThemeFile, themeXml, cancellationToken);
					}
					else
					{
						await TrySaveLocalThemeFileAsync(solution.PublisherCustomizationPrefix, themeXml, cancellationToken);
					}
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
				publishXmlBuilder.AddWebResource(themeWebResource.Id);
				var publish = publishXmlBuilder.Build();
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
				new XElement("CustomTheme",
					new XAttribute("logoWebResource", logoWebResourceName)
				)
			);
			return document.ToString();
		}

		private static string UpdateThemeLogo(string themeXml, string logoWebResourceName)
		{
			var document = XDocument.Parse(themeXml, LoadOptions.PreserveWhitespace);
			if (document.Root == null)
			{
				throw new InvalidOperationException("Invalid theme xml: root element not found.");
			}
			if (!"CustomTheme".Equals(document.Root.Name.LocalName, StringComparison.OrdinalIgnoreCase)
				&& !"AppHeaderColors".Equals(document.Root.Name.LocalName, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Invalid theme xml: root element should be 'CustomTheme' or 'AppHeaderColors'.");
			}

			if ("CustomTheme".Equals(document.Root.Name.LocalName, StringComparison.OrdinalIgnoreCase))
			{
				document.Root.SetAttributeValue("logoWebResource", logoWebResourceName);
				return document.ToString();
			}

			//if ("AppHeaderColors".Equals(document.Root.Name.LocalName, StringComparison.OrdinalIgnoreCase))
			//{
				var document2 = new XDocument(
					new XElement("CustomTheme",
						new XAttribute("logoWebResource", logoWebResourceName)
					)
				);
				document2.Root!.Add(document.Root);
				return document2.ToString();
			//}
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

		private async Task<string?> GetThemeWebResourceNameFromSettingAsync(
			IOrganizationServiceAsync2 crm,
			IAppSettingRepository appSettingRepository,
			IOrganizationSettingRepository organizationSettingRepository,
			SettingDefinition settingDefinition,
			Guid? appId)
		{
			if (appId.HasValue)
			{
				var appSetting = await appSettingRepository.GetByAppAndDefinitionAsync(crm, settingDefinition, appId.Value);
				return appSetting?.value;
			}
			else
			{
				var orgSettingList = await organizationSettingRepository.GetByDefinitionsAsync(crm, [settingDefinition]);
				var orgSetting = orgSettingList.FirstOrDefault();
				return orgSetting?.value;
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
				output.WriteLine("No .wr.pacx file found in the folder tree, skipping local theme file save.", ConsoleColor.Yellow);
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
