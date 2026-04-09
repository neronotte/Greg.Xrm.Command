using Greg.Xrm.Command.Parsing;
using System.Text.Json.Nodes;

namespace Greg.Xrm.Command.Commands.Mcp
{
	public static class McpToolMapper
	{
		public static ModelContextProtocol.Protocol.Tool ToMcpTool(this CommandDefinition definition)
		{
			var schemaObj = new JsonObject
			{
				["type"] = "object",
				["properties"] = new JsonObject(),
				["required"] = new JsonArray()
			};

			var properties = schemaObj["properties"]!.AsObject();
			var required = schemaObj["required"]!.AsArray();

			foreach (var option in definition.Options)
			{
				var propName = option.Option.LongName;
				if (string.IsNullOrEmpty(propName)) continue;

				var prop = new JsonObject
				{
					["type"] = MapType(option.Property.PropertyType),
					["description"] = option.Option.HelpText ?? string.Empty
				};

				if (option.Option.DefaultValue != null)
				{
					prop["default"] = JsonValue.Create(option.Option.DefaultValue);
				}

				properties[propName] = prop;

				if (option.IsRequired)
				{
					required.Add(propName);
				}
			}

			var tool = new ModelContextProtocol.Protocol.Tool
			{
				Name = definition.ExpandedVerbs.Replace(" ", "_"),
				Description = definition.HelpText,
				InputSchema = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(schemaObj.ToJsonString())
			};

			return tool;
		}

		private static string MapType(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;

			if (type == typeof(bool)) return "boolean";
			if (type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return "number";
			return "string";
		}
	}
}
