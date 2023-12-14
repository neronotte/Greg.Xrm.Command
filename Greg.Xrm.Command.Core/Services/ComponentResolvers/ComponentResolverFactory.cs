using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.ComponentResolvers
{
	public class ComponentResolverFactory
    {
        private readonly IOrganizationServiceAsync2 crm;
        private readonly ILogger log;

        public ComponentResolverFactory(IOrganizationServiceAsync2 crm, ILogger log)
        {
            this.crm = crm;
            this.log = log;


			this.AddStrategy(ComponentType.SavedQuery);
			this.AddStrategy(ComponentType.AppModule);
			this.AddStrategy(ComponentType.CustomControl);
			this.AddStrategy(ComponentType.DisplayString, () => ByQuery("displaystring", "displaystringkey"));
			this.AddStrategy(ComponentType.EmailTemplate, () => ByQuery("template", "title"));
			this.AddStrategy(ComponentType.FieldSecurityProfile);
			this.AddStrategy(ComponentType.PluginAssembly);
			this.AddStrategy(ComponentType.RibbonCustomization, () => ByQuery("ribboncustomization", "entity"));
			this.AddStrategy(ComponentType.Role);
			this.AddStrategy(ComponentType.SDKMessageProcessingStep);
			this.AddStrategy(ComponentType.SystemForm, () => new ResolverForSystemForms(crm, log));
			this.AddStrategy(ComponentType.WebResource);
			this.AddStrategy(ComponentType.Workflow, () => ByQuery("workflow", "uniquename"));
			this.AddStrategy(ComponentType.EntityRelationship, () => ByQuery("entityrelationship", "schemaname"));

		}

		#region Strategy Management

		private readonly Dictionary<int, Func<IComponentResolver>> resolverStrategyCache = new();
		private void AddStrategy(ComponentType componentType)
		{
			var tableName = Enum.GetName(typeof(ComponentType), componentType)?.ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(tableName))
				throw new InvalidOperationException($"Invalid enum value {componentType}");

			AddStrategy(componentType, () => ByQuery(tableName));
		}

		private void AddStrategy(ComponentType componentType, Func<IComponentResolver> factory)
		{
			this.resolverStrategyCache[(int)componentType] = factory;
		}

		private IComponentResolver ByQuery(string table, string nameColumn = "name", string? tableIdColumn = null)
		{
			return new ResolverByQuery(this.crm, this.log, table, nameColumn, tableIdColumn);
		}


		#endregion



		public IComponentResolver? GetComponentResolverFor(ComponentType componentType)
        {
            return GetComponentResolverFor((int)componentType);
        }



        public IComponentResolver? GetComponentResolverFor(int componentType)
        {
            var componentTypeName = componentType.ToString();
#pragma warning disable S2486 // Generic exceptions should not be ignored
            try
            {
                componentTypeName = Enum.GetName(typeof(ComponentType), componentType);
            }
            catch { }
#pragma warning restore S2486 // Generic exceptions should not be ignored


            if (!resolverStrategyCache.TryGetValue(componentType, out var factoryMethod))
            {
                return null;
            }


            return factoryMethod();
        }
    }
}
