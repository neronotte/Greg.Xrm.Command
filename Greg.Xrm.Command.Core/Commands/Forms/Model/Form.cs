using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
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



		public class Repository(IOutput output) : IFormRepository
		{
			public async Task<(CommandResult, Form?)> GetByNameAsync(IOrganizationServiceAsync2 crm, string tableName, string? formName, bool announce = true)
			{
				output.Write($"Retrieving main form of table {tableName}...");
				var forms = await GetMainFormByTableNameAsync(crm, tableName);
				output.WriteLine("Done", ConsoleColor.Green);

				if (forms.Count == 0)
					return (CommandResult.Fail($"No main form found for table {tableName}"), null);

				if (!string.IsNullOrWhiteSpace(formName))
				{
					forms = forms.Where(form => string.Equals(form.name, formName, StringComparison.OrdinalIgnoreCase)).ToList();
					if (forms.Count == 0)
						return (CommandResult.Fail($"Main form <{formName}> not found for table <{tableName}>"), null);
				}
				else if (forms.Count > 1)
				{
					return (CommandResult.Fail($"Table <{tableName}> has more than one main form. Please specify the form name using the --form parameter."), null);
				}

				if (forms.Count > 1)
					return (CommandResult.Fail($"Table <{tableName}> has more than one main form called <{formName}>. Please change the name of the form to uniquely identify it."), null);

				var form = forms[0];
				if (announce) output.WriteLine($"Main form found: {form.name}");
				return (CommandResult.Success(), form);
			}

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
