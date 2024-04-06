using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Settings.Model
{
	public interface IOrganizationSettingRepository
	{
		Task<IReadOnlyList<OrganizationSetting>> GetByDefinitionsAsync(IOrganizationServiceAsync2 crm, IReadOnlyList<SettingDefinition> settingDefinitionList);
	}
}
