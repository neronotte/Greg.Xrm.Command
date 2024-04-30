using Autofac;
using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Commands.Table.Builders;
using Greg.Xrm.Command.Commands.Table.ExportMetadata;
using Greg.Xrm.Command.Commands.WebResources.ApplyIconsRules;
using Greg.Xrm.Command.Commands.WebResources.Templates;
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
			builder.RegisterType<WebResource.Repository>().As<IWebResourceRepository>();
			builder.RegisterType<Solution.Repository>().As<ISolutionRepository>();
			builder.RegisterType<JsTemplateManager>().As<IJsTemplateManager>();
			builder.RegisterType<IconFinder>().As<IIconFinder>();
			builder.RegisterType<Commands.Settings.Model.SettingDefinition.Repository>().As<Commands.Settings.Model.ISettingDefinitionRepository>();
			builder.RegisterType<Commands.Settings.Model.OrganizationSetting.Repository>().As<Commands.Settings.Model.IOrganizationSettingRepository>();
			builder.RegisterType<Commands.Settings.Model.AppSetting.Repository>().As<Commands.Settings.Model.IAppSettingRepository>();
			builder.RegisterType<Commands.Table.Migration.TableGraphBuilder>().AsSelf();
		}
	}
}
