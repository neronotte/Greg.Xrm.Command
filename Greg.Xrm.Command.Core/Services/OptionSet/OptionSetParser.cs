using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.OptionSet
{
	public class OptionSetParser : IOptionSetParser
	{
		const string validEx = "0123456789ABCDEF";


		public IReadOnlyCollection<OptionMetadata> Parse(
			string? optionString, 
			string? colorsString, 
			int customizationOptionValuePrefix,
			int languageCode)
		{
			if (string.IsNullOrWhiteSpace(optionString))
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The options are required");


			var optionArray = optionString.Split(",;|".ToCharArray())
					.Select(x => x.Trim())
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Select(x => new OptionTuple(x))
					.ToArray();

			if (optionArray.Length == 0)
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The options are required for columns of type Picklist");

			if (optionArray.Any(x => x.HasValue) && 
				optionArray.Count(x => x.HasValue) != optionArray.Length)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"If you specify the value for one option, it must be specified for all options.");

			if (optionArray.Any(x => x.HasValue) && 
				optionArray.Select(x => x.Value).Distinct().Count() != optionArray.Length)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The values of the options must be unique.");

			string?[] colorsArray;
			if (colorsString != null)
			{
				colorsArray = colorsString.Split(",;|".ToCharArray())
					.Select(x => x.Trim().ToUpperInvariant())
					.ToArray();

				if (colorsArray.Length != optionArray.Length)
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The number of colors must match the number of options.");

				for (int i = 0; i < colorsArray.Length; i++)
				{
					var color = colorsArray[i];
					if (string.IsNullOrWhiteSpace(color))
					{
						colorsArray[i] = null;
						continue;
					}

					if (!color.StartsWith('#'))
					{
						color = "#" + color;
						colorsArray[i] = color;
					}

					if (!color.IsValidExadecimalColor())
						throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The color at position '{i}' ({color}) is not valid. It must be a valid hexadecimal color code.");
				}
			}
			else
			{
				colorsArray = new string?[optionArray.Length];
			}



			var optionSet = new List<OptionMetadata>();
			for (int i = 0; i < optionArray.Length; i++)
			{
				var option = optionArray[i];
				var optionValue = customizationOptionValuePrefix * 10000 + i;
				option.TrySetValue(optionValue);

				optionSet.Add(new OptionMetadata(new Label(option.Label, languageCode), option.Value) 
				{
					Color = colorsArray[i],
				});
			}

			return optionSet;
		}
	}
}
