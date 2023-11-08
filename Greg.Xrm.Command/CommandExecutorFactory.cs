namespace Greg.Xrm.Command
{
	public class CommandExecutorFactory : ICommandExecutorFactory
	{
		private readonly IServiceProvider serviceProvider;

		public CommandExecutorFactory(IServiceProvider serviceProvider)
        {
			this.serviceProvider = serviceProvider;
		}

        public object? CreateFor(Type commandType)
		{
			var executorType = typeof(ICommandExecutor<>).MakeGenericType(commandType);

			var executor = serviceProvider.GetService(executorType);
			return executor;
		}
	}
}
