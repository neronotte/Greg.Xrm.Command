using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Views;

namespace Greg.Xrm.Command.Commands.Views
{
	public class SetCommandExecutor(
		IOrganizationServiceRepository connection, IOutput output, IViewRetrieverService retriever,
		IPublishXmlBuilder publisher, IViewXmlEditor xmlEditor)
		: SetViewCommandExecutor<SetCommand>(connection, output, retriever, publisher, xmlEditor)
	{
		protected override (string FetchXml, string? LayoutXml, IReadOnlyList<string> UnusedAttributes) Edit(TableView view, SetCommand command)
		{
			return XmlEditor.SetView(command.FetchXml, command.LayoutXml, view.returnedtypecode);
		}
	}
}
