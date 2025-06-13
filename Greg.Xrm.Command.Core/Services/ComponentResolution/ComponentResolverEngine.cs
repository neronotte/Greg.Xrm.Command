using Greg.Xrm.Command.Model;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.ComponentResolution
{
	public class ComponentResolverEngine: IComponentResolverEngine
	{
		private readonly ILogger<ComponentResolverEngine> log;
		private readonly Dictionary<int, Func<IComponentResolver>> resolverStrategyCache = new();

		public async Task ResolveAllAsync(IReadOnlyCollection<SolutionComponent> componentList, IOrganizationServiceAsync2 crm)
		{
			var groups = componentList
				.GroupBy(_ => _.componenttype.Value)
				.OrderBy(x => x.Key)
				.ToList();

			foreach (var group in groups)
			{
				if (resolverStrategyCache.TryGetValue(group.Key, out var resolverFactory))
				{
					var resolver = resolverFactory();
					await resolver.ResolveAsync(group.ToArray(), crm);
					continue;
				}

				if (group.Key == (int)ComponentType.Entity || group.Key == (int)ComponentType.Attribute)
				{
					// this is handled at the end of the foreach
					continue;
				}

				var sampleComponent = group.First();

				if (string.IsNullOrWhiteSpace(sampleComponent.ComponentTypeName) && !string.IsNullOrEmpty(sampleComponent.SolutionComponentDefinitionPrimaryEntityName))
				{
					var resolver = ByQuery(sampleComponent.SolutionComponentDefinitionPrimaryEntityName, null);
					await resolver.ResolveAsync(group.ToArray(), crm);
				}
			}

			var entityAndAttributeResolver = new ResolverForEntitiesAndAttributes(log);
			await entityAndAttributeResolver.ResolveAsync(componentList, crm);
		}






		public ComponentResolverEngine(ILogger<ComponentResolverEngine> log)
		{
			this.log = log;
			// add here a component resolver for each value of the SolutionComponentType enum, sorted by the enum value


#pragma warning disable S125 // Sections of code should not be commented out
			//this.resolverStrategyCache.Add((int)ComponentType.Entity);
			//this.resolverStrategyCache.Add((int)ComponentType.Attribute);
			//this.resolverStrategyCache.Add((int)ComponentType.Relationship);
			//this.resolverStrategyCache.Add((int)ComponentType.AttributePicklistValue);
			//this.resolverStrategyCache.Add((int)ComponentType.AttributeLookupValue);
			//this.resolverStrategyCache.Add((int)ComponentType.ViewAttribute);
			//this.resolverStrategyCache.Add((int)ComponentType.LocalizedLabel);
			//this.resolverStrategyCache.Add((int)ComponentType.RelationshipExtraCondition);
			this.AddStrategy(ComponentType.OptionSet, () => new ResolverForGlobalOptionSets(log));
			this.AddStrategy(ComponentType.EntityRelationship, () => ByQuery("entityrelationship", "schemaname"));
			//this.resolverStrategyCache.Add((int)ComponentType.EntityRelationshipRole);
			//this.resolverStrategyCache.Add((int)ComponentType.EntityRelationshipRelationships, null);
			//this.resolverStrategyCache.Add((int)ComponentType.ManagedProperty);
			this.AddStrategy(ComponentType.EntityKey);
			this.AddStrategy(ComponentType.Privilege);
			//this.resolverStrategyCache.Add((int)ComponentType.PrivilegeObjectTypeCode);
			//this.resolverStrategyCache.Add((int)ComponentType.Index);
			this.AddStrategy(ComponentType.Role);
			//this.resolverStrategyCache.Add((int)ComponentType.RolePrivilege);
			this.AddStrategy(ComponentType.DisplayString, () => ByQuery("displaystring", "displaystringkey"));
			//this.resolverStrategyCache.Add((int)ComponentType.DisplayStringMap);
			//this.resolverStrategyCache.Add((int)ComponentType.Form);
			//this.resolverStrategyCache.Add((int)ComponentType.Organization);
			this.AddStrategy(ComponentType.SavedQuery);
			this.AddStrategy(ComponentType.Workflow, () => new ResolverForWorkflows(log));
			//this.resolverStrategyCache.Add((int)ComponentType.Report);
			//this.resolverStrategyCache.Add((int)ComponentType.ReportEntity);
			//this.resolverStrategyCache.Add((int)ComponentType.ReportCategory);
			//this.resolverStrategyCache.Add((int)ComponentType.ReportVisibility);
			//this.resolverStrategyCache.Add((int)ComponentType.Attachment);
			this.AddStrategy(ComponentType.EmailTemplate, () => ByQuery("template", "title"));
			//this.resolverStrategyCache.Add((int)ComponentType.ContractTemplate);
			//this.resolverStrategyCache.Add((int)ComponentType.KBArticleTemplate);
			//this.resolverStrategyCache.Add((int)ComponentType.MailMergeTemplate);
			//this.resolverStrategyCache.Add((int)ComponentType.DuplicateRule);
			//this.resolverStrategyCache.Add((int)ComponentType.DuplicateRuleCondition);
			//this.resolverStrategyCache.Add((int)ComponentType.EntityMap);
			//this.resolverStrategyCache.Add((int)ComponentType.AttributeMap);
			//this.resolverStrategyCache.Add((int)ComponentType.RibbonCommand);
			//this.resolverStrategyCache.Add((int)ComponentType.RibbonContextGroup);
			this.AddStrategy(ComponentType.RibbonCustomization, () => ByQuery("ribboncustomization", "entity"));
			//this.resolverStrategyCache.Add((int)ComponentType.RibbonRule);
			//this.resolverStrategyCache.Add((int)ComponentType.RibbonTabToCommandMap);
			//this.resolverStrategyCache.Add((int)ComponentType.RibbonDiff);
			this.AddStrategy(ComponentType.SavedQueryVisualization);
			this.AddStrategy(ComponentType.SystemForm, () => new ResolverForSystemForms(log));
			this.AddStrategy(ComponentType.WebResource);
			//this.resolverStrategyCache.Add((int)ComponentType.SiteMap);
			//this.resolverStrategyCache.Add((int)ComponentType.ConnectionRole);
			//this.resolverStrategyCache.Add((int)ComponentType.ComplexControl);
			//this.resolverStrategyCache.Add((int)ComponentType.HierarchyRule);
			this.AddStrategy(ComponentType.CustomControl);
			//this.resolverStrategyCache.Add((int)ComponentType.CustomControlDefaultConfig);
			this.AddStrategy(ComponentType.FieldSecurityProfile);
			//this.resolverStrategyCache.Add((int)ComponentType.FieldPermission);
			this.AddStrategy(ComponentType.AppModule);
			//this.resolverStrategyCache.Add((int)ComponentType.PluginType);
			this.AddStrategy(ComponentType.PluginAssembly);
			this.AddStrategy(ComponentType.SDKMessageProcessingStep);
			//this.resolverStrategyCache.Add((int)ComponentType.SDKMessageProcessingStepImage, null);
			//this.resolverStrategyCache.Add((int)ComponentType.ServiceEndpoint);
			//this.resolverStrategyCache.Add((int)ComponentType.RoutingRule);
			//this.resolverStrategyCache.Add((int)ComponentType.RoutingRuleItem);
			//this.resolverStrategyCache.Add((int)ComponentType.SLA);
			//this.resolverStrategyCache.Add((int)ComponentType.SLAItem);
			//this.resolverStrategyCache.Add((int)ComponentType.ConvertRule);
			//this.resolverStrategyCache.Add((int)ComponentType.ConvertRuleItem);
			//this.resolverStrategyCache.Add((int)ComponentType.MobileOfflineProfile);
			//this.resolverStrategyCache.Add((int)ComponentType.MobileOfflineProfileItem);
			//this.resolverStrategyCache.Add((int)ComponentType.SimilarityRule);
			//this.resolverStrategyCache.Add((int)ComponentType.DataSourceMapping);
			//this.resolverStrategyCache.Add((int)ComponentType.SDKMessage);
			//this.resolverStrategyCache.Add((int)ComponentType.SDKMessageFilter);
			//this.resolverStrategyCache.Add((int)ComponentType.SdkMessagePair);
			//this.resolverStrategyCache.Add((int)ComponentType.SdkMessageRequest);
			//this.resolverStrategyCache.Add((int)ComponentType.SdkMessageRequestField);
			//this.resolverStrategyCache.Add((int)ComponentType.SdkMessageResponse);
			//this.resolverStrategyCache.Add((int)ComponentType.SdkMessageResponseField);
			//this.resolverStrategyCache.Add((int)ComponentType.WebWizard);
			//this.resolverStrategyCache.Add((int)ComponentType.ImportMap);
			//this.resolverStrategyCache.Add((int)ComponentType.CanvasApp);
			//this.resolverStrategyCache.Add((int)ComponentType.Connector);
			//this.resolverStrategyCache.Add((int)ComponentType.Connector2);
			this.AddStrategy(ComponentType.EnvironmentVariableDefinition, () => ByQuery("environmentvariabledefinition", "schemaname", "environmentvariabledefinitionid"));
			this.AddStrategy(ComponentType.EnvironmentVariableValue, () => ByQuery("environmentvariablevalue", "schemaname", "environmentvariablevalueid"));
			//this.resolverStrategyCache.Add((int)ComponentType.AIProjectType);
			//this.resolverStrategyCache.Add((int)ComponentType.AIProject);
			//this.resolverStrategyCache.Add((int)ComponentType.AIConfiguration);
			//this.resolverStrategyCache.Add((int)ComponentType.EntityAnalyticsConfiguration, null);
			//this.resolverStrategyCache.Add((int)ComponentType.AttributeImageConfiguration, null);
			//this.resolverStrategyCache.Add((int)ComponentType.EntityImageConfiguration);
#pragma warning restore S125 // Sections of code should not be commented out

		}

		private void AddStrategy(ComponentType componentType)
		{
			var tableName = Enum.GetName(typeof(ComponentType), componentType)?.ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(tableName))
				throw new InvalidOperationException($"Invalid enum value {componentType}");

			AddStrategy(componentType, () => ByQuery(tableName));
		}

		private void AddStrategy(ComponentType componentType, Func<IComponentResolver> factory)
		{
			resolverStrategyCache[(int)componentType] = factory;
		}

		private IComponentResolver ByQuery(string table, string nameColumn = "name", string tableIdColumn = null)
		{
			return new ResolverByQuery(log, table, nameColumn, tableIdColumn);
		}
	}
}
