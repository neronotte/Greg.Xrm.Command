using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	public interface IPublishXmlBuilder
	{
		void Clear();

		void AddWebResource(Guid id);

		PublishXmlRequest? Build();
	}
}