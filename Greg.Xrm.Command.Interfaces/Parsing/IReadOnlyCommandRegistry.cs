using Autofac.Core;

namespace Greg.Xrm.Command.Parsing
{
	public interface IReadOnlyCommandRegistry
	{
		ICommandTree Tree { get; }


		IReadOnlyList<CommandDefinition> Commands { get; }


		IReadOnlyList<IModule> Modules { get; }
	}
}
