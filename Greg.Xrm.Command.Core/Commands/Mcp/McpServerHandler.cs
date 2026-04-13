using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Mcp
{
	/// <summary>
	/// MCP server implementation using stdio transport.
	/// Implements the Model Context Protocol JSON-RPC specification.
	/// See: https://modelcontextprotocol.io/specification
	/// </summary>
	public class McpServerHandler
	{
		private readonly IReadOnlyCommandRegistry _commandRegistry;
		private readonly IStorage _storage;
		private readonly TextReader _input;
		private readonly TextWriter _output;
		private int _requestId;

		public McpServerHandler(
			IReadOnlyCommandRegistry commandRegistry,
			IStorage storage,
			TextReader? input = null,
			TextWriter? output = null)
		{
			_commandRegistry = commandRegistry;
			_storage = storage;
			_input = input ?? Console.In;
			_output = output ?? Console.Out;
			_requestId = 0;
		}

		/// <summary>
		/// Main server loop — reads JSON-RPC requests from stdin and writes responses to stdout.
		/// </summary>
		public async Task RunAsync(CancellationToken cancellationToken = default)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var line = await _input.ReadLineAsync(cancellationToken);
				if (string.IsNullOrWhiteSpace(line))
				{
					if (line == null) break; // EOF
					continue;
				}

				try
				{
					var request = JsonSerializer.Deserialize<McpJsonRpcRequest>(line);
					if (request == null) continue;

					var response = await HandleRequestAsync(request, cancellationToken);
					var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
					{
						WriteIndented = false,
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					});

					await _output.WriteLineAsync(jsonResponse);
					await _output.FlushAsync(cancellationToken);
				}
				catch (Exception ex)
				{
					var errorResponse = new McpJsonRpcResponse
					{
						Id = -1,
						Error = new McpError { Code = -32603, Message = $"Internal error: {ex.Message}" },
					};
					var json = JsonSerializer.Serialize(errorResponse);
					await _output.WriteLineAsync(json);
					await _output.FlushAsync(cancellationToken);
				}
			}
		}

		private async Task<McpJsonRpcResponse> HandleRequestAsync(McpJsonRpcRequest request, CancellationToken ct)
		{
			var response = new McpJsonRpcResponse { Id = request.Id };

			switch (request.Method)
			{
				case "initialize":
					response.Result = await HandleInitializeAsync(request.Params, ct);
					break;

				case "tools/list":
					response.Result = await HandleToolsListAsync(request.Params, ct);
					break;

				case "tools/call":
					response.Result = await HandleToolCallAsync(request.Params, ct);
					break;

				default:
					response.Error = new McpError
					{
						Code = -32601,
						Message = $"Method not found: {request.Method}",
					};
					break;
			}

			return response;
		}

		private Task<object> HandleInitializeAsync(JsonElement? @params, CancellationToken ct)
		{
			var result = new
			{
				protocolVersion = "2024-11-05",
				capabilities = new
				{
					tools = new
					{
						listChanged = false,
					},
				},
				serverInfo = new
				{
					name = "pacx-mcp",
					version = typeof(McpServerHandler).Assembly.GetName().Version?.ToString() ?? "1.0.0",
				},
			};
			return Task.FromResult((object)result);
		}

		private Task<object> HandleToolsListAsync(JsonElement? @params, CancellationToken ct)
		{
			var tools = BuildToolDefinitions(_commandRegistry);
			return Task.FromResult((object)new { tools });
		}

		private async Task<object> HandleToolCallAsync(JsonElement? @params, CancellationToken ct)
		{
			if (!@params.HasValue)
				throw new ArgumentException("Missing params for tools/call");

			var name = @params.Value.GetProperty("name").GetString() ?? "";
			var arguments = @params.Value.TryGetProperty("arguments", out var argsElement)
				? argsElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString())
				: new Dictionary<string, string>();

			// Find matching command
			var verbs = name.Split('_');
			var command = _commandRegistry.Commands.FirstOrDefault(c =>
				c.Verbs.SequenceEqual(verbs, StringComparer.OrdinalIgnoreCase));

			if (command == null)
			{
				return new
				{
					content = new[]
					{
						new { type = "text", text = $"Unknown tool: {name}" },
					},
					isError = true,
				};
			}

			// Build command arguments from MCP arguments
			var cmdArgs = new List<string>();
			cmdArgs.AddRange(verbs);

			foreach (var (key, value) in arguments)
			{
				if (!string.IsNullOrEmpty(value))
				{
					cmdArgs.Add($"--{key}");
					cmdArgs.Add(value);
				}
			}

			// Capture output
			var capturedOutput = new CapturedOutput();

			try
			{
				// Execute command via the registry's command runner pattern
				// For now, we return the tool definition info since full execution
				// requires DI container setup
				var result = new
				{
					content = new[]
					{
						new { type = "text", text = $"Command found: {command.ExpandedVerbs}\nHelp: {command.HelpText}\nOptions: {string.Join(", ", command.Options.Select(o => o.Option.LongName))}" },
					},
					isError = false,
				};

				return result;
			}
			catch (Exception ex)
			{
				return new
				{
					content = new[]
					{
						new { type = "text", text = $"Error executing {name}: {ex.Message}" },
					},
					isError = true,
				};
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

	/// <summary>
	/// Output capture implementation for MCP tool execution.
	/// </summary>
	public class CapturedOutput : IOutput
	{
		private readonly List<string> _lines = new();

		public IReadOnlyList<string> Lines => _lines.AsReadOnly();

		public string GetCapturedOutput() => string.Join("\n", _lines);

		public IOutput Write(object? text)
		{
			_lines.Add(text?.ToString() ?? "");
			return this;
		}

		public IOutput Write(object? text, ConsoleColor color)
		{
			_lines.Add(text?.ToString() ?? "");
			return this;
		}

		public IOutput WriteLine()
		{
			_lines.Add("");
			return this;
		}

		public IOutput WriteLine(object? text)
		{
			_lines.Add(text?.ToString() ?? "");
			return this;
		}

		public IOutput WriteLine(object? text, ConsoleColor color)
		{
			_lines.Add(text?.ToString() ?? "");
			return this;
		}

		public IOutput WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, TRow, ConsoleColor?>? colorPicker = null)
		{
			// Simple table serialization for MCP output
			var headers = rowHeaders();
			_lines.Add(string.Join(" | ", headers));
			_lines.Add(new string('-', headers.Sum(h => h.Length + 3) - 3));
			for (var i = 0; i < collection.Count; i++)
			{
				_lines.Add(string.Join(" | ", rowData(collection[i])));
			}
			return this;
		}
	}
}
