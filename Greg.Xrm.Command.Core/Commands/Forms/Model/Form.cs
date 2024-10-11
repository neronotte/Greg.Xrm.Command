using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Forms.Model
{
    public class Form : EntityWrapper
	{
        public Form(Entity entity) : base(entity)
        {
        }

		[DataverseColumn]
#pragma warning disable IDE1006 // Naming Styles
		public OptionSetValue type => this.Get<OptionSetValue>();

		[DataverseColumn]
		public bool ismanaged => this.Get<bool>();

		[DataverseColumn]
		public int iscustomizable => this.Get<int>();

		[DataverseColumn]
		public string name => this.Get<string>();

		[DataverseColumn]
		public string objecttypecode => this.Get<string>();

		[DataverseColumn]
		public string formxml
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public string formjson
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}
#pragma warning restore IDE1006 // Naming Styles



		public class Repository : IFormRepository
		{
			public async Task<List<Form>> GetMainFormByTableNameAsync(IOrganizationServiceAsync2 crm, string tableName)
			{
				var query = new QueryExpression("systemform");
				query.ColumnSet.AddColumns(DataverseColumnAttribute.GetFromClass<Form>());
				query.Criteria.AddCondition(nameof(Form.objecttypecode), ConditionOperator.Equal, tableName);
				query.Criteria.AddCondition(nameof(Form.type), ConditionOperator.Equal, (int)FormType.Main);
				query.AddOrder("formid", OrderType.Ascending);
				query.NoLock = true;

				var response = await crm.RetrieveMultipleAsync(query);
				var formList = response.Entities.Select(x => new Form(x)).ToList();
				return formList;
			}
		}
	}
}
