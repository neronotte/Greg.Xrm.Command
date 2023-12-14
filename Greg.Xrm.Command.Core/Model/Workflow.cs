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
		}

		public enum Status
		{
			Draft = 1,
			Activated = 2,
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
		}
	}
}
