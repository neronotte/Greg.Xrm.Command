using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Greg.Xrm.Command.Commands.Mcp
{
	public class McpStartCommandExecutor(
		IOutput output,
		ICommandRegistry commandRegistry,
		ICommandExecutorFactory executorFactory) : ICommandExecutor<McpStartCommand>
	{
		public async Task<CommandResult> ExecuteAsync(McpStartCommand command, CancellationToken cancellationToken)
		{
			output.WriteLine("Starting MCP Server...");

			var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
			services.AddLogging(l => l.AddConsole());
			var builder = services.AddMcpServer(options =>
			{
				options.ServerInfo = new Implementation
				{
					Name = "PACX MCP Server",
					Version = "1.0.0"
				};
			});

			// Map commands to tools
			var toolMap = commandRegistry.Commands
				.Where(c => !c.Hidden && c.Verbs[0] != "mcp")
				.ToDictionary(c => c.ExpandedVerbs.Replace(" ", "_"), c => c);

			builder.WithListToolsHandler((p, ct) =>
			{
				var tools = toolMap.Values.Select(c => c.ToMcpTool()).ToList();
				return ValueTask.FromResult(new ListToolsResult { Tools = tools });
			});

			builder.WithCallToolHandler(async (p, ct) =>
			{
				if (!toolMap.TryGetValue(p.Params.Name, out var definition))
				{
					throw new Exception($"Tool '{p.Params.Name}' not found.");
				}

				// Create command instance from arguments
				var options = new Dictionary<string, string>();
				if (p.Params.Arguments != null)
				{
					foreach (var prop in p.Params.Arguments)
					{
						options["--" + prop.Key] = prop.Value.ToString() ?? string.Empty;
					}
				}
				
				var cmdInstance = definition.CreateCommand(options);

				// Create executor and run
				var executor = executorFactory.CreateFor(definition.CommandType);
				if (executor == null)
				{
					throw new Exception($"Executor for '{p.Params.Name}' not found.");
				}

				var method = executor.GetType().GetMethod("ExecuteAsync");
				if (method == null)
				{
					throw new Exception($"ExecuteAsync method not found on executor.");
				}

				var resultTask = (Task<CommandResult>)method.Invoke(executor, new object[] { cmdInstance, ct })!;
				var result = await resultTask;

				var responseContent = new List<ContentBlock>();
				if (result.IsSuccess)
				{
					responseContent.Add(new TextContentBlock { Text = "Command executed successfully." });
				}
				else
				{
					responseContent.Add(new TextContentBlock { Text = "Error: " + result.ErrorMessage });
				}
				
				return new CallToolResult
				{
					Content = responseContent,
					IsError = !result.IsSuccess
				};
			});

			builder.WithStdioServerTransport();

			var serviceProvider = services.BuildServiceProvider();
			var server = serviceProvider.GetRequiredService<ModelContextProtocol.Server.McpServer>();
			await server.RunAsync(cancellationToken);

			return CommandResult.Success();
		}
	}
}
