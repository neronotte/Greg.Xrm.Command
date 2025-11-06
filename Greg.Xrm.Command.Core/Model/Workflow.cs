using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
#pragma warning disable IDE1006 // Naming Styles
	public class Workflow : EntityWrapper
	{
		private Workflow(Entity entity) : base(entity)
		{
		}

		public Workflow() : base("workflow")
		{
		}

		public Workflow(Guid id) : base(new Entity("workflow", id))
		{
		}

		public string? name
		{
			get => Get<string>();
		}

		public OptionSetValue category
		{
			get => Get<OptionSetValue>();
		}

		public string? CategoryFormatted => GetFormatted(nameof(category));

		public string? xaml
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string? triggeronupdateattributelist
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public OptionSetValue? statecode
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}
		public string? StateCodeFormatted => GetFormatted(nameof(statecode));

		public OptionSetValue? statuscode
		{
			get => Get<OptionSetValue>();
			set => SetValue(value);
		}
		public string? StatusCodeFormatted => GetFormatted(nameof(statuscode));


		public enum State
		{
			Draft = 0,
			Activated = 1,
			Suspended = 2,
		}

		public enum Status
		{
			Draft = 1,
			Activated = 2,
			CompanyDLPViolation = 3,
		}

		public enum Category
		{
			Worfklow = 0,
			Dialog = 1,
			BusinessRule = 2,
			Action = 3,
			BusinessProcessFlow = 4,
			ModernFlow = 5,
			DesktopFlow = 6,
			AIFlow = 7,
		}


		public class Repository : IWorkflowRepository
		{
			public async Task<IReadOnlyList<Workflow>> GetByIdsAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> ids)
			{
				var query = new QueryExpression("workflow");
				query.ColumnSet.AddColumns(nameof(name), nameof(category), nameof(xaml), nameof(statecode), nameof(statuscode), nameof(triggeronupdateattributelist));
				query.Criteria.AddCondition("workflowid", ConditionOperator.In, ids.Cast<object>().ToArray());
				query.NoLock = true;


				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities.Select(x => new Workflow(x)).ToArray();
			}

			public async Task<IReadOnlyList<Workflow>> GetByNameAsync(IOrganizationServiceAsync2 crm, string uniqueName)
			{
				var query = new QueryExpression("workflow");
				query.ColumnSet.AddColumns(nameof(name), nameof(category), nameof(xaml), nameof(statecode), nameof(statuscode), nameof(triggeronupdateattributelist));
				query.Criteria.AddCondition("name", ConditionOperator.Equal, uniqueName);
				query.NoLock = true;


				var result = await crm.RetrieveMultipleAsync(query);

				return [.. result.Entities.Select(x => new Workflow(x))];
			}

			public async Task<IReadOnlyList<Workflow>> GetBySolutionAsync(IOrganizationServiceAsync2 crm, string solutionUniqueName)
			{
				var query = new QueryExpression("workflow");
				query.ColumnSet.AddColumns(nameof(name), nameof(category), nameof(xaml), nameof(statecode), nameof(statuscode), nameof(triggeronupdateattributelist));
				var solutionComponentLink = query.AddLink("solutioncomponent", "workflowid", "objectid");
				var solutionLink = solutionComponentLink.AddLink("solution", "solutionid", "solutionid");
				solutionLink.LinkCriteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);
				query.NoLock = true;


				var result = await crm.RetrieveMultipleAsync(query);

				return [.. result.Entities.Select(x => new Workflow(x))];
			}

			public async Task<IReadOnlyList<Workflow>> SearchByNameAndSolutionAndCategoryAsync(IOrganizationServiceAsync2 crm, string namePart, string? solutionUniqueName, Category? category)
			{
				var query = new QueryExpression("workflow");
				query.ColumnSet.AddColumns(nameof(name), nameof(category), nameof(xaml), nameof(statecode), nameof(statuscode), nameof(triggeronupdateattributelist));

				if (!string.IsNullOrWhiteSpace(namePart))
				{
					query.Criteria.AddCondition("name", ConditionOperator.Like, $"%{namePart}%");
				}
				if (!string.IsNullOrWhiteSpace(solutionUniqueName))
				{
					var solutionComponentLink = query.AddLink("solutioncomponent", "workflowid", "objectid");
					var solutionLink = solutionComponentLink.AddLink("solution", "solutionid", "solutionid");
					solutionLink.LinkCriteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);
				}
				if (category.HasValue)
				{
					query.Criteria.AddCondition("category", ConditionOperator.Equal, (int)category.Value);
				}
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return [.. result.Entities.Select(x => new Workflow(x))];
			}
		}
	}
}
