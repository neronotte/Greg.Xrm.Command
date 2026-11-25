using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Ribbon;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class RibbonDiffDependencyRegistrationTest
	{
		[TestMethod]
		public void ResolvesExecutorsAndSupportingServicesThroughInterfaces()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule<IoCModule>();
			builder.RegisterInstance<IOutput>(new OutputToMemory());
			builder.RegisterInstance(AnsiConsole.Create(new AnsiConsoleSettings { Ansi = AnsiSupport.No, Out = new AnsiConsoleOutput(new StringWriter()) })).As<IAnsiConsole>();
			builder.RegisterInstance(new Mock<IOrganizationServiceRepository>().Object);
			builder.RegisterAssemblyTypes(typeof(GetRibbonDiffCommand).Assembly).Where(t => !t.IsAbstract).AsClosedTypesOf(typeof(ICommandExecutor<>));
			using var container = builder.Build();
			Assert.IsInstanceOfType<GetRibbonDiffCommandExecutor>(container.Resolve<ICommandExecutor<GetRibbonDiffCommand>>());
			Assert.IsInstanceOfType<SetRibbonDiffCommandExecutor>(container.Resolve<ICommandExecutor<SetRibbonDiffCommand>>());
			Assert.IsInstanceOfType<RibbonComponent.Repository>(container.Resolve<IRibbonComponentRepository>());
			Assert.IsInstanceOfType<RibbonDiffService>(container.Resolve<IRibbonDiffService>());
		}
	}
}
