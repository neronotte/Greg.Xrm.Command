using System.Xml.Linq;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Commands.Org
{
	public class InfoCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
		) : ICommandExecutor<InfoCommand>
	{
		public async Task<CommandResult> ExecuteAsync(InfoCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			OrganizationDetail details;
			Entity org;
			output.Write("Retrieving organization details...");
			try
			{
				var request = new RetrieveCurrentOrganizationRequest();
				var response = (RetrieveCurrentOrganizationResponse)await crm.ExecuteAsync(request);

				details = response.Detail;

				var query = new QueryExpression("organization");
				query.ColumnSet.AllColumns = true;
				query.TopCount = 1;

				org = (await crm.RetrieveMultipleAsync(query)).Entities[0];

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Failed to retrieve organization details", ex);
			}


			output.WriteLine();
			output.WriteLine("=== Details: ".PadRight(100, '='), ConsoleColor.Cyan);
			output.WriteLine();
			output.WriteLine(JsonConvert.SerializeObject(details, Formatting.Indented));


			output.WriteLine();
			output.WriteLine("=== Data: ".PadRight(100, '='), ConsoleColor.Cyan);
			output.WriteLine();

			var attributesToShowInTable = org.Attributes
				.Where(x => x.Key != "orgdborgsettings")
				.Where(x => x.Key != "kmsettings")
				.Where(x => x.Key != "defaultemailsettings")
				.Where(x => x.Key != "sitemapxml")
				.Where(x => x.Key != "referencesitemapxml")
				.Where(x => x.Key != "highcontrastthemedata")
				.Where(x => x.Key != "defaultthemedata")
				.Where(x => x.Key != "blockedattachments")
				.OrderBy(x => x.Key)
				.ToList();

			output.WriteTable(attributesToShowInTable,
				() => ["Attribute", "Value"],
				x => [x.Key, org.GetFormattedValue(x.Key) ?? string.Empty]
				);

			var orgSettings = org.GetAttributeValue<string>("orgdborgsettings");
			if (!string.IsNullOrWhiteSpace(orgSettings))
			{
				var doc = XDocument.Parse(orgSettings);

				output.WriteLine();
				output.WriteLine("=== Org Settings: ".PadRight(100, '='), ConsoleColor.Cyan);
				output.WriteLine();
				output.WriteLine(doc.ToString());
			}


			var kmsettings = org.GetAttributeValue<string>("kmsettings");
			if (!string.IsNullOrWhiteSpace(kmsettings))
			{
				var doc = XDocument.Parse(kmsettings);

				output.WriteLine();
				output.WriteLine("=== KM Settings: ".PadRight(100, '='), ConsoleColor.Cyan);
				output.WriteLine();
				output.WriteLine(doc.ToString());
			}


			var defaultemailsettings = org.GetAttributeValue<string>("defaultemailsettings");
			if (!string.IsNullOrWhiteSpace(defaultemailsettings))
			{
				var doc = XDocument.Parse(defaultemailsettings);

				output.WriteLine();
				output.WriteLine("=== Default Email Settings: ".PadRight(100, '='), ConsoleColor.Cyan);
				output.WriteLine();
				output.WriteLine(doc.ToString());
			}


			var blockedAttachments = org.GetAttributeValue<string>("blockedattachments");
			if (!string.IsNullOrWhiteSpace(blockedAttachments))
			{
				output.WriteLine();
				output.WriteLine("=== Blocked Attachments: ".PadRight(100, '='), ConsoleColor.Cyan);
				output.WriteLine();

				var items = blockedAttachments.Split(";").OrderBy(x => x).ToList();
				foreach (var item in items)
				{
					output.WriteLine("  ." + item);
				}
			}

			return CommandResult.Success();
		}
	}
}
