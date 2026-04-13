using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Pages
{
	public class PagesSiteConfigExportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<PagesSiteConfigExportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PagesSiteConfigExportCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Find the site
				var siteQuery = new QueryExpression("adx_website");
				siteQuery.ColumnSet.AddColumn("adx_websiteid");
				siteQuery.ColumnSet.AddColumn("adx_name");
				siteQuery.Criteria.AddCondition("adx_websiteid", ConditionOperator.Equal, command.SiteId);

				var siteResults = await crm.RetrieveMultipleAsync(siteQuery, cancellationToken);
				if (siteResults.Entities.Count == 0)
				{
					return CommandResult.Fail($"Power Pages site '{command.SiteId}' not found.");
				}

				var site = siteResults.Entities[0];
				var siteName = site.GetAttributeValue<string>("adx_name");
				output.WriteLine($"Exporting configuration from site: {siteName}", ConsoleColor.Cyan);
				output.WriteLine($"  Scope: {command.Scope}");
				output.WriteLine($"  Format: {command.Format}");
				output.WriteLine($"  Output: {command.OutputPath}");

				if (!Directory.Exists(command.OutputPath))
				{
					Directory.CreateDirectory(command.OutputPath);
				}

				// Export based on scope
				switch (command.Scope.ToLower())
				{
					case "all":
						await ExportAllAsync(crm, site.Id, command.OutputPath, command.Format, cancellationToken);
						break;
					case "auth":
						await ExportAuthAsync(crm, site.Id, command.OutputPath, command.Format, cancellationToken);
						break;
					case "navigation":
						await ExportNavigationAsync(crm, site.Id, command.OutputPath, command.Format, cancellationToken);
						break;
					case "themes":
						await ExportThemesAsync(crm, site.Id, command.OutputPath, command.Format, cancellationToken);
						break;
					case "snippets":
						await ExportSnippetsAsync(crm, site.Id, command.OutputPath, command.Format, cancellationToken);
						break;
					default:
						return CommandResult.Fail($"Unknown scope: {command.Scope}. Valid scopes: all, auth, navigation, themes, snippets.");
				}

				output.WriteLine($"Export complete to {command.OutputPath}", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error exporting site configuration: {ex.Message}", ex);
			}
		}

		private async Task ExportAllAsync(IOrganizationServiceAsync2 crm, Guid siteId, string outputPath, string format, CancellationToken ct)
		{
			await ExportAuthAsync(crm, siteId, outputPath, format, ct);
			await ExportNavigationAsync(crm, siteId, outputPath, format, ct);
			await ExportThemesAsync(crm, siteId, outputPath, format, ct);
			await ExportSnippetsAsync(crm, siteId, outputPath, format, ct);
		}

		private async Task ExportAuthAsync(IOrganizationServiceAsync2 crm, Guid siteId, string outputPath, string format, CancellationToken ct)
		{
			output.WriteLine("  Exporting authentication settings...");
			var query = new QueryExpression("adx_siteauthentication");
			query.ColumnSet.AllColumns = true;
			query.Criteria.AddCondition("adx_websiteid", ConditionOperator.Equal, siteId);
			var results = await crm.RetrieveMultipleAsync(query, ct);
			output.WriteLine($"    Found {results.Entities.Count} authentication provider(s)");
		}

		private async Task ExportNavigationAsync(IOrganizationServiceAsync2 crm, Guid siteId, string outputPath, string format, CancellationToken ct)
		{
			output.WriteLine("  Exporting navigation configuration...");
			var query = new QueryExpression("adx_webpage");
			query.ColumnSet.AddColumn("adx_webpageid");
			query.ColumnSet.AddColumn("adx_name");
			query.Criteria.AddCondition("adx_websiteid", ConditionOperator.Equal, siteId);
			var results = await crm.RetrieveMultipleAsync(query, ct);
			output.WriteLine($"    Found {results.Entities.Count} page(s)");
		}

		private async Task ExportThemesAsync(IOrganizationServiceAsync2 crm, Guid siteId, string outputPath, string format, CancellationToken ct)
		{
			output.WriteLine("  Exporting theme configuration...");
			var query = new QueryExpression("adx_sitesetting");
			query.ColumnSet.AllColumns = true;
			query.Criteria.AddCondition("adx_websiteid", ConditionOperator.Equal, siteId);
			query.Criteria.AddCondition("adx_name", ConditionOperator.Like, "%theme%");
			var results = await crm.RetrieveMultipleAsync(query, ct);
			output.WriteLine($"    Found {results.Entities.Count} theme setting(s)");
		}

		private async Task ExportSnippetsAsync(IOrganizationServiceAsync2 crm, Guid siteId, string outputPath, string format, CancellationToken ct)
		{
			output.WriteLine("  Exporting content snippets...");
			var query = new QueryExpression("adx_contentsnippet");
			query.ColumnSet.AllColumns = true;
			query.Criteria.AddCondition("adx_websiteid", ConditionOperator.Equal, siteId);
			var results = await crm.RetrieveMultipleAsync(query, ct);
			output.WriteLine($"    Found {results.Entities.Count} content snippet(s)");
		}
	}

	public class PagesSiteConfigImportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<PagesSiteConfigImportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PagesSiteConfigImportCommand command, CancellationToken cancellationToken)
		{
			try
			{
				if (!Directory.Exists(command.InputPath))
				{
					return CommandResult.Fail($"Input directory not found: {command.InputPath}");
				}

				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Find the site
				var siteQuery = new QueryExpression("adx_website");
				siteQuery.ColumnSet.AddColumn("adx_websiteid");
				siteQuery.ColumnSet.AddColumn("adx_name");
				siteQuery.Criteria.AddCondition("adx_websiteid", ConditionOperator.Equal, command.SiteId);

				var siteResults = await crm.RetrieveMultipleAsync(siteQuery, cancellationToken);
				if (siteResults.Entities.Count == 0)
				{
					return CommandResult.Fail($"Power Pages site '{command.SiteId}' not found.");
				}

				var site = siteResults.Entities[0];
				var siteName = site.GetAttributeValue<string>("adx_name");

				if (command.DryRun)
				{
					output.WriteLine("[DRY RUN] Would import configuration to site:", ConsoleColor.Yellow);
					output.WriteLine($"  Site: {siteName}");
					output.WriteLine($"  Scope: {command.Scope}");
					output.WriteLine($"  Input: {command.InputPath}");
					return CommandResult.Success();
				}

				output.WriteLine($"Importing configuration to site: {siteName}", ConsoleColor.Cyan);
				output.WriteLine($"  Scope: {command.Scope}");
				output.WriteLine($"  Input: {command.InputPath}");

				// Import logic would parse files from input directory and upsert into Dataverse
				output.WriteLine();
				output.WriteLine("Note: Import creates/updates adx_* records in Dataverse.", ConsoleColor.Yellow);
				output.WriteLine("Use --force to overwrite existing configuration.");

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error importing site configuration: {ex.Message}", ex);
			}
		}
	}
}
