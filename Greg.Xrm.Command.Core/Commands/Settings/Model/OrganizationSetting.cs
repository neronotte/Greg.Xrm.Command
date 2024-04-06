using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Settings.Model
{
	public class OrganizationSetting : EntityWrapper
	{
		public OrganizationSetting(Entity entity) : base(entity)
		{
		}

		public OrganizationSetting() : base("organizationsetting")
		{
		}


		[DataverseColumn]
		public EntityReference? settingdefinitionid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}

		[DataverseColumn]
		public string? uniquename
		{
			get => Get<string>();
			set => SetValue(value);
		}

		[DataverseColumn]
		public string? value
		{
			get => Get<string>();
			set => SetValue(value);
		}


		public override string ToString()
		{
			return $"{uniquename}: {value}";
		}


		public class Repository : IOrganizationSettingRepository
		{
			private readonly IOutput output;

			public Repository(IOutput output)
            {
				this.output = output;
			}

			public async Task<IReadOnlyList<OrganizationSetting>> GetByDefinitionsAsync(IOrganizationServiceAsync2 crm, IReadOnlyList<SettingDefinition> settingDefinitionList)
			{
				if (settingDefinitionList == null || settingDefinitionList.Count == 0) return Array.Empty<OrganizationSetting>();

				this.output.Write("Retrieving settings org. values...");

				var query = new QueryExpression("organizationsetting");
				query.ColumnSet.AddColumns(DataverseColumnAttribute.GetFromClass<OrganizationSetting>());
				query.Criteria.AddCondition(nameof(settingdefinitionid), ConditionOperator.In, settingDefinitionList.Select(x => x.Id).Cast<object>().ToArray());
				query.NoLock = true;

				var response = await crm.RetrieveMultipleAsync(query);
				var settings = response.Entities.Select(e => new OrganizationSetting(e)).ToList();

				this.output.WriteLine("DONE", ConsoleColor.Green);

				return settings;
			}
		}


	}
}
