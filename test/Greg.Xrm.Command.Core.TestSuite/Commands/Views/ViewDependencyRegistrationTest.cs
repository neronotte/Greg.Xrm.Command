using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Views;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ViewDependencyRegistrationTest
	{
		[TestMethod]
		public void ViewCommandsResolveWithRegisteredServiceInterfaces()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule<IoCModule>();
			builder.RegisterInstance<IOutput>(new OutputToMemory());
			builder.RegisterInstance(new Mock<IOrganizationServiceRepository>().Object);
			builder.RegisterInstance(new Mock<ISavedQueryRepository>().Object);
			builder.RegisterInstance(new Mock<IUserQueryRepository>().Object);
			builder.RegisterAssemblyTypes(typeof(CreateCommand).Assembly)
				.Where(t => !t.IsAbstract)
				.AsClosedTypesOf(typeof(ICommandExecutor<>));
			using var container = builder.Build();

			Assert.IsInstanceOfType<ViewXmlValidator>(container.Resolve<IViewXmlValidator>());
			Assert.IsInstanceOfType<ViewXmlEditor>(container.Resolve<IViewXmlEditor>());
			Assert.IsInstanceOfType<ViewRetrieverService>(container.Resolve<IViewRetrieverService>());
			Assert.IsInstanceOfType<CreateCommandExecutor>(container.Resolve<ICommandExecutor<CreateCommand>>());
			Assert.IsInstanceOfType<SetCommandExecutor>(container.Resolve<ICommandExecutor<SetCommand>>());
			Assert.IsInstanceOfType<SetFilterCommandExecutor>(container.Resolve<ICommandExecutor<SetFilterCommand>>());
			Assert.IsInstanceOfType<SetFetchXmlCommandExecutor>(container.Resolve<ICommandExecutor<SetFetchXmlCommand>>());
			Assert.IsInstanceOfType<SetColumnsCommandExecutor>(container.Resolve<ICommandExecutor<SetColumnsCommand>>());
		}
	}
}
