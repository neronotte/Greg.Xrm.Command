using Greg.Xrm.Command.Commands.Table.ExportMetadata;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Settings.Model
{
	public class SettingDefinition : EntityWrapper
	{
		protected SettingDefinition(Entity entity) : base(entity)
		{
		}

		public SettingDefinition() : base("settingdefinition")
		{
		}



		[DataverseColumn]
		public string uniquename
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public string displayname
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public string? description
		{
			get => this.Get<string?>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public OptionSetValue datatype
		{
			get => this.Get<OptionSetValue>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public string? defaultvalue
		{
			get => this.Get<string?>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public string? informationurl
		{
			get => this.Get<string?>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public bool ishidden => this.Get<bool>();

		[DataverseColumn]
		public bool isplatform => this.Get<bool>();

		[DataverseColumn]
		public bool isoverridable
		{
			get => this.Get<bool>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public OptionSetValue overridablelevel
		{
			get => this.Get<OptionSetValue>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public OptionSetValue releaselevel
		{
			get => this.Get<OptionSetValue>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public OptionSetValue statecode
		{
			get => this.Get<OptionSetValue>();
			set => this.SetValue(value);
		}

		[DataverseColumn]
		public OptionSetValue statuscode
		{
			get => this.Get<OptionSetValue>();
			set => this.SetValue(value);
		}



		public string? FormattedDataType => this.GetFormatted(nameof(datatype));
		public string? FormattedReleaseLevel => this.GetFormatted(nameof(releaselevel));
		public string? FormattedOverridableLevel => !this.isoverridable ? "None" : this.GetFormatted(nameof(overridablelevel));
		public string? FormattedStateCode => this.GetFormatted(nameof(statecode));
		public string? FormattedStatusCode => this.GetFormatted(nameof(statuscode));


		public override string ToString()
		{
			return this.uniquename;
		}

		public class Repository : ISettingDefinitionRepository
		{
			private readonly IOutput output;

			public Repository(IOutput output)
            {
				this.output = output;
			}


			public async Task<IReadOnlyList<SettingDefinition>> GetAllAsync(IOrganizationServiceAsync2 crm, Guid? solutionId, bool onlyVisible)
			{
				this.output.Write("Retrieving settings...");

				var query = new QueryExpression("settingdefinition");
				query.ColumnSet.AddColumns(DataverseColumnAttribute.GetFromClass<SettingDefinition>());
				query.AddOrder(nameof(uniquename), OrderType.Ascending);
				query.NoLock = true;

				if (onlyVisible)
				{
					query.Criteria.AddCondition(nameof(ishidden), ConditionOperator.Equal, false);
				}

				if (solutionId != null)
				{
					var solutionComponentLink = query.AddLink("solutioncomponent", "settingdefinitionid", "objectid");
					solutionComponentLink.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
				}

				var result = await crm.RetrieveMultipleAsync(query);

				var settings = result.Entities.Select(e => new SettingDefinition(e)).ToList();

				this.output.WriteLine("DONE", ConsoleColor.Green);
				return settings;
			}

			public async Task<SettingDefinition?> GetByUniqueNameAsync(IOrganizationServiceAsync2 crm, string uniqueName)
			{
				this.output.Write($"Retrieving setting {uniqueName}...");

				var query = new QueryExpression("settingdefinition");
				query.ColumnSet.AddColumns(DataverseColumnAttribute.GetFromClass<SettingDefinition>());
				query.Criteria.AddCondition(nameof(uniquename), ConditionOperator.Equal, uniqueName);
				query.AddOrder(nameof(uniquename), OrderType.Ascending);
				query.NoLock = true;
				query.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(query);

				var settings = result.Entities.Select(e => new SettingDefinition(e)).ToList();

				this.output.WriteLine("DONE", ConsoleColor.Green);
				return settings.FirstOrDefault();
			}
		}
	}
}
