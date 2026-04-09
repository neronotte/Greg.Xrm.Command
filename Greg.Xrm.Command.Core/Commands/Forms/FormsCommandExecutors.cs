using Greg.Xrm.Command.Services.Output;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Forms
{
	public class FormsListCommandExecutor(
		IOutput output) : ICommandExecutor<FormsListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(FormsListCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine($"Microsoft Forms List — Tenant: {command.TenantId}", ConsoleColor.Cyan);
			output.WriteLine($"  Owner: {command.OwnerId ?? "(current user)"}");
			output.WriteLine($"  Format: {command.Format}");
			output.WriteLine();
			output.WriteLine("Note: Forms API access requires OAuth2 authentication.", ConsoleColor.Yellow);
			output.WriteLine("  User-owned forms: Client Credentials flow (Application permissions)");
			output.WriteLine("  Group-owned forms: ROPC flow (requires MFA-excluded service account)");
			output.WriteLine();
			output.WriteLine("API endpoint: https://forms.office.com/formapi/api/{tenantId}/users/{ownerId}/light/forms");
			output.WriteLine();
			output.WriteLine("To authenticate:");
			output.WriteLine("  1. Register an Azure AD App Registration");
			output.WriteLine("  2. Grant Forms.Read.All (Application) permission");
			output.WriteLine("  3. Set MSAL_CLIENT_ID and MSAL_CLIENT_SECRET environment variables");
			output.WriteLine("  4. Run: pacx forms list --tenant contoso.onmicrosoft.com");

			return CommandResult.Success();
		}
	}

	public class FormsResponseCountCommandExecutor(
		IOutput output) : ICommandExecutor<FormsResponseCountCommand>
	{
		public async Task<CommandResult> ExecuteAsync(FormsResponseCountCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine($"Form Response Count — {command.FormId}", ConsoleColor.Cyan);
			output.WriteLine($"  Tenant: {command.TenantId}");
			output.WriteLine($"  Owner: {command.OwnerId ?? "(current user)"}");
			output.WriteLine();
			output.WriteLine("Note: Requires OAuth2 authentication.", ConsoleColor.Yellow);
			output.WriteLine("API: GET /formapi/api/{tenantId}/users/{ownerId}/light/forms('{formId}')?$select=rowCount");

			return CommandResult.Success();
		}
	}

	public class FormsResponsesExportCommandExecutor(
		IOutput output) : ICommandExecutor<FormsResponsesExportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(FormsResponsesExportCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine($"Forms Response Export — {command.FormId}", ConsoleColor.Cyan);
			output.WriteLine($"  Tenant: {command.TenantId}");
			output.WriteLine($"  Output: {command.OutputPath}");
			output.WriteLine($"  Format: {command.Format}");
			output.WriteLine($"  Incremental: {command.Incremental}");
			output.WriteLine();
			output.WriteLine("Note: Requires OAuth2 authentication.", ConsoleColor.Yellow);
			output.WriteLine("API: GET /formapi/api/{tenantId}/users/{ownerId}/light/forms('{formId}')/responses");
			output.WriteLine("  Supports $skip/$top for paged retrieval");

			if (command.Incremental)
			{
				output.WriteLine();
				output.WriteLine("Incremental mode: Will track last exported response offset.", ConsoleColor.Yellow);
			}

			return CommandResult.Success();
		}
	}
}
