using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Linq;
using System.ServiceModel.Channels;

namespace Greg.Xrm.Command.Parsing
{
	public class CommandDefinition : IComparable<CommandDefinition>
	{
		public CommandDefinition(CommandAttribute commandAttribute, Type commandType, IReadOnlyList<AliasAttribute> aliases)
		{
			this.Verbs = commandAttribute.Verbs;
			this.ExpandedVerbs = string.Join(" ", this.Verbs);
			this.HelpText = commandAttribute.HelpText ?? string.Empty;
			this.Hidden = commandAttribute.Hidden;

			this.CommandType = commandType;
			this.Aliases = aliases;
			this.Options = (from property in this.CommandType.GetProperties()
							let optionAttribute = property.GetCustomAttribute<OptionAttribute>()
							let requiredAttribute = property.GetCustomAttribute<RequiredAttribute>()
							let isRequired = requiredAttribute != null
							where optionAttribute != null
							select new OptionDefinition(property, optionAttribute, isRequired)).ToList();


			CheckDuplicateOptions();
		}

		private void CheckDuplicateOptions()
		{
			this.Options.ToLookup(x => x.Option.LongName)
				.Where(x => x.Count() > 1)
				.Select(x => x.Key)
				.ToList()
				.ForEach(x => throw new CommandException(CommandException.DuplicateOption, $"On command '{ExpandedVerbs}', Option --{x} is defined more than once."));


			this.Options.Where(x => x.Option.ShortName != null)
				.ToLookup(x => x.Option.ShortName ?? string.Empty)
				.Where(x => x.Count() > 1)
				.Select(x => x.Key)
				.ToList()
				.ForEach(x => throw new CommandException(CommandException.DuplicateOption, $"On command '{ExpandedVerbs}', Option --{x} is defined more than once."));
		}

		public string ExpandedVerbs { get; }
		public Type CommandType { get; }
		public IReadOnlyList<AliasAttribute> Aliases { get; }
		public string HelpText { get; }

		public bool Hidden { get; }
		public IReadOnlyList<string> Verbs { get; }
		public IReadOnlyList<OptionDefinition> Options { get; }

		public override string ToString()
		{
			return this.CommandType.FullName ?? string.Empty;
		}


		public object CreateCommand(IReadOnlyDictionary<string, string> options)
		{
			var usedOptions = new List<string>();

			var command = Activator.CreateInstance(this.CommandType);
			if (command == null)
				throw new CommandException(CommandException.CommandCannotBeCreated, $"Command '{this.CommandType}'cannot be created. Please pull an issue on GitHub page.");


			foreach (var optionDef in this.Options)
			{
				var property = optionDef.Property;
				var option = optionDef.Option;

				if (options.TryGetValue("--" + option.LongName, out var optionValue))
				{
					usedOptions.Add(option.LongName);
				}
				else if (!string.IsNullOrWhiteSpace(option.ShortName) && options.TryGetValue("-" + option.ShortName, out optionValue))
				{
					usedOptions.Add(option.ShortName);
				}
				else if(optionDef.IsRequired)
				{
					throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Option --{option.LongName} is required.");
				}
				else
				{
					continue;
				}

				var propertyType = property.PropertyType;


				if (string.IsNullOrWhiteSpace(optionValue) && optionDef.IsRequired)
					throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"You must specify a value for the option --{option.LongName}.");


				var propertyValue = Convert(optionValue, propertyType, option.LongName, optionDef.IsRequired, option.DefaultValue);

				property.SetValue(command, propertyValue);
			}

			if (usedOptions.Count != options.Count)
			{
				var unusedOptions = options.Keys.Except(usedOptions).ToList();
				throw new DataException($"The following options are not valid: {string.Join(", ", unusedOptions)}");
			}

			return command;
		}



		public bool TryMatch(CommandDefinition other, out string matchedAlias)
		{
			var thisVerbs = new List<string> { this.ExpandedVerbs };
			thisVerbs.AddRange(this.Aliases.Select(x => x.ExpandedVerbs));

			var otherVerbs = new List<string> { other.ExpandedVerbs };
			otherVerbs.AddRange(other.Aliases.Select(x => x.ExpandedVerbs));


			var v = thisVerbs.Intersect(otherVerbs, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
			if (v != null)
			{
				matchedAlias = $"<{v}>: {this.CommandType.FullName} and {other.CommandType.FullName}";
				return true;
			}

			matchedAlias = string.Empty;
			return false;
		}



		public bool IsMatch(IReadOnlyList<string> verbs)
		{
			if (IsMatch(verbs, this.Verbs)) return true;
			return this.Aliases.Any(x => IsMatch(verbs, x.Verbs));
		}

		private static bool IsMatch(IReadOnlyList<string> outerVerbs, IReadOnlyList<string> innerVerbs)
		{
			if (outerVerbs.Count != innerVerbs.Count) return false;

			for (int i = 0; i < innerVerbs.Count; i++)
			{
				if (!string.Equals(outerVerbs[i], innerVerbs[i], StringComparison.OrdinalIgnoreCase)) return false;
			}

			return true;
		}




		private static object? Convert(string optionValue, Type propertyType, string argumentName, bool isRequired, object? defaultValue)
		{
			if (propertyType == typeof(string))
			{
				if (!string.IsNullOrWhiteSpace(optionValue))
					return optionValue;

				CheckIfMatchType(defaultValue, propertyType, argumentName);
				return defaultValue;
			}

			if (propertyType == typeof(bool) && (optionValue == string.Empty)) optionValue = "true";

			if (string.IsNullOrWhiteSpace(optionValue))
			{
				CheckIfMatchType(defaultValue, propertyType, argumentName);
				if (defaultValue == null && isRequired)
					throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"You must specify a value for the option --{argumentName}.");
				
				return defaultValue;
			}

			if (propertyType.IsEnum)
			{
				if (!Enum.TryParse(propertyType, optionValue, true, out var enumValue))
					throw new CommandException(CommandException.CommandInvalidArgumentType, $"Argument '{argumentName}' error: the value '{optionValue}' is not a valid {propertyType.FullName}.");

				return enumValue;
			}


			var undelyingType = Nullable.GetUnderlyingType(propertyType);
			if (undelyingType != null && undelyingType.IsEnum)
			{
				if (string.IsNullOrWhiteSpace(optionValue))
					return null;

				if (!Enum.TryParse(undelyingType, optionValue, true, out var enumValue))
					throw new CommandException(CommandException.CommandInvalidArgumentType, $"Argument '{argumentName}' error: the value '{optionValue}' is not a valid {propertyType.FullName}.");

				return enumValue;
			}

			if (propertyType == typeof(Guid))
			{
				if (!Guid.TryParse(optionValue, out Guid guidValue))
				{
					throw new CommandException(CommandException.CommandInvalidArgumentType, $"Argument '{argumentName}' error: the value '{optionValue}' is not a valid {propertyType.FullName}.");
				}

				return guidValue;
			}
			if (propertyType == typeof(Guid?))
			{
				if (string.IsNullOrWhiteSpace(optionValue))
					return null;

				if (!Guid.TryParse(optionValue, out Guid guidValue))
				{
					throw new CommandException(CommandException.CommandInvalidArgumentType, $"Argument '{argumentName}' error: the value '{optionValue}' is not a valid {propertyType.FullName}.");
				}

				return guidValue;
			}


			try
			{
				propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

				var actualValue = System.Convert.ChangeType(optionValue, propertyType);
				if (actualValue != null)
					return actualValue;

				throw new CommandException(CommandException.CommandInvalidArgumentType, $"Argument '{argumentName}' error: the value '{optionValue}' is not a valid {propertyType.FullName}.");
			}
			catch (InvalidCastException)
			{
				throw new CommandException(CommandException.CommandInvalidArgumentType, $"Argument '{argumentName}' error: the value '{optionValue}' is not a valid {propertyType.FullName}.");
			}
		}


		private static void CheckIfMatchType(object? obj, Type type, string argumentName)
		{
			if (obj is null) return;
			if (obj.GetType() != type)
				throw new CommandException(CommandException.CommandInvalidArgumentType, $"Invalid type for the default value of argument '{argumentName}': expected '{type.FullName}', actual '{obj.GetType().FullName}'");
        }



		#region IComparable or IEquatable interface


		public int CompareTo(CommandDefinition? other)
		{
			if (other is null) return -1;

			for (int i = 0; i < Math.Min(Verbs.Count, other.Verbs.Count); i++)
			{
				var compareVerb = string.Compare(Verbs[i], other.Verbs[i], StringComparison.OrdinalIgnoreCase);
				if (compareVerb != 0) return compareVerb;
			}

			if (Verbs.Count < other.Verbs.Count) return -1;
			if (Verbs.Count > other.Verbs.Count) return 1;
			return 0;
		}

		public override bool Equals(object? obj)
		{
			if (obj == null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj is not CommandDefinition other) return false;

			return this.CompareTo(other) == 0;
		}

		public override int GetHashCode()
		{
			return this.ExpandedVerbs.GetHashCode();
		}

		public static bool operator ==(CommandDefinition left, CommandDefinition right)
		{
			if (left is null)
			{
				return right is null;
			}

			return left.Equals(right);
		}

		public static bool operator !=(CommandDefinition left, CommandDefinition right)
		{
			return !(left == right);
		}

		public static bool operator <(CommandDefinition left, CommandDefinition right)
		{
			return left is null ? right is not null : left.CompareTo(right) < 0;
		}

		public static bool operator <=(CommandDefinition left, CommandDefinition right)
		{
			return left is null || left.CompareTo(right) <= 0;
		}

		public static bool operator >(CommandDefinition left, CommandDefinition right)
		{
			return left is not null && left.CompareTo(right) > 0;
		}

		public static bool operator >=(CommandDefinition left, CommandDefinition right)
		{
			return left is null ? right is null : left.CompareTo(right) >= 0;
		}

		#endregion
	}
}