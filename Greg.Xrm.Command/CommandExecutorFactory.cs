using Autofac;
using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command
{
	public class CommandExecutorFactory : ICommandExecutorFactory
	{
		private readonly ILifetimeScope container;
		private readonly ICommandRegistry commandRegistry;
		private bool disposedValue;
		private ILifetimeScope? scope;

		public CommandExecutorFactory(ILifetimeScope container, ICommandRegistry commandRegistry)
		{
			this.container = container;
			this.commandRegistry = commandRegistry;
		}




		public object? CreateFor(Type commandType)
		{
			scope ??= this.container.BeginLifetimeScope("executor", builder =>
			{
				foreach (var module in commandRegistry.Modules)
				{
					builder.RegisterModule(module);
				}

				builder
					.RegisterAssemblyTypes(commandType.Assembly)
					.Where(t => t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)))
					.AsSelf()
					.AsImplementedInterfaces();
			});


			var commandExecutorType = commandRegistry.GetExecutorTypeFor(commandType);
			if (commandExecutorType == null)
			{
				commandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(commandType);
			}

			var executor = this.scope.ResolveOptional(commandExecutorType);
			return executor;
		}




		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing && this.scope != null)
				{
					scope.Dispose();
				}

				disposedValue = true;
			}
		}


		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
