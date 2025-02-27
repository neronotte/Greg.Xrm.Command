
using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using System.Xml.Linq;

namespace Greg.Xrm.Command.Commands.Views
{
	public class GetCommandExecutor(
		IOrganizationServiceRepository organizationServiceRepository,
		IOutput output,
		IViewRetrieverService viewRetriever
	)
	: ICommandExecutor<GetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var (result, view) = await viewRetriever.GetByNameAsync(crm, command.QueryType, command.ViewName, command.TableName);
			if (view == null) return result;


			if (!string.IsNullOrWhiteSpace(view.layoutxml)) {

				output.WriteLine()
					.WriteLine("--- LayoutXml ---------------------------------------------")
					.WriteLine();
				try
				{
					output.WriteLine(XDocument.Parse(view.layoutxml));
				}
				catch(Exception ex)
				{
					output.WriteLine(ex.ToString());

					output.WriteLine(view.layoutxml, ConsoleColor.Red);
				}
			}

			if (!string.IsNullOrWhiteSpace(view.fetchxml))
			{
				output.WriteLine()
					.WriteLine("--- FetchXml ----------------------------------------------")
					.WriteLine();
				try
				{
					output.WriteLine(XDocument.Parse(view.fetchxml));
				}
				catch
				{
					output.WriteLine(view.fetchxml, ConsoleColor.Red);
				}
			}

			return CommandResult.Success();
		}
	}
}
