using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Greg.Xrm.Command.Commands.Workflows
{
	/// <summary>
	/// Helpers to inspect a flow clientdata json. Only the stable base grammar of the
	/// workflow definition language is checked (the schema version referenced by every
	/// flow definition is frozen since 2016), everything deeper is left to the flow engine.
	/// </summary>
	public static class WorkflowClientData
	{
		/// <summary>
		/// Manual triggers only fire on an explicit call, they need no muzzle.
		/// </summary>
		private const string ManualTriggerType = "Request";

		/// <summary>
		/// A trigger condition that can never be true.
		/// </summary>
		private const string AlwaysFalseExpression = "@false";


		/// <summary>
		/// Returns the structural problems of the definition that would make the flow
		/// designer fail on it: no trigger, no action, or runAfter dependencies pointing
		/// to actions that don't exist in the same scope.
		/// </summary>
		public static IReadOnlyList<string> GetStructureErrors(string clientData)
		{
			var errors = new List<string>();

			using var document = JsonDocument.Parse(clientData);
			if (!document.RootElement.TryGetProperty("properties", out var properties)
				|| properties.ValueKind != JsonValueKind.Object
				|| !properties.TryGetProperty("definition", out var definition)
				|| definition.ValueKind != JsonValueKind.Object)
			{
				// the caller already warns about this shape, nothing to check here
				return errors;
			}

			if (!definition.TryGetProperty("triggers", out var triggers)
				|| triggers.ValueKind != JsonValueKind.Object
				|| !triggers.EnumerateObject().Any())
			{
				errors.Add("The definition contains no trigger. A flow must contain at least one trigger.");
			}

			if (!definition.TryGetProperty("actions", out var actions)
				|| actions.ValueKind != JsonValueKind.Object
				|| !actions.EnumerateObject().Any())
			{
				errors.Add("The definition contains no action. A flow must contain at least one action.");
				return errors;
			}

			CollectRunAfterErrors(actions, errors);
			return errors;
		}


		/// <summary>
		/// Returns the logical names of the connection references used by the definition.
		/// </summary>
		public static IReadOnlyList<string> GetConnectionReferenceLogicalNames(string clientData)
		{
			using var document = JsonDocument.Parse(clientData);
			if (!document.RootElement.TryGetProperty("properties", out var properties)
				|| properties.ValueKind != JsonValueKind.Object
				|| !properties.TryGetProperty("connectionReferences", out var references)
				|| references.ValueKind != JsonValueKind.Object)
			{
				return [];
			}

			return references.EnumerateObject()
				.Select(r => r.Value.ValueKind == JsonValueKind.Object
					&& r.Value.TryGetProperty("connection", out var connection)
					&& connection.ValueKind == JsonValueKind.Object
					&& connection.TryGetProperty("connectionReferenceLogicalName", out var logicalName)
					&& logicalName.ValueKind == JsonValueKind.String
						? logicalName.GetString()
						: null)
				.Where(n => !string.IsNullOrWhiteSpace(n))
				.Select(n => n!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}


		/// <summary>
		/// Returns a copy of the definition where every non-manual trigger carries a
		/// trigger condition that can never be true, so that the validation probe flow
		/// cannot fire in the short moment it is activated.
		/// </summary>
		public static string AddTriggerMuzzle(string clientData)
		{
			try
			{
				var root = JsonNode.Parse(clientData);
				if (root == null)
				{
					return clientData;
				}

				var properties = root["properties"] as JsonObject;
				var definition = properties?["definition"] as JsonObject;
				var triggers = definition?["triggers"] as JsonObject;
				if (triggers == null)
				{
					return clientData;
				}

				foreach (var trigger in triggers)
				{
					if (trigger.Value is not JsonObject triggerNode) continue;

					var type = triggerNode["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var typeName) ? typeName : null;
					if (string.Equals(type, ManualTriggerType, StringComparison.OrdinalIgnoreCase)) continue;

					// conditions are and-ed by the engine, so the muzzle is appended and the
					// conditions of the user still take part in the validation of the probe.
					// a conditions value that is not an array is left alone, the engine has
					// to see (and reject) it exactly as it is
					var existing = triggerNode["conditions"];
					if (existing != null && existing is not JsonArray) continue;

					var conditions = existing as JsonArray ?? [];
					triggerNode.Remove("conditions");
					conditions.Add(new JsonObject
					{
						["expression"] = AlwaysFalseExpression
					});
					triggerNode["conditions"] = conditions;
				}

				return root.ToJsonString(serializerOptions);
			}
			catch (ArgumentException)
			{
				// e.g. duplicate keys: JsonDocument tolerates them, JsonNode does not.
				// the engine has to see (and judge) such a definition exactly as it is
				return clientData;
			}
		}


		private static void CollectRunAfterErrors(JsonElement actions, List<string> errors)
		{
			var names = actions.EnumerateObject().Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

			foreach (var action in actions.EnumerateObject())
			{
				if (action.Value.ValueKind != JsonValueKind.Object) continue;

				if (action.Value.TryGetProperty("runAfter", out var runAfter) && runAfter.ValueKind == JsonValueKind.Object)
				{
					foreach (var dependency in runAfter.EnumerateObject())
					{
						if (!names.Contains(dependency.Name))
						{
							errors.Add($"The runAfter of action <{action.Name}> references the action <{dependency.Name}>, which does not exist in the same scope.");
						}
					}
				}

				// nested scopes: Scope/Foreach/If/Until keep child actions in "actions",
				// If also in "else.actions", Switch in "cases.*.actions" and "default.actions"
				if (action.Value.TryGetProperty("actions", out var children) && children.ValueKind == JsonValueKind.Object)
				{
					CollectRunAfterErrors(children, errors);
				}
				if (action.Value.TryGetProperty("else", out var elseNode) && elseNode.ValueKind == JsonValueKind.Object
					&& elseNode.TryGetProperty("actions", out var elseChildren) && elseChildren.ValueKind == JsonValueKind.Object)
				{
					CollectRunAfterErrors(elseChildren, errors);
				}
				if (action.Value.TryGetProperty("cases", out var cases) && cases.ValueKind == JsonValueKind.Object)
				{
					foreach (var caseNode in cases.EnumerateObject())
					{
						if (caseNode.Value.ValueKind == JsonValueKind.Object
							&& caseNode.Value.TryGetProperty("actions", out var caseChildren) && caseChildren.ValueKind == JsonValueKind.Object)
						{
							CollectRunAfterErrors(caseChildren, errors);
						}
					}
				}
				if (action.Value.TryGetProperty("default", out var defaultNode) && defaultNode.ValueKind == JsonValueKind.Object
					&& defaultNode.TryGetProperty("actions", out var defaultChildren) && defaultChildren.ValueKind == JsonValueKind.Object)
				{
					CollectRunAfterErrors(defaultChildren, errors);
				}
			}
		}


		/// <summary>
		/// The same relaxed escaping used when compacting the definition, so the probe
		/// differs from the real clientdata only by the muzzle itself.
		/// </summary>
		private static readonly JsonSerializerOptions serializerOptions = new()
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};
	}
}
