using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Views;

namespace Greg.Xrm.Command.Commands.Views
{
	public class SetFetchXmlCommandExecutor(
		IOrganizationServiceRepository connection, IOutput output, IViewRetrieverService retriever,
		IPublishXmlBuilder publisher, IViewXmlEditor xmlEditor)
		: SetViewCommandExecutor<SetFetchXmlCommand>(connection, output, retriever, publisher, xmlEditor)
	{
		protected override (string FetchXml, string? LayoutXml, IReadOnlyList<string> UnusedAttributes) Edit(TableView view, SetFetchXmlCommand command)
		{
			var updated = XmlEditor.SetFetchXml(view.layoutxml, command.FetchXml, view.returnedtypecode);
			return (updated.FetchXml, updated.LayoutXml, Array.Empty<string>());
		}
	}
}
