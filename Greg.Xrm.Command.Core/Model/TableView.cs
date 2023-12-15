using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public abstract class TableView : EntityWrapper
	{
		protected TableView(Entity entity) : base(entity)
		{
		}

		protected TableView(string entityName) : base(entityName)
		{
		}

		public string? name
		{
			get => Get<string>();
			set => SetValue(value);
		}


		public int? querytype
		{
			get => Get<int>();
			set => SetValue(value);
		}

		public string? fetchxml
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public string? layoutxml
		{
			get => Get<string>();
			set => SetValue(value);
		}

		public OptionSetValue? returnedtypecode
		{
			get => Get<OptionSetValue>();
		}


		public abstract class Repository<T> where T : TableView
		{
			private readonly string entityName;
			private readonly Func<Entity, T> factoryMethod;

			protected Repository(string entityName, Func<Entity, T> factoryMethod)
            {
				this.entityName = entityName;
				this.factoryMethod = factoryMethod;
			}


			public async Task<IReadOnlyList<T>> GetByIdAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> ids)
			{
				var query = new QueryExpression(this.entityName);
				query.ColumnSet.AddColumns(nameof(name), nameof(querytype), nameof(fetchxml), nameof(layoutxml), nameof(returnedtypecode));
				query.Criteria.AddCondition(entityName + "id", ConditionOperator.In, ids.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities.Select(this.factoryMethod).ToList();
			}

			public async Task<IReadOnlyList<T>> GetContainingAsync(IOrganizationServiceAsync2 crm, string tableName, string columnName)
			{
				var query = new QueryExpression(this.entityName);
				query.ColumnSet.AddColumns(nameof(name), nameof(querytype), nameof(fetchxml), nameof(layoutxml), nameof(returnedtypecode));

				query.Criteria.FilterOperator = LogicalOperator.Or;
				query.Criteria.AddCondition(nameof(fetchxml), ConditionOperator.Like, $"%<entity%name=\"{tableName}\">%name=\"{columnName}\"%</entity>%");
				query.Criteria.AddCondition(nameof(fetchxml), ConditionOperator.Like, $"%<entity%name=\"{tableName}\">%attribute=\"{columnName}\"%</entity>%");
				query.Criteria.AddCondition(nameof(fetchxml), ConditionOperator.Like, $"%<link-entity%name=\"{tableName}\"%>%name=\"{columnName}\"%>%</link-entity>%");
				query.Criteria.AddCondition(nameof(fetchxml), ConditionOperator.Like, $"%<link-entity%name=\"{tableName}\"%>%attribute=\"{columnName}\"%>%</link-entity>%");

				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities.Select(this.factoryMethod).ToList();
			}
		}
	}
}
