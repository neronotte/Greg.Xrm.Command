using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public enum CustomApiBindingType     { Global = 0, Entity = 1, EntityCollection = 2 }
	public enum CustomApiType            { Action = 0, Function = 1 }
	public enum CustomApiAllowedStepType { None = 0, AsyncOnly = 1, SyncAndAsync = 2 }

	public enum CustomApiParamType
	{
		Boolean, DateTime, Decimal, Entity, EntityCollection,
		EntityReference, Float, Integer, Money, Picklist,
		String, StringArray, Guid
	}

	public record CustomApiParamSpec(string UniqueName, CustomApiParamType Type, bool IsOptional)
	{
		public static bool TryParse(string raw, out CustomApiParamSpec? result, out string error)
		{
			result = null;
			var colonIdx = raw.IndexOf(':');
			if (colonIdx < 0) { error = "expected Name:Type or Name?:Type"; return false; }

			var namePart = raw[..colonIdx];
			var typePart = raw[(colonIdx + 1)..];

			bool isOptional = namePart.EndsWith('?');
			var rawName  = isOptional ? namePart[..^1] : namePart;
			var uniqueName = CustomApiDisplayNameHelper.CleanIdentifier(rawName);

			if (!Enum.TryParse<CustomApiParamType>(typePart, true, out var type))
			{ error = $"unknown type '{typePart}'"; return false; }

			result = new(uniqueName, type, isOptional);
			error = string.Empty;
			return true;
		}

		/// <summary>
		/// Maps CustomApiParamType to the Dataverse integer code for the type field.
		/// </summary>
		public int TypeCode => Type switch
		{
			CustomApiParamType.Boolean         => 0,
			CustomApiParamType.DateTime        => 1,
			CustomApiParamType.Decimal         => 2,
			CustomApiParamType.Entity          => 3,
			CustomApiParamType.EntityCollection => 4,
			CustomApiParamType.EntityReference => 5,
			CustomApiParamType.Float           => 6,
			CustomApiParamType.Integer         => 7,
			CustomApiParamType.Money           => 8,
			CustomApiParamType.Picklist        => 9,
			CustomApiParamType.String          => 10,
			CustomApiParamType.StringArray     => 11,
			CustomApiParamType.Guid            => 12,
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static class CustomApiDisplayNameHelper
	{
		/// <summary>
		/// Infers a display name from a unique name: strips the publisher prefix
		/// (everything up to and including the first '_'), then inserts spaces before
		/// each capital letter boundary.
		/// Example: nn_GregSum -> "Greg Sum"
		/// </summary>
		public static string InferDisplayName(string uniqueName)
		{
			var underscoreIdx = uniqueName.IndexOf('_');
			var namePart = underscoreIdx >= 0 ? uniqueName[(underscoreIdx + 1)..] : uniqueName;
			return namePart.SplitNameInPartsByCapitalLetters().Trim();
		}

		/// <summary>
		/// Infers a unique name from a display name and publisher prefix by removing spaces
		/// and prepending the prefix.
		/// Example: "Greg Sum", "nn" -> "nn_GregSum"
		/// </summary>
		public static string InferUniqueName(string displayName, string publisherPrefix)
		{
			var namePart = string.Concat(displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
			return $"{publisherPrefix}_{namePart}";
		}

		/// <summary>
		/// Strips all characters that are not letters or digits.
		/// Spaces and special characters are removed so the result is a valid identifier.
		/// Example: "Data Di Nascita" -> "DataDiNascita"
		/// </summary>
		public static string CleanIdentifier(string raw)
			=> string.Concat(raw.Where(char.IsLetterOrDigit));

		/// <summary>
		/// Builds the value for the <c>name</c> attribute of a request parameter record:
		/// <c>{cleanApiDisplayName}-In-{cleanParamName}</c>.
		/// Example: "Pacx Sum", "Addend1" -> "PacxSum-In-Addend1"
		/// </summary>
		public static string BuildParamName(string apiDisplayName, string cleanParamName)
			=> $"{CleanIdentifier(apiDisplayName)}-In-{cleanParamName}";

		/// <summary>
		/// Builds the value for the <c>name</c> attribute of a response property record:
		/// <c>{cleanApiDisplayName}-Out-{cleanPropName}</c>.
		/// Example: "Pacx Sum", "Result" -> "PacxSum-Out-Result"
		/// </summary>
		public static string BuildResponseName(string apiDisplayName, string cleanPropName)
			=> $"{CleanIdentifier(apiDisplayName)}-Out-{cleanPropName}";
	}

	/// <summary>
	/// Writes a colorized Custom API signature to <see cref="IOutput"/>.
	/// Punctuation = White, required arg names = Cyan / types = Green, optional = DarkCyan / DarkGreen.
	/// Does NOT write the API name — callers are responsible for that.
	/// </summary>
	public static class CustomApiSignatureWriter
	{
		public static void WriteSignature(
			IOutput output,
			IEnumerable<(string name, string type, bool optional)> inputParams,
			IEnumerable<(string name, string type)> outputParams)
		{
			output.Write("(", ConsoleColor.White);

			bool first = true;
			foreach (var (name, type, optional) in inputParams)
			{
				if (!first) output.Write(", ", ConsoleColor.White);
				first = false;
				if (optional) output.Write("[", ConsoleColor.White);
				output.Write(name, optional ? ConsoleColor.DarkCyan  : ConsoleColor.Cyan);
				output.Write(": ", ConsoleColor.White);
				output.Write(type, optional ? ConsoleColor.DarkGreen : ConsoleColor.Green);
				if (optional) output.Write("]", ConsoleColor.White);
			}

			output.Write(")", ConsoleColor.White);

			var outputs = outputParams.ToList();
			if (outputs.Count > 0)
			{
				output.Write(" -> ", ConsoleColor.White);
				bool firstOut = true;
				foreach (var (name, type) in outputs)
				{
					if (!firstOut) output.Write(", ", ConsoleColor.White);
					firstOut = false;
					output.Write(name, ConsoleColor.Cyan);
					output.Write(": ", ConsoleColor.White);
					output.Write(type, ConsoleColor.Green);
				}
			}
		}
	}
}
