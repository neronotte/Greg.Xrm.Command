using Autofac;
using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Commands.Table.Builders;
using Greg.Xrm.Command.Commands.Table.ExportMetadata;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command
{
    public class IoCModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.RegisterType<AttributeMetadataBuilderFactory>().As<IAttributeMetadataBuilderFactory>();
			builder.RegisterType<ExportMetadataStrategyFactory>().As<IExportMetadataStrategyFactory>();
			builder.RegisterType<AttributeDeletionService>().As<IAttributeDeletionService>();
			builder.RegisterType<Dependency.Repository>().As<IDependencyRepository>();
			builder.RegisterType<Workflow.Repository>().As<IWorkflowRepository>();
			builder.RegisterType<ProcessTrigger.Repository>().As<IProcessTriggerRepository>();
			builder.RegisterType<AttributeMetadataScriptBuilderFactory>().As<IAttributeMetadataScriptBuilderFactory>();
		}
	}
}
