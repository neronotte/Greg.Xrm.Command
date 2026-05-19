using System.Text;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
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
			Mock<ISolutionRepository> solutionRepositoryMock)
		CreateMocks()
		{
			var output = new OutputToMemory();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			var repoMock = new Mock<IOrganizationServiceRepository>();
			var webResourceRepositoryMock = new Mock<IWebResourceRepository>();
			var solutionRepositoryMock = new Mock<ISolutionRepository>();

			repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(crmMock.Object);

			return (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateExistingThemeWebResource_WhenSettingContainsThemeReference()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			var existingThemeXml = "<CustomThemeDefinition><Theme><Logo>webresource:old_logo.png</Logo></Theme></CustomThemeDefinition>";
			var theme = CreateWebResource(
				"new_/themes/theme.xml",
				WebResourceType.Data,
				Convert.ToBase64String(Encoding.UTF8.GetBytes(existingThemeXml)),
				Guid.NewGuid());

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_logo.png"), false))
				.ReturnsAsync([logo]);

			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.Is<string[]>(n => n.Length == 1 && n[0] == "new_/themes/theme.xml"), true))
				.ReturnsAsync([theme]);

			crmMock
				.SetupSequence(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(new EntityCollection([new Entity("settingdefinition") { Id = Guid.NewGuid() }]))
				.ReturnsAsync(new EntityCollection([new Entity("organizationsetting")
				{
					["value"] = "new_/themes/theme.xml"
				}]));

			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Returns(Task.CompletedTask);
			crmMock
				.Setup(c => c.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(Guid.NewGuid());

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync(new PublishXmlResponse());

			var executor = new SetEnvImageCommandExecutor(output, repoMock.Object, webResourceRepositoryMock.Object, solutionRepositoryMock.Object);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png"
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var updatedContentBase64 = theme.content;
			Assert.IsFalse(string.IsNullOrWhiteSpace(updatedContentBase64));
			var updatedXml = Encoding.UTF8.GetString(Convert.FromBase64String(updatedContentBase64));
			StringAssert.Contains(updatedXml, "webresource:new_logo.png");

			crmMock.Verify(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r.RequestName == "SaveSettingValue")), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenSettingIsMissingAndNoDefaultSolutionExists()
		{
			var (output, repoMock, crmMock, webResourceRepositoryMock, solutionRepositoryMock) = CreateMocks();

			var logo = CreateWebResource("new_logo.png", WebResourceType.ImagePng, null);
			webResourceRepositoryMock
				.Setup(r => r.GetByNameAsync(crmMock.Object, It.IsAny<string[]>(), false))
				.ReturnsAsync([logo]);

			crmMock
				.SetupSequence(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(new EntityCollection([new Entity("settingdefinition") { Id = Guid.NewGuid() }]))
				.ReturnsAsync(new EntityCollection());

			repoMock.Setup(r => r.GetCurrentDefaultSolutionAsync()).ReturnsAsync((string?)null);

			var executor = new SetEnvImageCommandExecutor(output, repoMock.Object, webResourceRepositoryMock.Object, solutionRepositoryMock.Object);
			var result = await executor.ExecuteAsync(
				new SetEnvImageCommand
				{
					WebResourceUniqueName = "new_logo.png"
				},
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "default solution");
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
				new[] { typeof(Entity) },
				modifiers: null);
			return (WebResource)constructor!.Invoke([entity]);
		}
	}
}
