using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Plugin.Trace
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.Write("Retrieving plugin trace logs...");

				var q = new QueryExpression("plugintracelog")
				{
					NoLock = true,
					TopCount = command.Top
				};
				q.ColumnSet.AddColumns(
					"createdon", "typename", "messagename", "primaryentity", "mode", "depth",
					"performanceexecutionduration", "messageblock", "exceptiondetails");

				if (!string.IsNullOrWhiteSpace(command.TypeName))
					q.Criteria.AddCondition("typename", ConditionOperator.Like, $"%{command.TypeName}%");

				if (command.ErrorsOnly)
					q.Criteria.AddCondition("exceptiondetails", ConditionOperator.NotNull);

				q.AddOrder("createdon", OrderType.Descending);

				var results = await crm.RetrieveMultipleAsync(q, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				if (results.Entities.Count == 0)
				{
					output.WriteLine("No plugin trace log records found matching the specified criteria.", ConsoleColor.Yellow);
					output.WriteLine("Trace logs are written only when trace logging is enabled on the environment (organization setting 'plugintracelogsetting'), and records are deleted automatically after 24 hours.", ConsoleColor.DarkGray);
					return CommandResult.Success();
				}

				output.WriteLine();

				foreach (var row in results.Entities)
				{
					WriteTraceRecord(row);
				}

				var result = CommandResult.Success();
				result["Count"] = results.Entities.Count;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private void WriteTraceRecord(Entity row)
		{
			var createdOn = row.GetAttributeValue<DateTime?>("createdon");
			var typeName = row.GetAttributeValue<string>("typename") ?? string.Empty;
			var messageName = row.GetAttributeValue<string>("messagename") ?? string.Empty;
			var primaryEntity = row.GetAttributeValue<string>("primaryentity") ?? string.Empty;
			var mode = row.GetAttributeValue<OptionSetValue>("mode");
			var depth = row.GetAttributeValue<int?>("depth");
			var duration = row.GetAttributeValue<int?>("performanceexecutionduration");
			var messageBlock = row.GetAttributeValue<string>("messageblock");
			var exceptionDetails = row.GetAttributeValue<string>("exceptiondetails");

			output.Write(createdOn?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "(no date)", ConsoleColor.White);
			output.Write("  ");
			output.Write(TypeDisplayName(typeName), ConsoleColor.Cyan);
			output.WriteLine();

			var operation = string.IsNullOrWhiteSpace(primaryEntity) || primaryEntity == "none"
				? messageName
				: $"{messageName} on {primaryEntity}";
			var version = TypeVersion(typeName);
			var details = $"  {operation}  [{ModeLabel(mode)}, depth {depth?.ToString() ?? "?"}, {(duration.HasValue ? duration + " ms" : "n/a")}{(version == null ? string.Empty : ", v" + version)}]";
			output.WriteLine(details, ConsoleColor.DarkGray);

			WriteIndentedBlock(messageBlock, ConsoleColor.Gray);
			WriteIndentedBlock(exceptionDetails, ConsoleColor.Red);

			output.WriteLine();
		}

		private void WriteIndentedBlock(string? text, ConsoleColor color)
		{
			if (string.IsNullOrWhiteSpace(text)) return;

			foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
			{
				output.WriteLine("    " + line, color);
			}
		}

		// typename holds the assembly qualified name ("My.Plugin, MyAssembly, Version=1.0.0.0, ...").
		// Only the class name and the version are relevant when reading traces.
		private static string TypeDisplayName(string typeName)
		{
			var comma = typeName.IndexOf(',');
			return comma < 0 ? typeName : typeName[..comma].TrimEnd();
		}

		private static string? TypeVersion(string typeName)
		{
			foreach (var part in typeName.Split(','))
			{
				var trimmed = part.Trim();
				if (trimmed.StartsWith("Version=", StringComparison.OrdinalIgnoreCase))
					return trimmed["Version=".Length..];
			}
			return null;
		}

		private static string ModeLabel(OptionSetValue? mode) => mode?.Value switch
		{
			0 => "Sync",
			1 => "Async",
			_ => "Unknown"
		};
	}
}
