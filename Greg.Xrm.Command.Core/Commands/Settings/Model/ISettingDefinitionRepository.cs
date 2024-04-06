using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Settings.Model
{
	public interface ISettingDefinitionRepository
	{
		Task<IReadOnlyList<SettingDefinition>> GetAllAsync(IOrganizationServiceAsync2 crm, Guid? solutionId, bool onlyVisible);
	}
}
