using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CustomApiCreateCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CustomApiCreateCommandExecutor executor;

		public CustomApiCreateCommandExecutorTest()
		{
			this.executor = new CustomApiCreateCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithBasicInputs_ShouldCreateCustomApi()
		{
			var expectedApiId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")))
				.ReturnsAsync(expectedApiId);

			var command = new CustomApiCreateCommand
			{
				Name = "new_MyAction",
				Description = "Test description"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "customapi" &&
				e["uniquename"].ToString() == "new_MyAction" &&
				e["displayname"].ToString() == "new_MyAction" &&
				e["description"].ToString() == "Test description"
			)), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithInputParameters_ShouldCreateParameters()
		{
			var expectedApiId = Guid.NewGuid();
			var createCalls = new List<Entity>();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => createCalls.Add(e))
				.ReturnsAsync((Entity e) => e.LogicalName == "customapi" ? expectedApiId : Guid.NewGuid());

			var command = new CustomApiCreateCommand
			{
				Name = "new_MyAction",
				Inputs = new[] { "String:Target", "Int:Count" },
				Outputs = new[] { "Entity:Result" }
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(3, createCalls.Count);
			
			var param1 = createCalls.FirstOrDefault(e => e.LogicalName == "customapirequestparameter");
			Assert.IsNotNull(param1);
			Assert.AreEqual("Target", param1["uniquename"]);
			Assert.AreEqual(10, ((OptionSetValue)param1["type"]).Value);

			var param2 = createCalls.FirstOrDefault(e => e.LogicalName == "customapirequestparameter" && e["uniquename"].ToString() == "Count");
			Assert.IsNotNull(param2);
			Assert.AreEqual(6, ((OptionSetValue)param2["type"]).Value);

			var outputParam = createCalls.FirstOrDefault(e => e.LogicalName == "customapiresponseproperty");
			Assert.IsNotNull(outputParam);
			Assert.AreEqual("Result", outputParam["uniquename"]);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithEntityBinding_ShouldSetCorrectType()
		{
			var expectedApiId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(expectedApiId);

			var command = new CustomApiCreateCommand
			{
				Name = "new_MyAccountAction",
				BindingType = "Entity",
				EntityLogicalName = "account"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "customapi" &&
				e["boundentitylogicalname"].ToString() == "account" &&
				((OptionSetValue)e["customapirequestprocessingtype"]).Value == 0
			)), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithFunction_ShouldSetIsFunction()
		{
			var expectedApiId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(expectedApiId);

			var command = new CustomApiCreateCommand
			{
				Name = "new_GetData",
				IsFunction = true
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "customapi" &&
				(bool)e["isfunction"] == true
			)), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithExecutePlugin_ShouldLookupPlugin()
		{
			var expectedApiId = Guid.NewGuid();
			var pluginTypeId = Guid.NewGuid();
			var pluginResult = new EntityCollection(new List<Entity> { new Entity("plugintype") { Id = pluginTypeId } });
			
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(expectedApiId);

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "plugintype")))
				.ReturnsAsync(pluginResult);

			var command = new CustomApiCreateCommand
			{
				Name = "new_MyAction",
				ExecutePluginTypeName = "MyPlugin.MyPluginType"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
				q.EntityName == "plugintype" &&
				q.Criteria.Conditions.Any(c => c.AttributeName == "typename" && c.Value?.ToString() == "MyPlugin.MyPluginType")
			)), Times.Once);

			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "customapi" &&
				((EntityReference)e["workflowsdksteppluginTypeId"]).Id == pluginTypeId
			)), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithInvalidParameterFormat_ShouldWarn()
		{
			var expectedApiId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(expectedApiId);

			var command = new CustomApiCreateCommand
			{
				Name = "new_MyAction",
				Inputs = new[] { "InvalidFormat" }
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "WARNING");
		}
	}
}
