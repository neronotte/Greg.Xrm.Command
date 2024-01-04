using Autofac.Core;
using System.Reflection;

namespace Greg.Xrm.Command.Parsing
{
	/// <summary>
	/// Registry that holds all the command definitions
	/// </summary>
	public interface ICommandRegistry
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="assembly"></param>
		void InitializeFromAssembly(Assembly assembly);


		CommandTree Tree { get; }


		IReadOnlyList<CommandDefinition> Commands { get; }


		IReadOnlyList<IModule> Modules { get; }
	}
}
