using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Model
{
	public class Solution : EntityWrapper
	{
		protected Solution(Entity entity) : base(entity)
		{
		}


		public string uniquename => Get<string>();
		public string version => Get<string>();

		public bool ismanaged => Get<bool>();

		public EntityReference publisherid => Get<EntityReference>();


		public string? PublisherCustomizationPrefix => GetAliased<string>("publisher", "customizationprefix");
		public string? PublisherUniqueName => GetAliased<string>("publisher", "uniquename");
		public int? PublisherOptionSetPrefix => GetAliased<int>("publisher", "customizationoptionvalueprefix");



		public async Task<UpsertSolutionComponentResult<Entity>> UpsertSolutionComponentsAsync(IOrganizationServiceAsync2 crm, IReadOnlyCollection<Entity> componentList, ComponentType componentType)
		{
			return await UpsertSolutionComponentsAsync(crm, componentList, x => x.Id, componentType);
		}

		public async Task<UpsertSolutionComponentResult<EntityWrapper>> UpsertSolutionComponentsAsync(IOrganizationServiceAsync2 crm, IReadOnlyCollection<EntityWrapper> componentList, ComponentType componentType)
		{
			return await UpsertSolutionComponentsAsync(crm, componentList, x => x.Id, componentType);
		}


		private async Task<UpsertSolutionComponentResult<T>> UpsertSolutionComponentsAsync<T>(IOrganizationServiceAsync2 crm, IReadOnlyCollection<T> componentList, Func<T, Guid> idAccessor, ComponentType componentType)
		{
			var componentIds = componentList.Select(idAccessor).Cast<object>().ToArray();

			var query = new QueryExpression("solutioncomponent");
			query.ColumnSet.AddColumns("objectid", "componenttype");
			query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, Id);
			query.Criteria.AddCondition("objectid", ConditionOperator.In, componentIds);
			query.NoLock = true;

			var existingComponents = await crm.RetrieveMultipleAsync(query);

			var existingComponentsById = existingComponents.Entities.ToDictionary(x => x.GetAttributeValue<Guid>("objectid"));

			var componentsToAdd = new List<T>();
			var componentsAdded = new List<T>();
			var componentsAlreadyThere = new List<T>();
			var componentsWithErrors = new List<ComponentWithError<T>>();

			foreach (var component in componentList)
			{
				var componentId = idAccessor(component);

				if (existingComponentsById.ContainsKey(componentId))
				{
					componentsAlreadyThere.Add(component);
					continue;
				}

				componentsToAdd.Add(component);
			}



			var executeMultipleRequest = new ExecuteMultipleRequest
			{
				Requests = new OrganizationRequestCollection(),
				Settings = new ExecuteMultipleSettings
				{
					ContinueOnError = true,
					ReturnResponses = true
				}
			};
			componentsToAdd.ForEach(component =>
			{
				var addRequest = new AddSolutionComponentRequest
				{
					ComponentType = (int)componentType,
					SolutionUniqueName = this.uniquename,
					ComponentId = idAccessor(component)
				};

				executeMultipleRequest.Requests.Add(addRequest);
			});

			var response = (ExecuteMultipleResponse)(await crm.ExecuteAsync(executeMultipleRequest));

			foreach (var childResponse in response.Responses)
			{
				var request = (AddSolutionComponentRequest)executeMultipleRequest.Requests[childResponse.RequestIndex];
				var webResourceId = request.ComponentId;
				var webResource = componentsToAdd.Find(w => idAccessor(w) == webResourceId);

#pragma warning disable CS8604 // Possible null reference argument.
				if (childResponse.Fault != null)
				{
					componentsWithErrors.Add(new ComponentWithError<T>(webResource, childResponse.Fault));
				}
				else
				{
					componentsAdded.Add(webResource);
				}
#pragma warning restore CS8604 // Possible null reference argument.
			}

			return new UpsertSolutionComponentResult<T>(componentsAdded, componentsAlreadyThere, componentsWithErrors);
		}




		public record UpsertSolutionComponentResult<T>(IReadOnlyCollection<T> ComponentsAdded, IReadOnlyCollection<T> ComponentsAlreadyThere, IReadOnlyCollection<ComponentWithError<T>> ComponentsWithErrors);

		public record ComponentWithError<T>(T Component, OrganizationServiceFault Fault);




		public class Repository : ISolutionRepository 
		{
			private readonly Dictionary<string, Solution> cache = new();
			private readonly IOutput output;

			public Repository(IOutput output)
            {
				this.output = output ?? throw new ArgumentNullException(nameof(output));
			}


            public async Task<ITemporarySolution> CreateTemporarySolutionAsync(IOrganizationServiceAsync2 crm, EntityReference publisherRef)
			{
				var solutionName = $"pacx_temp_{DateTime.Now.Ticks}";

				var solution = new Entity("solution");
				solution["friendlyname"] = solutionName;
				solution["uniquename"] = solutionName;
				solution["publisherid"] = publisherRef;
				solution["version"] = "0.0.0.1";
				solution["ismanaged"] = false;
				solution.Id = await crm.CreateAsync(solution);

				return new TemporarySolution(crm, this.output, new Solution(solution));
			}

			public async Task<Solution?> GetByUniqueNameAsync(IOrganizationServiceAsync2 crm, string uniqueName)
			{
				if (cache.TryGetValue(uniqueName.ToLowerInvariant(), out var solution))
				{
					return solution;
				}


				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("ismanaged", "uniquename", "version", "publisherid");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
				var link = query.AddLink("publisher", "publisherid", "publisherid");
				link.Columns.AddColumns("customizationprefix", "uniquename", "customizationoptionvalueprefix");
				link.EntityAlias = "publisher";
				query.NoLock = true;
				query.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(query);

				solution = result.Entities.Select(x => new Solution(x)).FirstOrDefault();

				if (solution != null) cache[uniqueName.ToLowerInvariant()] = solution;

				return solution;
			}
		}
	}
}
