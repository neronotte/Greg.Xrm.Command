using Greg.Xrm.Command.Commands.Settings.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Settings
{
	public class ListCommandExecutor : ICommandExecutor<ListCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISolutionRepository solutionRepository;
		private readonly ISettingDefinitionRepository settingRepository;
		private readonly IOrganizationSettingRepository organizationSettingRepository;
		private readonly IAppSettingRepository appSettingRepository;

		public ListCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			ISettingDefinitionRepository settingRepository,
			IOrganizationSettingRepository organizationSettingRepository,
			IAppSettingRepository appSettingRepository)
        {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
			this.solutionRepository = solutionRepository ?? throw new ArgumentNullException(nameof(solutionRepository));
			this.settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
			this.organizationSettingRepository = organizationSettingRepository ?? throw new ArgumentNullException(nameof(organizationSettingRepository));
			this.appSettingRepository = appSettingRepository ?? throw new ArgumentNullException(nameof(appSettingRepository));
		}


        public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("DONE", ConsoleColor.Green);

			Guid? solutionId = null;
			if (command.Origin == Origin.Solution)
			{
				var defaultSolutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(defaultSolutionName))
				{
					defaultSolutionName = await this.organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (string.IsNullOrWhiteSpace(defaultSolutionName))
					{
						return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
					}
				}

				var solution = await this.solutionRepository.GetByUniqueNameAsync(crm, defaultSolutionName);
				if (solution == null)
				{
					return CommandResult.Fail($"No solution found with uniquename <{defaultSolutionName}>.");
				}

				solutionId = solution.Id;
			}

			var onlyVisible = command.Filter == Which.Visible;
			var settingList = await this.settingRepository.GetAllAsync(crm, solutionId, onlyVisible);
			if (settingList.Count == 0)
			{
				this.output.WriteLine("No setting found!", ConsoleColor.Yellow);
				return CommandResult.Success();
			}

			var organizationSettingList = await this.organizationSettingRepository.GetByDefinitionsAsync(crm, settingList);

			var appSettingList = await this.appSettingRepository.GetByDefinitionsAsync(crm, settingList);

			var appList = appSettingList
				.Select(x => x.parentappmoduleid)
				.Where(x => x != null)
				.DistinctBy(x => x.Id)
				.OrderBy(x => x.Name)
				.ToList();
			
			this.output.WriteLine();
			this.output.WriteTable(
				settingList,
				rowHeaders: () => new [] { "Unique Name", "Default Value", "Env. Value", "Type", "Visible", "Overridable on" },
				rowData: setting => new [] { 
					setting.uniquename, 
					setting.defaultvalue?.Trim() ?? string.Empty,
					organizationSettingList.FirstOrDefault(x => x.settingdefinitionid?.Id == setting.Id)?.value ?? string.Empty,
					setting.FormattedDataType ?? string.Empty, 
					setting.ishidden ? string.Empty : "X",
					setting.FormattedOverridableLevel ?? string.Empty});

			return CommandResult.Success();
		}
	}
}
