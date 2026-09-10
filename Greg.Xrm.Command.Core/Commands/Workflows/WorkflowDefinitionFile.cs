using System.Text.Encodings.Web;
using System.Text.Json;

namespace Greg.Xrm.Command.Commands.Workflows
{
	/// <summary>
	/// Reads a flow definition from a json file, shared by the create and update commands.
	/// The definition may come indented from "pacx workflow get" or from an editor,
	/// it is returned compacted the same way the flow designer saves it.
	/// </summary>
	static class WorkflowDefinitionFile
	{
		public sealed class ReadResult
		{
			public string? ClientData { get; init; }
			public bool LooksLikeAFlowDefinition { get; init; }
			public string? Error { get; init; }
		}

		public static async Task<ReadResult> ReadAsync(string definitionFile, CancellationToken cancellationToken)
		{
			string raw;
			try
			{
				var fullPath = Path.GetFullPath(definitionFile);
				if (!File.Exists(fullPath))
				{
					return new ReadResult { Error = $"The definition file <{definitionFile}> does not exist." };
				}

				raw = await File.ReadAllTextAsync(fullPath, cancellationToken);
			}
			catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException or UnauthorizedAccessException)
			{
				return new ReadResult { Error = $"Unable to read the definition file <{definitionFile}>: {ex.Message}" };
			}

			try
			{
				using var document = JsonDocument.Parse(raw);

				var looksLikeAFlowDefinition = document.RootElement.ValueKind == JsonValueKind.Object
					&& document.RootElement.TryGetProperty("properties", out var properties)
					&& properties.ValueKind == JsonValueKind.Object
					&& properties.TryGetProperty("definition", out _);

				return new ReadResult
				{
					ClientData = JsonSerializer.Serialize(document, compactOptions),
					LooksLikeAFlowDefinition = looksLikeAFlowDefinition
				};
			}
			catch (JsonException ex)
			{
				return new ReadResult { Error = $"The definition file <{definitionFile}> does not contain valid json: {ex.Message}" };
			}
		}


		/// <summary>
		/// Keeps apostrophes and non ascii characters as they are, the same way
		/// the flow designer stores them.
		/// </summary>
		private static readonly JsonSerializerOptions compactOptions = new()
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};
	}
}
