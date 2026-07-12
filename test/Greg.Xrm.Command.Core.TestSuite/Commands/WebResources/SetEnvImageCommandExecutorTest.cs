using System.Text;
using Greg.Xrm.Command.Commands.Settings.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[TestClass]
	public class SetEnvImageCommandExecutorTest
	{
		private static (
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> repoMock,
			Mock<IOrganizationServiceAsync2> crmMock,
			Mock<IWebResourceRepository> webResourceRepositoryMock,
			Mock<ISolutionRepository> solutionRepositoryMock,
			Mock<ISettingDefinitionRepository> settingDefinitionRepositoryMock,
			Mock<IAppSettingRepository> appSettingRepositoryMock,
			Mock<IOrganizationSettingRepository> organizationSettingRepositoryMock,
			Mock<IPublishXmlBuilder> publishXmlBuilderMock)
		CreateMocks()
		{
			var output = new OutputToMemory();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			var repoMock = new Mock<IOrganizationServiceRepository>();
			var webResourceRepositoryMock = new Mock<IWebResourceRepository>();
			var solutionRepositoryMock = new Mock<ISolutionRepository>();
			var settingDefinitionRepositoryMock = new Mock<ISettingDefinitionRepository>();
			var appSettingRepositoryMock = new Mock<IAppSettingRepository>();
			var organizationSettingRepositoryMock = new Mock<IOrganizationSettingRepository>();
			var publishXmlBuilderMock = new Mock<IPublishXmlBuilder>();

			repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(crmMock.Object);
			publishXmlBuilderMock.Setup(p => p.Build()).Returns(new PublishXmlRequest());

			return (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock);
		}

		private static SetEnvImageCommandExecutor CreateExecutor(
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> repoMock,
			Mock<IWebResourceRepository> webResourceRepositoryMock,
			Mock<ISettingDefinitionRepository> settingDefinitionRepositoryMock,
			Mock<IAppSettingRepository> appSettingRepositoryMock,
			Mock<IOrganizationSettingRepository> organizationSettingRepositoryMock,
			Mock<IPublishXmlBuilder> publishXmlBuilderMock,
			Mock<ISolutionRepository> solutionRepositoryMock)
		{
			return new SetEnvImageCommandExecutor(
				output,
				repoMock.Object,
				webResourceRepositoryMock.Object,
				settingDefinitionRepositoryMock.Object,
				appSettingRepositoryMock.Object,
				organizationSettingRepositoryMock.Object,
				publishXmlBuilderMock.Object,
				solutionRepositoryMock.Object);
		}

		#region Logo WebResource Validation Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenLogoWebResourceNotFound()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([]);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "nonexistent_logo.png" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "does not exists");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenWebResourceIsNotAnImage()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var jsWebResource = CreateWebResource("new_script.js", WebResourceType.Script, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([jsWebResource]);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_script.js" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not supported for the logo");
		}

		[TestMethod]
		[DataRow(WebResourceType.ImagePng)]
		[DataRow(WebResourceType.ImageGif)]
		[DataRow(WebResourceType.ImageJpg)]
		public async Task ExecuteAsync_ShouldAcceptValidImageTypes(WebResourceType imageType)
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo", imageType, null);
			var existingThemeXml = "<CustomTheme basePaletteColor=\"#0078D4\" logoWebResource=\"old_logo.png\" />";
			var theme = CreateWebResource("new_/themes/theme.xml", WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)), Guid.NewGuid());

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_logo" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}

		#endregion

		#region App Context Resolution Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenAppIdSpecifiedButAppNotFound()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([logo]);

			crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ReturnsAsync(new EntityCollection());

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png",
					AppId = Guid.NewGuid().ToString()
				},
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unable to find the target app");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenAppNameSpecifiedButAppNotFound()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([logo]);

			crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ReturnsAsync(new EntityCollection());

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png",
					AppName = "NonExistentApp"
				},
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unable to find the target app");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUseAppSetting_WhenAppIdProvided()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var appId = Guid.NewGuid();
			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var existingThemeXml = "<CustomTheme basePaletteColor=\"#0078D4\" logoWebResource=\"old_logo.png\" />";
			var theme = CreateWebResource("new_/themes/theme.xml", WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)), Guid.NewGuid());

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_logo.png"), false))
				.ReturnsAsync([logo]);

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_/themes/theme.xml"), true))
				.ReturnsAsync([theme]);

			var appEntity = new Entity("appmodule") { Id = appId };
			appEntity["uniquename"] = "SalesHub";
			appEntity["name"] = "Sales Hub";
			crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ReturnsAsync(new EntityCollection([appEntity]));

			var settingDef = CreateSettingDefinition(Guid.NewGuid());
			settingDefinitionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
				.ReturnsAsync(settingDef);

			var appSetting = CreateAppSetting("new_/themes/theme.xml");
			appSettingRepositoryMock
				.Setup(r => r.GetByAppAndDefinitionAsync(crmMock.Object, settingDef, appId))
				.ReturnsAsync(appSetting);

			crmMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>())).Returns(Task.CompletedTask);
			crmMock.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>())).ReturnsAsync(new PublishXmlResponse());

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png",
					AppId = appId.ToString()
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			appSettingRepositoryMock.Verify(r => r.GetByAppAndDefinitionAsync(crmMock.Object, settingDef, appId), Times.Once);
			organizationSettingRepositoryMock.Verify(r => r.GetByDefinitionsAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<IReadOnlyList<SettingDefinition>>()), Times.Never);
		}

		#endregion

		#region Setting Definition Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenSettingDefinitionNotFound()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([logo]);

			settingDefinitionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
				.ReturnsAsync((SettingDefinition?)null);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_logo.png" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "CustomThemeDefinition");
			StringAssert.Contains(result.ErrorMessage, "was not found");
		}

		#endregion

		#region No Theme Configured (First-time Setup) Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoThemeAndNoSolutionProvided()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([logo]);

			var settingDefRecord = CreateSettingDefinition(Guid.NewGuid());
			settingDefinitionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
				.ReturnsAsync(settingDefRecord);

			organizationSettingRepositoryMock
				.Setup(r => r.GetByDefinitionsAsync(crmMock.Object, It.IsAny<IReadOnlyList<SettingDefinition>>()))
				.ReturnsAsync(new List<OrganizationSetting>());

			repoMock.Setup(r => r.GetCurrentDefaultSolutionAsync()).ReturnsAsync((string?)null);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_logo.png" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "default solution");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoThemeAndSolutionHasNoPublisherPrefix()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([logo]);

			var settingDefRecord = CreateSettingDefinition(Guid.NewGuid());
			settingDefinitionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
				.ReturnsAsync(settingDefRecord);

			organizationSettingRepositoryMock
				.Setup(r => r.GetByDefinitionsAsync(crmMock.Object, It.IsAny<IReadOnlyList<SettingDefinition>>()))
				.ReturnsAsync(new List<OrganizationSetting>());

			repoMock.Setup(r => r.GetCurrentDefaultSolutionAsync()).ReturnsAsync("MySolution");

			var solutionWithNoPrefix = CreateSolution("MySolution", null);
			solutionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "MySolution"))
				.ReturnsAsync(solutionWithNoPrefix);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_logo.png" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "publisher prefix");
		}

		[TestMethod]
			public async Task ExecuteAsync_ShouldCreateNewTheme_WhenNoThemeExistsAndColorProvided()
			{
				var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

				var newThemeId = Guid.NewGuid();
				var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
				webResourceRepositoryMock
					.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_logo.png"), false))
					.ReturnsAsync([logo]);

				// No existing theme webresource
				webResourceRepositoryMock
					.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_/themes/theme.xml"), true))
					.ReturnsAsync([]);

				var settingDefRecord = CreateSettingDefinition(Guid.NewGuid());
				settingDefinitionRepositoryMock
					.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
					.ReturnsAsync(settingDefRecord);

				organizationSettingRepositoryMock
					.Setup(r => r.GetByDefinitionsAsync(crmMock.Object, It.IsAny<IReadOnlyList<SettingDefinition>>()))
					.ReturnsAsync(new List<OrganizationSetting>());

				repoMock.Setup(r => r.GetCurrentDefaultSolutionAsync()).ReturnsAsync("MySolution");

				var solution = CreateSolution("MySolution", "new");
				solutionRepositoryMock
					.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "MySolution"))
					.ReturnsAsync(solution);

				crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>())).ReturnsAsync(newThemeId);

				// Mock for UpsertSolutionComponentsAsync query - return "already exists" so ExecuteMultiple has empty request
				var existingComponent = new Entity("solutioncomponent") { Id = Guid.NewGuid() };
				existingComponent["objectid"] = newThemeId;
				existingComponent["componenttype"] = 61; // WebResource
				crmMock.Setup(c => c.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solutioncomponent")))
					.ReturnsAsync(new EntityCollection([existingComponent]));

				// Mock ExecuteMultiple to return an empty response collection
				crmMock.Setup(c => c.ExecuteAsync(It.IsAny<ExecuteMultipleRequest>()))
					.ReturnsAsync(CreateEmptyExecuteMultipleResponse());

				// Mock for SaveSettingValue and PublishXml
				crmMock.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r.RequestName == "SaveSettingValue" || r.RequestName == "PublishXml")))
					.ReturnsAsync(new OrganizationResponse());

				var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
				var result = await executor.ExecuteAsync(
					new SetEnvImageCommand
					{
						WebResourceUniqueName = "new_logo.png",
						BasePaletteColor = "#0078D4"
					},
					CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);

				// Verify theme webresource was created (any webresource entity)
				crmMock.Verify(c => c.CreateAsync(It.Is<Entity>(e => e.LogicalName == "webresource")), Times.Once);

				// Verify SaveSettingValue was called with the correct theme name
				crmMock.Verify(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r =>
					r.RequestName == "SaveSettingValue" &&
					r["SettingName"].ToString() == "CustomThemeDefinition" &&
					r["Value"].ToString() == "new_/themes/theme.xml")), Times.Once);
			}

		#endregion

		#region Existing Theme Update Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateExistingThemeWebResource_WhenSettingContainsThemeReference()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var existingThemeXml = "<CustomTheme basePaletteColor=\"#0078D4\" logoWebResource=\"old_logo.png\" />";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)),
				Guid.NewGuid());

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_logo.png" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var updatedContentBase64 = theme.content;
			Assert.IsFalse(string.IsNullOrWhiteSpace(updatedContentBase64));
			var updatedXml = Encoding.UTF8.GetString(Convert.FromBase64String(updatedContentBase64));
			StringAssert.Contains(updatedXml, "new_logo.png");
			// Original color should be preserved
			StringAssert.Contains(updatedXml, "#0078D4");

			crmMock.Verify(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r.RequestName == "SaveSettingValue")), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenThemeSettingExistsButWebResourceNotFound()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_logo.png"), false))
				.ReturnsAsync([logo]);

			// Theme webresource not found
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "deleted_theme.xml"), true))
				.ReturnsAsync([]);

			var settingDef = CreateSettingDefinition(Guid.NewGuid());
			settingDefinitionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
				.ReturnsAsync(settingDef);

			var orgSetting = CreateOrganizationSetting("deleted_theme.xml");
			organizationSettingRepositoryMock
				.Setup(r => r.GetByDefinitionsAsync(crmMock.Object, It.IsAny<IReadOnlyList<SettingDefinition>>()))
				.ReturnsAsync([orgSetting]);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_logo.png" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unable to find theme webresource");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenThemeWebResourceHasNoContent()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var themeWithNoContent = CreateWebResource("new_/themes/theme.xml", WebResourceType.Data, null, Guid.NewGuid());

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_logo.png"), false))
				.ReturnsAsync([logo]);

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_/themes/theme.xml"), true))
				.ReturnsAsync([themeWithNoContent]);

			var settingDef = CreateSettingDefinition(Guid.NewGuid());
			settingDefinitionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
				.ReturnsAsync(settingDef);

			var orgSetting = CreateOrganizationSetting("new_/themes/theme.xml");
			organizationSettingRepositoryMock
				.Setup(r => r.GetByDefinitionsAsync(crmMock.Object, It.IsAny<IReadOnlyList<SettingDefinition>>()))
				.ReturnsAsync([orgSetting]);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);

			try
			{
				await executor.ExecuteAsync(
					new SetEnvImageCommand { WebResourceUniqueName = "new_logo.png" },
					CancellationToken.None);
				Assert.Fail("Expected CommandException was not thrown");
			}
			catch (CommandException)
			{
				// Expected
			}
		}

		#endregion

		#region Color (basePaletteColor) Logic Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldPreserveExistingColor_WhenNoColorProvided()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var existingThemeXml = "<CustomTheme basePaletteColor=\"#FF5733\" logoWebResource=\"old_logo.png\" />";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)),
				Guid.NewGuid());

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png"
					// No BasePaletteColor provided
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var updatedXml = Encoding.UTF8.GetString(Convert.FromBase64String(theme.content!));
			StringAssert.Contains(updatedXml, "#FF5733", "Original color should be preserved");
			StringAssert.Contains(updatedXml, "new_logo.png", "Logo should be updated");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateColor_WhenNewColorProvided()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var existingThemeXml = "<CustomTheme basePaletteColor=\"#FF5733\" logoWebResource=\"old_logo.png\" />";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)),
				Guid.NewGuid());

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png",
					BasePaletteColor = "#00FF00"
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var updatedXml = Encoding.UTF8.GetString(Convert.FromBase64String(theme.content!));
			StringAssert.Contains(updatedXml, "#00FF00", "Color should be updated to new value");
			Assert.IsFalse(updatedXml.Contains("#FF5733"), "Original color should be replaced");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldNormalizeColorFormat_WhenColorProvidedWithoutHash()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var existingThemeXml = "<CustomTheme basePaletteColor=\"#000000\" logoWebResource=\"old_logo.png\" />";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)),
				Guid.NewGuid());

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png",
					BasePaletteColor = "AABBCC" // Without hash
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var updatedXml = Encoding.UTF8.GetString(Convert.FromBase64String(theme.content!));
			StringAssert.Contains(updatedXml, "#AABBCC", "Color should be normalized with hash prefix");
		}

		#endregion

		#region XML Handling Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldWrapAppHeaderColors_InCustomThemeRoot()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var appHeaderColorsXml = "<AppHeaderColors primaryColor=\"#000000\" secondaryColor=\"#FFFFFF\" />";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(appHeaderColorsXml)),
				Guid.NewGuid());

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png",
					BasePaletteColor = "#0078D4"
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var updatedXml = Encoding.UTF8.GetString(Convert.FromBase64String(theme.content!));
			StringAssert.Contains(updatedXml, "<CustomTheme", "Should have CustomTheme as root");
			StringAssert.Contains(updatedXml, "<AppHeaderColors", "Should contain original AppHeaderColors as child");
			StringAssert.Contains(updatedXml, "logoWebResource=\"new_logo.png\"");
			StringAssert.Contains(updatedXml, "basePaletteColor=\"#0078D4\"");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateCustomThemeDirectly_WhenRootIsCustomTheme()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var customThemeXml = "<CustomTheme basePaletteColor=\"#123456\" logoWebResource=\"old.png\"><AppHeaderColors primaryColor=\"#000\" /></CustomTheme>";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(customThemeXml)),
				Guid.NewGuid());

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png",
					BasePaletteColor = "#ABCDEF"
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var updatedXml = Encoding.UTF8.GetString(Convert.FromBase64String(theme.content!));

			// Should update attributes in place, not wrap
			Assert.AreEqual(1, CountOccurrences(updatedXml, "<CustomTheme"), "Should have only one CustomTheme element");
			StringAssert.Contains(updatedXml, "logoWebResource=\"new_logo.png\"");
			StringAssert.Contains(updatedXml, "basePaletteColor=\"#ABCDEF\"");
			StringAssert.Contains(updatedXml, "<AppHeaderColors", "Child elements should be preserved");
		}

		#endregion

		#region Publishing Tests

		[TestMethod]
		public async Task ExecuteAsync_ShouldPublishThemeWebResource_AfterUpdate()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock) = CreateMocks();

			var themeId = Guid.NewGuid();
			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var existingThemeXml = "<CustomTheme basePaletteColor=\"#0078D4\" logoWebResource=\"old_logo.png\" />";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)),
				themeId);

			SetupValidScenario(crmMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, organizationSettingRepositoryMock, logo, theme);

			var executor = CreateExecutor(output, repoMock, webResourceRepositoryMock, settingDefinitionRepositoryMock, appSettingRepositoryMock, organizationSettingRepositoryMock, publishXmlBuilderMock, solutionRepositoryMock);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand { WebResourceUniqueName = "new_logo.png" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			publishXmlBuilderMock.Verify(p => p.AddWebResource(themeId), Times.Once);
			publishXmlBuilderMock.Verify(p => p.Build(), Times.Once);
		}

		#endregion

		#region Helper Methods

		private static void SetupValidScenario(
			Mock<IOrganizationServiceAsync2> crmMock,
			Mock<IWebResourceRepository> webResourceRepositoryMock,
			Mock<ISettingDefinitionRepository> settingDefinitionRepositoryMock,
			Mock<IOrganizationSettingRepository> organizationSettingRepositoryMock,
			WebResource logo,
			WebResource theme)
		{
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == logo.name), false))
				.ReturnsAsync([logo]);

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == theme.name), true))
				.ReturnsAsync([theme]);

			var settingDef = CreateSettingDefinition(Guid.NewGuid());
			settingDefinitionRepositoryMock
				.Setup(r => r.GetByUniqueNameAsync(crmMock.Object, "CustomThemeDefinition"))
				.ReturnsAsync(settingDef);

			var orgSetting = CreateOrganizationSetting(theme.name!);
			organizationSettingRepositoryMock
				.Setup(r => r.GetByDefinitionsAsync(crmMock.Object, It.IsAny<IReadOnlyList<SettingDefinition>>()))
				.ReturnsAsync([orgSetting]);

			crmMock.Setup(c => c.UpdateAsync(It.IsAny<Entity>())).Returns(Task.CompletedTask);
			crmMock.Setup(c => c.CreateAsync(It.IsAny<Entity>())).ReturnsAsync(Guid.NewGuid());
			crmMock.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>())).ReturnsAsync(new PublishXmlResponse());
		}

		private static SettingDefinition CreateSettingDefinition(Guid id)
		{
			var entity = new Entity("settingdefinition") { Id = id };
			entity["uniquename"] = "CustomThemeDefinition";
			var constructor = typeof(SettingDefinition).GetConstructor(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				binder: null,
				[typeof(Entity)],
				modifiers: null);
			return (SettingDefinition)constructor!.Invoke([entity]);
		}

		private static OrganizationSetting CreateOrganizationSetting(string value)
		{
			var entity = new Entity("organizationsetting") { Id = Guid.NewGuid() };
			entity["value"] = value;
			return new OrganizationSetting(entity);
		}

		private static AppSetting CreateAppSetting(string value)
		{
			var entity = new Entity("appsetting") { Id = Guid.NewGuid() };
			entity["value"] = value;
			var constructor = typeof(AppSetting).GetConstructor(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				binder: null,
				[typeof(Entity)],
				modifiers: null);
			return (AppSetting)constructor!.Invoke([entity]);
		}

		private static WebResource CreateWebResource(string name, WebResourceType type, string? content, Guid? id = null)
		{
			var entity = new Entity("webresource")
			{
				Id = id ?? Guid.NewGuid()
			};
			entity["name"] = name;
			entity["displayname"] = name;
			entity["webresourcetype"] = new OptionSetValue((int)type);
			if (!string.IsNullOrWhiteSpace(content))
			{
				entity["content"] = content;
			}

			var constructor = typeof(WebResource).GetConstructor(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				binder: null,
				[typeof(Entity)],
				modifiers: null);
			return (WebResource)constructor!.Invoke([entity]);
		}

		private static Greg.Xrm.Command.Model.Solution CreateSolution(string uniqueName, string? publisherPrefix)
		{
			var entity = new Entity("solution") { Id = Guid.NewGuid() };
			entity["uniquename"] = uniqueName;
			if (publisherPrefix != null)
			{
				// Set the aliased value that the Solution model expects
				entity["publisher.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", publisherPrefix);
			}

			var constructor = typeof(Greg.Xrm.Command.Model.Solution).GetConstructor(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
				binder: null,
				[typeof(Entity)],
				modifiers: null);
			return (Greg.Xrm.Command.Model.Solution)constructor!.Invoke([entity]);
		}

		private static Microsoft.Xrm.Sdk.Messages.ExecuteMultipleResponse CreateEmptyExecuteMultipleResponse()
		{
			var response = new Microsoft.Xrm.Sdk.Messages.ExecuteMultipleResponse();
			response.Results["Responses"] = new ExecuteMultipleResponseItemCollection();
			return response;
		}

		private static int CountOccurrences(string text, string pattern)
		{
			int count = 0;
			int index = 0;
			while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
			{
				count++;
				index += pattern.Length;
			}
			return count;
		}

		#endregion
	}
}
