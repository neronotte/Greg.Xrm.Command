using System.Xml.Linq;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Ribbon
{
	public interface IRibbonDiffService
	{
		Task<(Guid componentId, ComponentType componentType)> ResolveComponentAsync(IOrganizationServiceAsync2 crm, string tableName, CancellationToken cancellationToken);
		Task<string?> ValidateSolutionAsync(IOrganizationServiceAsync2 crm, Guid solutionId, string solutionName, Guid componentId, ComponentType componentType, CancellationToken cancellationToken);
		void ValidateXml(XElement ribbon, string tableName);
		XElement Read(byte[] solutionBytes, string tableName);
		byte[] Replace(byte[] solutionBytes, string tableName, XElement replacement);
	}
}
