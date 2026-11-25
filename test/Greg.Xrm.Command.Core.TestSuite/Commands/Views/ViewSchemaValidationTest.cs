using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Views;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ViewSchemaValidationTest : CommandExecutorTestBase
	{
		[TestMethod]
		[DataRow("create")]
		[DataRow("set")]
		[DataRow("setFilter")]
		[DataRow("setFetchXml")]
		[DataRow("setColumns")]
		public async Task SchemaInvalidDefinitionDoesNotSaveOrPublish(string operation)
		{
			const string fetch = "<fetch><entity name='account'><attribute name='name'/></entity></fetch>";
			const string invalidFetch = "<fetch><entity name='account'><attribute name='name'/><filter type='xor'/></entity></fetch>";
			const string layout = "<grid><row id='accountid'><cell name='name' width='150'/></row></grid>";
			const string invalidLayout = "<grid><row id='accountid'><cell name='name' width='wide'/></row></grid>";
			var view = new SavedQuery(new Entity("savedquery", Guid.NewGuid())
			{
				["name"] = "Active", ["returnedtypecode"] = "account", ["fetchxml"] = fetch,
				["layoutxml"] = operation == "setColumns" ? invalidLayout : layout
			});
			var retriever = new Mock<IViewRetrieverService>();
			retriever.Setup(r => r.GetByNameAsync(OrganizationServiceMock.Object, QueryType1.SavedQuery, "Active", null))
				.ReturnsAsync((CommandResult.Success(), (TableView)view));
			var builder = new ContainerBuilder();
			builder.RegisterModule<IoCModule>();
			builder.RegisterInstance<IOutput>(Output);
			builder.RegisterInstance(OrganizationServiceRepositoryMock.Object);
			builder.RegisterInstance(retriever.Object);
			builder.RegisterAssemblyTypes(typeof(CreateCommand).Assembly).Where(t => !t.IsAbstract)
				.AsClosedTypesOf(typeof(ICommandExecutor<>));
			using var container = builder.Build();

			var result = operation switch
			{
				"create" => await container.Resolve<ICommandExecutor<CreateCommand>>().ExecuteAsync(
					new CreateCommand { ViewName = "Active", FetchXml = invalidFetch, Publish = true }, CancellationToken.None),
				"set" => await container.Resolve<ICommandExecutor<SetCommand>>().ExecuteAsync(
					new SetCommand { ViewName = "Active", FetchXml = fetch, LayoutXml = invalidLayout, Publish = true }, CancellationToken.None),
				"setFilter" => await container.Resolve<ICommandExecutor<SetFilterCommand>>().ExecuteAsync(
					new SetFilterCommand { ViewName = "Active", Filter = "<filter type='xor'/>", Publish = true }, CancellationToken.None),
				"setFetchXml" => await container.Resolve<ICommandExecutor<SetFetchXmlCommand>>().ExecuteAsync(
					new SetFetchXmlCommand { ViewName = "Active", FetchXml = invalidFetch, Publish = true }, CancellationToken.None),
				_ => await container.Resolve<ICommandExecutor<SetColumnsCommand>>().ExecuteAsync(
					new SetColumnsCommand { ViewName = "Active", Columns = "name", Publish = true }, CancellationToken.None)
			};

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage!, "schema");
			OrganizationServiceMock.Verify(c => c.CreateAsync(It.IsAny<Entity>()), Times.Never);
			OrganizationServiceMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Never);
			OrganizationServiceMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}
	}
}
