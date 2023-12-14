using Greg.Xrm.Command.Services.ComponentResolvers;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	public class Dependency : EntityWrapper
    {
        private Dependency(Entity entity) : base(entity)
        {
			this.DependentComponentLabel = $"{this.dependentcomponentobjectid} ({this.DependentComponentTypeFormatted})";
		}

#pragma warning disable IDE1006 // Naming Styles
		public Guid requiredcomponentbasesolutionid => Get<Guid>();
        public Guid requiredcomponentobjectid => Get<Guid>();

		public OptionSetValue dependencytype => Get<OptionSetValue>();
		public Guid requiredcomponentparentid => Get<Guid>();
        public OptionSetValue requiredcomponenttype => Get<OptionSetValue>();

        public EntityReference requiredcomponentnodeid => Get<EntityReference>();
        public OptionSetValue dependentcomponenttype => Get<OptionSetValue>();
        public Guid dependentcomponentparentid => Get<Guid>();
        public Guid dependentcomponentbasesolutionid => Get<Guid>();
        public EntityReference dependentcomponentnodeid => Get<EntityReference>();
        public Guid dependentcomponentobjectid => Get<Guid>();
#pragma warning restore IDE1006 // Naming Styles


		public string? DependencyTypeFormatted => GetFormatted(nameof(dependencytype));
        public string? RequiredComponentTypeFormatted => GetFormatted(nameof(requiredcomponenttype));
        public string? DependentComponentTypeFormatted => GetFormatted(nameof(dependentcomponenttype));


		public string DependentComponentLabel { get; protected set; }


		public override string ToString()
		{
			return $"{this.DependentComponentTypeFormatted}: {this.DependentComponentLabel} --- depends on --> {this.RequiredComponentTypeFormatted}: {this.requiredcomponentobjectid}";
		}


		public class Repository : IDependencyRepository
        {
			private readonly ILogger log;

			public Repository(ILogger<Repository> log)
            {
				this.log = log;
			}

            public async Task<DependencyList> GetDependenciesAsync(IOrganizationServiceAsync2 crm, ComponentType componentType, Guid componentId, bool? forDelete = false)
			{
                EntityCollection entities;

                if (forDelete.GetValueOrDefault())
				{
					var request = new RetrieveDependenciesForDeleteRequest
					{
						ComponentType = (int)componentType,
						ObjectId = componentId
					};
					var response = (RetrieveDependenciesForDeleteResponse)await crm.ExecuteAsync(request);
                    entities = response.EntityCollection;
				}
                else
				{
					var request = new RetrieveDependentComponentsRequest
					{
						ComponentType = (int)componentType,
						ObjectId = componentId
					};
					var response = (RetrieveDependentComponentsResponse)await crm.ExecuteAsync(request);
					entities = response.EntityCollection;
				}

                var dependencies = entities.Entities
					.Select(x => new Dependency(x))
					.OrderBy(x => x.DependentComponentTypeFormatted)
					.ThenBy(x => x.dependentcomponentobjectid)
					.ToArray();

				var resolverFactory = new ComponentResolverFactory(crm, log);

				await ResolveDependencyNamesAsync(dependencies, resolverFactory);

				return new DependencyList(dependencies);
			}

			private static async Task ResolveDependencyNamesAsync(Dependency[] dependencies, ComponentResolverFactory resolverFactory)
			{
				var dependencyGroups = dependencies.GroupBy(x => x.dependentcomponenttype.Value)
				.OrderBy(x => x.First().DependentComponentTypeFormatted)
				.ToArray();

				foreach (var dependencyGroup in dependencyGroups)
				{
					var componentType = dependencyGroup.Key;
					var resolver = resolverFactory.GetComponentResolverFor(componentType);

					if (resolver != null)
					{
						var componentIds = dependencyGroup.Select(x => x.dependentcomponentobjectid).ToArray();
						var componentNames = await resolver.GetNamesAsync(componentIds);

						foreach (var dependency in dependencyGroup.OrderBy(x => x.dependentcomponentobjectid))
						{
							var componentName = componentNames[dependency.dependentcomponentobjectid];
							dependency.DependentComponentLabel = componentName;
						}
					}
				}
			}
		}
    }
}
