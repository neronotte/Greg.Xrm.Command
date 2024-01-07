using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
    /// <summary>
    /// Registry that holds all the command definitions
    /// </summary>
    public interface ICommandRegistry : IReadOnlyCommandRegistry
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="assembly"></param>
		void InitializeFromAssembly(Assembly assembly);

		void ScanPluginsFolder(ICommandLineArguments args);

		/// <summary>
		/// Returns the executor type for the given command type
		/// </summary>
		/// <param name="commandType">The type of the command</param>
		/// <returns></returns>
		Type? GetExecutorTypeFor(Type commandType);
	}
}
