using Microsoft.Crm.Sdk;
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

		public string returnedtypecode
		{
			get => Get<string>();
			set => SetValue(value);
		}



		public string GetQueryTypeLabel()
		{
			if (this.querytype == null)
				return "Unknown";

			if (this.querytype == SavedQueryQueryType.AddressBookFilters)
				return "Address Book Filters";
			if (this.querytype == SavedQueryQueryType.AdvancedSearch)
				return "Advanced Find";
			if (this.querytype == SavedQueryQueryType.CopilotView)
				return "Copilot View";
			if (this.querytype == SavedQueryQueryType.CustomDefinedView)
				return "Custom View";
			if (this.querytype == SavedQueryQueryType.ExportFieldTranslationsView)
				return "Export Field Translations View";
			if (this.querytype == SavedQueryQueryType.InteractiveWorkflowView)
				return "Interactive Workflow View";
			if (this.querytype == SavedQueryQueryType.LookupView)
				return "Lookup View";
			if (this.querytype == SavedQueryQueryType.MainApplicationView)
				return "Public View";
			if (this.querytype == SavedQueryQueryType.MainApplicationViewWithoutSubject)
				return "Public View (without subject)";
			if (this.querytype == SavedQueryQueryType.OfflineFilters)
				return "Offline Filters";
			if (this.querytype == SavedQueryQueryType.OfflineTemplate)
				return "Offline Template";
			if (this.querytype == SavedQueryQueryType.OutlookFilters)
				return "Outlook Filters";
			if (this.querytype == SavedQueryQueryType.OutlookTemplate)
				return "Outlook Template";
			if (this.querytype == SavedQueryQueryType.QuickFindSearch)
				return "Quick Find View";
			if (this.querytype == SavedQueryQueryType.Reporting)
				return "Reporting";
			if (this.querytype == SavedQueryQueryType.SavedQueryTypeOther)
				return "Saved Query for Workflow or Email Templates";
			if (this.querytype == SavedQueryQueryType.SMAppointmentBookView)
				return "Service Management Appointment Book View";
			if (this.querytype == SavedQueryQueryType.SubGrid)
				return "Associated View";

			// If none of the known values match, return the numeric value
			return $"Unknown ({this.querytype})";
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

			public async Task<IReadOnlyList<T>> GetByTableNameAsync(IOrganizationServiceAsync2 crm, string tableName)
			{
				var query = new QueryExpression(this.entityName);
				query.ColumnSet.AddColumns(nameof(name), nameof(querytype), nameof(fetchxml), nameof(layoutxml), nameof(returnedtypecode));
				query.Criteria.AddCondition(nameof(returnedtypecode), ConditionOperator.Equal, tableName);
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities.Select(this.factoryMethod).ToList();
			}

			public async Task<IReadOnlyList<T>> GetByNameAsync(IOrganizationServiceAsync2 crm, string viewName)
			{
				var query = new QueryExpression(this.entityName);
				query.ColumnSet.AddColumns(nameof(name), nameof(querytype), nameof(fetchxml), nameof(layoutxml), nameof(returnedtypecode));
				query.Criteria.AddCondition(nameof(name), ConditionOperator.Equal, viewName);
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities.Select(this.factoryMethod).ToList();
			}
		}
	}
}
