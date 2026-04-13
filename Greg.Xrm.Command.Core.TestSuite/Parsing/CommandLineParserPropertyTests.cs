using FsCheck;
using FsCheck.Xunit;
using Greg.Xrm.Command.Commands.Auth;
using Greg.Xrm.Command.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greg.Xrm.Command.Parsing
{
	/// <summary>
	/// Property-based tests for the command-line parser using FsCheck.
	/// These tests discover edge cases by generating random inputs.
	/// </summary>
	public class CommandLineParserPropertyTests
	{
		private CommandRegistry GetRegistry()
		{
			var log = NullLogger<CommandRegistry>.Instance;
			var output = new OutputToMemory();
			var storage = new Storage();
			var registry = new CommandRegistry(log, output, storage);
			registry.InitializeFromAssembly(typeof(ListCommand).Assembly);
			return registry;
		}

		private CommandParser GetParser()
		{
			return new CommandParser(new OutputToMemory(), GetRegistry());
		}

		/// <summary>
		/// Property: Empty input should always produce a parse result (even if it's an error/help result).
		/// </summary>
		[Property]
		public Property EmptyInputShouldProduceParseResult()
		{
			var parser = GetParser();
			var (parseResult, _) = parser.Parse();
			return (parseResult != null).ToProperty();
		}

		/// <summary>
		/// Property: Any single-word command should not throw an exception.
		/// </summary>
		[Property]
		public Property SingleWordCommandShouldNotThrow(string word)
		{
			if (string.IsNullOrWhiteSpace(word))
				return true.ToProperty();

			var parser = GetParser();
			try
			{
				var (parseResult, _) = parser.Parse(word);
				return true.ToProperty();
			}
			catch
			{
				return false.ToProperty();
			}
		}

		/// <summary>
		/// Property: Repeated arguments should not crash the parser.
		/// </summary>
		[Property]
		public Property RepeatedArgsShouldNotCrash(string arg, int count)
		{
			if (string.IsNullOrWhiteSpace(arg) || count <= 0 || count > 20)
				return true.ToProperty();

			var parser = GetParser();
			var args = Enumerable.Repeat(arg, Math.Min(count, 20)).ToArray();

			try
			{
				var (parseResult, _) = parser.Parse(args);
				return true.ToProperty();
			}
			catch
			{
				return false.ToProperty();
			}
		}

		/// <summary>
		/// Property: Very long argument strings should not crash the parser.
		/// </summary>
		[Property]
		public Property LongArgsShouldNotCrash(string longArg)
		{
			if (string.IsNullOrEmpty(longArg))
				return true.ToProperty();

			// Generate a very long string
			var repeated = new string(longArg[0], Math.Min(longArg.Length * 10, 10000));
			var parser = GetParser();

			try
			{
				var (parseResult, _) = parser.Parse(repeated);
				return true.ToProperty();
			}
			catch
			{
				return false.ToProperty();
			}
		}

		/// <summary>
		/// Property: Special characters in arguments should not crash the parser.
		/// </summary>
		[Property]
		public Property SpecialCharactersShouldNotCrash(string input)
		{
			var specialChars = new[] { '&', '|', ';', '$', '`', '!', '#', '*', '?', '<', '>', '{', '}', '[', ']', '(', ')' };
			var specialInput = new string(specialChars);
			var parser = GetParser();

			try
			{
				var (parseResult, _) = parser.Parse(specialInput);
				return true.ToProperty();
			}
			catch
			{
				return false.ToProperty();
			}
		}

		/// <summary>
		/// Property: auth list command should always resolve to ListCommand.
		/// </summary>
		[Property]
		public Property AuthListShouldAlwaysResolveToListCommand(int dummy)
		{
			var parser = GetParser();
			var (parseResult, _) = parser.Parse("auth", "list");

			return (parseResult is ListCommand).ToProperty();
		}
	}
}
