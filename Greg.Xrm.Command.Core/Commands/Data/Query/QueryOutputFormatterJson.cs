using System.Diagnostics;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public class QueryOutputFormatterJson(IAnsiConsole console, string? fileName) : QueryOutputFormatterBase
	{
		public override async Task Print(IReadOnlyCollection<Entity> entities, bool autorun, CancellationToken cancellationToken)
		{

			var settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				Formatting = Formatting.Indented,
			};

			var serializer = new JsonSerializer
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
				DefaultValueHandling = DefaultValueHandling.Ignore
			};


			var items = new JArray();
			foreach (var entity in entities)
			{
				var item = new JObject();
				foreach (var attribute in entity.Attributes)
				{
					var value = JToken.FromObject(attribute.Value, serializer);

					item[attribute.Key] = value;
				}
				items.Add(item);
			}

			var json = JsonConvert.SerializeObject(items, settings);

			Print(console, json);


			if (!string.IsNullOrWhiteSpace(fileName))
			{
				await File.WriteAllTextAsync(fileName, json, cancellationToken);

				if (autorun)
				{
					Process.Start(new ProcessStartInfo
					{
						UseShellExecute = true,
						FileName = fileName
					});
				}
			}
		}
	}
}
