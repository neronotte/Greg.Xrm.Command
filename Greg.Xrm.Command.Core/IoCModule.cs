using Autofac;
using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Commands.Script.MetadataExtractor;
using Greg.Xrm.Command.Commands.Script.Service;
using Greg.Xrm.Command.Commands.Table.ExportMetadata;
using Greg.Xrm.Command.Commands.WebResources.ApplyIconsRules;
using Greg.Xrm.Command.Commands.WebResources.ProjectFile;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Commands.WebResources.Templates;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.AttributeDeletion;
using Greg.Xrm.Command.Services.OptionSet;
using Greg.Xrm.Command.Services.Plugin;

namespace Greg.Xrm.Command
{
	public class IoCModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.RegisterType<AttributeMetadataBuilderFactory>().As<IAttributeMetadataBuilderFactory>();
			builder.RegisterType<ExportMetadataStrategyFactory>().As<IExportMetadataStrategyFactory>();
			builder.RegisterType<Dependency.Repository>().As<IDependencyRepository>();
            builder.RegisterType<Workflow.Repository>().As<IWorkflowRepository>();
			builder.RegisterType<ProcessTrigger.Repository>().As<IProcessTriggerRepository>();
			builder.RegisterType<WebResource.Repository>().As<IWebResourceRepository>();
			builder.RegisterType<Solution.Repository>().As<ISolutionRepository>();
			builder.RegisterType<SolutionComponent.Repository>().As<ISolutionComponentRepository>();
			builder.RegisterType<JsTemplateManager>().As<IJsTemplateManager>();
			builder.RegisterType<IconFinder>().As<IIconFinder>();
			builder.RegisterType<SavedQuery.Repository>().As<ISavedQueryRepository>();
			builder.RegisterType<UserQuery.Repository>().As<IUserQueryRepository>();
			builder.RegisterType<ScriptExtractionService>().As<IScriptExtractionService>();
            builder.RegisterType<ScriptMetadataExtractor>().As<IScriptMetadataExtractor>();
            builder.RegisterType<ScriptBuilder>().As<IScriptBuilder>();
            builder.RegisterType<Commands.Settings.Model.SettingDefinition.Repository>().As<Commands.Settings.Model.ISettingDefinitionRepository>();
			builder.RegisterType<Commands.Settings.Model.OrganizationSetting.Repository>().As<Commands.Settings.Model.IOrganizationSettingRepository>();
			builder.RegisterType<Commands.Settings.Model.AppSetting.Repository>().As<Commands.Settings.Model.IAppSettingRepository>();
			builder.RegisterType<Commands.Settings.Imports.ImportStrategyFactory>().As<Commands.Settings.Imports.IImportStrategyFactory>();
			builder.RegisterType<Commands.Table.Migration.TableGraphBuilder>().AsSelf();
			builder.RegisterType<Commands.Views.Model.ViewRetrieverService>().As<Commands.Views.Model.IViewRetrieverService>();
			builder.RegisterType<OptionSetParser>().As<IOptionSetParser>();


			builder.RegisterType<FolderResolver>().As<IFolderResolver>();
			builder.RegisterType<WebResourceFilesResolver>().As<IWebResourceFilesResolver>();
			builder.RegisterType<WebResourceProjectFileRepository>().As<IWebResourceProjectFileRepository>();
			builder.RegisterType<PublishXmlBuilder>().As<IPublishXmlBuilder>();


			builder.RegisterType<AttributeDeletionService>().As<IAttributeDeletionService>();
			builder.RegisterType<AttributeDeletionStrategyForCharts>().As<IAttributeDeletionStrategy>();
			builder.RegisterType<AttributeDeletionStrategyForViews>().As<IAttributeDeletionStrategy>();
			builder.RegisterType<AttributeDeletionStrategyForForms>().As<IAttributeDeletionStrategy>();
			builder.RegisterType<AttributeDeletionStrategyForPluginSteps>().As<IAttributeDeletionStrategy>();
			builder.RegisterType<AttributeDeletionStrategyForPluginStepImages>().As<IAttributeDeletionStrategy>();
			builder.RegisterType<AttributeDeletionStrategyForRelationships>().As<IAttributeDeletionStrategy>();
			builder.RegisterType<AttributeDeletionStrategyForMappings>().As<IAttributeDeletionStrategy>();
			builder.RegisterType<AttributeDeletionStrategyForWorkflows>().As<IAttributeDeletionStrategy>();

			builder.RegisterType<Services.ComponentResolution.ComponentResolverEngine>().As<Services.ComponentResolution.IComponentResolverEngine>();

			builder.RegisterType<PluginPackageReader>().As<IPluginPackageReader>();
			builder.RegisterType<PluginPackage.Repository>().As<IPluginPackageRepository>();
			builder.RegisterType<PluginAssembly.Repository>().As<IPluginAssemblyRepository>();
			builder.RegisterType<PluginType.Repository>().As<IPluginTypeRepository>();
			builder.RegisterType<SdkMessage.Repository>().As<ISdkMessageRepository>();

			builder.RegisterType<Form.Repository>().As<IFormRepository>();
		}
	}
}
