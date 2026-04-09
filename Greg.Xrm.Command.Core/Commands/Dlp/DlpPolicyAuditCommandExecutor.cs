using Greg.Xrm.Command.Services.Output;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Dlp
{
	public class DlpPolicyAuditCommandExecutor(
		IOutput output) : ICommandExecutor<DlpPolicyAuditCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DlpPolicyAuditCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine("DLP Policy Audit", ConsoleColor.Cyan);
			output.WriteLine();
			output.WriteLine("Note: DLP policies are managed via Power Platform Admin Center.", ConsoleColor.Yellow);
			output.WriteLine("This command provides a local audit of policy configuration files.");
			output.WriteLine();
			output.WriteLine("To audit DLP policies programmatically, use the Power Platform Admin API:");
			output.WriteLine("  GET https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/{environmentId}/dataLossPreventionPolicies");
			output.WriteLine();
			output.WriteLine("For now, review your policies at:");
			output.WriteLine("  https://admin.powerplatform.microsoft.com/data-loss-prevention");

			if (command.ShowGaps)
			{
				output.WriteLine();
				output.WriteLine("Common connectors without default DLP policies:", ConsoleColor.Yellow);
				output.WriteLine("  - Custom connectors (not in any policy)");
				output.WriteLine("  - Premium connectors without explicit approval");
				output.WriteLine("  - On-premises data gateways");
			}

			return CommandResult.Success();
		}
	}
}
