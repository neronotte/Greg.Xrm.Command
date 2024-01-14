namespace Greg.Xrm.Command.Parsing
{
	/// <summary>
	/// Creates a command object starting from a set of arguments
	/// </summary>
	public interface ICommandParser
	{
		/// <summary>
		/// Checks if there is a command matching the arguments provided.
		/// If found, returns the command object filled with options.
		/// If not found, or any error has been encountered, returns an help command.
		/// </summary>
		/// <param name="args">The arguments that will be used to match the command.</param>
		/// <returns>
		/// A command object, or an help command if no command has been found or an error happened during parsing.
		/// </returns>
		object Parse(params string[] args);


		/// <summary>
		/// Checks if there is a command matching the arguments provided.
		/// If found, returns the command object filled with options.
		/// If not found, or any error has been encountered, returns an help command.
		/// </summary>
		/// <param name="args">The arguments that will be used to match the command.</param>
		/// <returns>
		/// A command object, or an help command if no command has been found or an error happened during parsing.
		/// </returns>
		object Parse(IEnumerable<string> args);
	}
}
