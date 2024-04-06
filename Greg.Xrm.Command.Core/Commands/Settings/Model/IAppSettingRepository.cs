using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Settings.Model
{
	public interface IAppSettingRepository
	{
		Task<IReadOnlyList<AppSetting>> GetByDefinitionsAsync(IOrganizationServiceAsync2 crm, IReadOnlyList<SettingDefinition> settingDefinitionList);
	}
}