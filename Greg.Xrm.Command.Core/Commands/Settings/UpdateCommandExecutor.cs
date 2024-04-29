using Greg.Xrm.Command.Commands.Settings.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Settings
{
	public class UpdateCommandExecutor : ICommandExecutor<UpdateCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISettingDefinitionRepository settingDefinitionRepository;

		public UpdateCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingDefinitionRepository settingDefinitionRepository
			)
		{
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
			this.settingDefinitionRepository = settingDefinitionRepository;
		}


		public async Task<CommandResult> ExecuteAsync(UpdateCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("DONE", ConsoleColor.Green);


			var settingDefinition = await this.settingDefinitionRepository.GetByUniqueNameAsync(crm, command.Name);
			if (settingDefinition == null)
			{
				return CommandResult.Fail($"No setting found with unique name <{command.Name}>.");
			}

			if (command.Description != null)
			{
				settingDefinition.description = command.Description;
			}
			if (command.DefaultValue != null)
			{
				settingDefinition.defaultvalue = command.DefaultValue;
			}
			if (command.ReleaseLevel != null)
			{
				settingDefinition.releaselevel = new OptionSetValue((int)command.ReleaseLevel);
			}
			if (command.OverridableLevel != null)
			{
				var newLevel = CreateCommandExecutor.ParseLevel(command.OverridableLevel ?? OverridableLevel.None);
				if (settingDefinition.overridablelevel?.Value != newLevel.Value)
				{
					settingDefinition.overridablelevel = new OptionSetValue(newLevel.Value);
				}

				var isOverridable = command.OverridableLevel != (int)OverridableLevel.None;
				settingDefinition.isoverridable = isOverridable;
			}
			if (command.InformationUrl != null)
			{
				settingDefinition.informationurl = command.InformationUrl;
			}


			try
			{
				this.output.Write($"Updating setting {command.Name}...");

				await settingDefinition.SaveOrUpdateAsync(crm);

				this.output.WriteLine("DONE", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				this.output.WriteLine(ex.Message, ConsoleColor.Red);

				return CommandResult.Fail("Error updating setting metadata: " + ex.Message, ex);
			}
		}
	}
}
