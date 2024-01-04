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
			var executorType = typeof(ICommandExecutor<>).MakeGenericType(commandType);

			scope ??= this.container.BeginLifetimeScope("executor", builder =>
			{
                foreach (var module in commandRegistry.Modules)
                {
                    builder.RegisterModule(module);
                }
            });

			var executor = this.scope.Resolve(executorType);
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
