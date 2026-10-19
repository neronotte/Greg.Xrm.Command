using Autofac;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Forms;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class FormServicesRegistrationTest
	{
		[TestMethod]
		public void ResolvesFormExecutorsWithRegisteredServices()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule<IoCModule>();
			builder.RegisterInstance(Mock.Of<IOrganizationServiceRepository>());
			builder.RegisterInstance(Mock.Of<IOutput>());
			builder.RegisterType<GetCommandExecutor>();
			builder.RegisterType<LayoutCommandExecutor>();
			builder.RegisterType<SetCommandExecutor>();
			using var container = builder.Build();

			Assert.IsInstanceOfType<Form.Repository>(container.Resolve<IFormRepository>());
			Assert.IsInstanceOfType<Greg.Xrm.Command.Model.Solution.Repository>(container.Resolve<ISolutionRepository>());
			Assert.IsInstanceOfType<FormLayoutRenderer>(container.Resolve<IFormLayoutRenderer>());
			Assert.IsInstanceOfType<FormXmlValidator>(container.Resolve<IFormXmlValidator>());
			Assert.IsNotNull(container.Resolve<GetCommandExecutor>());
			Assert.IsNotNull(container.Resolve<LayoutCommandExecutor>());
			Assert.IsNotNull(container.Resolve<SetCommandExecutor>());
		}
	}
}
