using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Settings.Model
{
    public class AppSetting : EntityWrapper
	{
		protected AppSetting(Entity entity) : base(entity)
		{
		}

		public AppSetting() : base("appsetting")
		{
		}

		[DataverseColumn]
		public EntityReference parentappmoduleid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}

		[DataverseColumn]
		public EntityReference settingdefinitionid
		{
			get => Get<EntityReference>();
			set => SetValue(value);
		}

		[DataverseColumn]
		public string value
		{
			get => Get<string>();
			set => SetValue(value);
		}


		public string? FormattedAppModule => GetFormatted(nameof(parentappmoduleid));

		public class Repository : IAppSettingRepository
		{
			private readonly IOutput output;

			public Repository(IOutput output)
            {
				this.output = output;
			}

			public async Task<IReadOnlyList<AppSetting>> GetByDefinitionsAsync(IOrganizationServiceAsync2 crm, IReadOnlyList<SettingDefinition> settingDefinitionList)
			{
				if (settingDefinitionList == null || settingDefinitionList.Count == 0) return Array.Empty<AppSetting>();

				this.output.Write("Retrieving settings app values...");

				var query = new QueryExpression("appsetting");
				query.ColumnSet.AddColumns(DataverseColumnAttribute.GetFromClass<AppSetting>());
				query.Criteria.AddCondition(nameof(settingdefinitionid), ConditionOperator.In, settingDefinitionList.Select(x => x.Id).Cast<object>().ToArray());
				query.NoLock = true;

				var response = await crm.RetrieveMultipleAsync(query);
				var settings = response.Entities.Select(e => new AppSetting(e)).ToList();

				this.output.WriteLine("Done", ConsoleColor.Green);

				return settings;
			}
		}
	}
}
