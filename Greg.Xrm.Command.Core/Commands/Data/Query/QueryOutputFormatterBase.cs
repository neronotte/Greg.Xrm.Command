using Microsoft.Xrm.Sdk;
using Spectre.Console;
using Color = Spectre.Console.Color;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public abstract class QueryOutputFormatterBase : IQueryOutputFormatter
	{
		public abstract Task Print(IReadOnlyCollection<Entity> entities, bool autorun, CancellationToken cancellationToken);


		protected string GetPrintableString(Entity entity, string attributeName)
		{
			var formattedValue = entity.GetFormattedValue(attributeName);
			if (!string.IsNullOrWhiteSpace(formattedValue)) return formattedValue;

			var attributeValue = entity.GetAttributeValue<object>(attributeName);

			if (attributeValue is EntityReference entityReference)
			{
				return entityReference.Name ?? entityReference.Id.ToString();
			}

			if (attributeValue is OptionSetValue optionSetValue)
			{
				return optionSetValue.Value.ToString();
			}

			return attributeValue?.ToString() ?? "-null-";
		}


		protected void Print(IAnsiConsole console, string text)
		{
			var panel = new Panel(Markup.Escape(text))
				.Header("Query Result")
				.RoundedBorder()
				.BorderColor(Color.Green);

			console.WriteLine();
			console.Write(panel);
			console.WriteLine();
		}
	}
}
