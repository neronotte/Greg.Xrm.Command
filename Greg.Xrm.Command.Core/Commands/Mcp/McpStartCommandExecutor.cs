using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Mcp
{
	public class McpStartCommandExecutor(
		IOutput output,
		IReadOnlyCommandRegistry commandRegistry) : ICommandExecutor<McpStartCommand>
	{
		public async Task<CommandResult> ExecuteAsync(McpStartCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.WriteLine("Starting MCP Server...", ConsoleColor.Cyan);
				output.WriteLine($"  Transport: {command.Transport}");
				output.WriteLine($"  Port: {command.Port}");
				output.WriteLine($"  Host: {command.Host}");
				output.WriteLine();

				var tools = BuildToolDefinitions(commandRegistry);
				output.WriteLine($"Discovered {tools.Count} PACX commands as MCP tools.", ConsoleColor.Green);

				switch (command.Transport.ToLower())
				{
					case "http":
						output.WriteLine($"Starting HTTP transport on {command.Host}:{command.Port}...", ConsoleColor.Yellow);
						output.WriteLine("Note: HTTP transport requires ASP.NET Core hosting setup.");
						break;

					case "stdio":
					default:
						output.WriteLine("Starting STDIO transport...", ConsoleColor.Green);
						var handler = new McpServerHandler(commandRegistry, new Storage());
						await handler.RunAsync(cancellationToken);
						break;
				}

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Error starting MCP server: {ex.Message}", ex);
			}
		}

		private static List<McpToolDefinition> BuildToolDefinitions(IReadOnlyCommandRegistry registry)
		{
			var tools = new List<McpToolDefinition>();

			foreach (var command in registry.Commands)
			{
				var tool = new McpToolDefinition
				{
					Name = string.Join("_", command.Verbs),
					Description = command.HelpText ?? $"PACX command: {string.Join(" ", command.Verbs)}",
					InputSchema = new McpInputSchema
					{
						Type = "object",
						Properties = new Dictionary<string, McpPropertyDefinition>(),
						Required = new List<string>(),
					},
				};

				// Convert command options to JSON Schema properties
				foreach (var option in command.Options)
				{
					var propName = option.Option.LongName.TrimStart('-').Replace("-", "_");
					var isBool = option.Property.PropertyType == typeof(bool) || option.Property.PropertyType == typeof(bool?);
					var propDef = new McpPropertyDefinition
					{
						Type = isBool ? "boolean" : "string",
						Description = option.Option.HelpText ?? "",
					};

					tool.InputSchema.Properties[propName] = propDef;

					if (option.IsRequired)
					{
						tool.InputSchema.Required.Add(propName);
					}
				}

				tools.Add(tool);
			}

			return tools;
		}
	}

	// === MCP JSON-RPC Models ===

	public class McpToolDefinition
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = "";

		[JsonPropertyName("description")]
		public string Description { get; set; } = "";

		[JsonPropertyName("inputSchema")]
		public McpInputSchema InputSchema { get; set; } = new();
	}

	public class McpInputSchema
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = "object";

		[JsonPropertyName("properties")]
		public Dictionary<string, McpPropertyDefinition> Properties { get; set; } = new();

		[JsonPropertyName("required")]
		public List<string> Required { get; set; } = new();
	}

	public class McpPropertyDefinition
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = "string";

		[JsonPropertyName("description")]
		public string Description { get; set; } = "";
	}

	public class McpJsonRpcRequest
	{
		[JsonPropertyName("jsonrpc")]
		public string JsonRpc { get; set; } = "2.0";

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; } = "";

		[JsonPropertyName("params")]
		public JsonElement? Params { get; set; }
	}

	public class McpJsonRpcResponse
	{
		[JsonPropertyName("jsonrpc")]
		public string JsonRpc { get; set; } = "2.0";

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("result")]
		public object? Result { get; set; }

		[JsonPropertyName("error")]
		public McpError? Error { get; set; }
	}

	public class McpError
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; } = "";
	}
}
