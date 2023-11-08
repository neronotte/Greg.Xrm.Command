using Greg.Xrm.Command.Services.Connection;

namespace Greg.Xrm.Command.Commands.Auth
{
	public class ListCommandExecutor : ICommandExecutor<ListCommand>
	{
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public ListCommandExecutor(IOrganizationServiceRepository organizationServiceRepository)
        {
			this.organizationServiceRepository = organizationServiceRepository;
		}

        public async Task ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			var connections = await organizationServiceRepository.GetAllConnectionDefinitionsAsync();

			if (connections.ConnectionStrings.Count == 0)
			{
				Console.WriteLine("No authentication profiles found.");
				return;
			}

			Console.WriteLine("The following authentication profiles are stored on this computer:");

			var padding = connections.ConnectionStrings.Max(_ => _.Key.Length) + 4;

			foreach (var item in connections.ConnectionStrings.OrderBy(x =>x.Key))
			{
				var name = item.Key;
				if (name.Equals(connections.CurrentConnectionStringKey, StringComparison.InvariantCultureIgnoreCase))
				{
					name += "*";
				}
				name = name.PadRight(padding);

				Console.Write("  ");
				Console.Write(name);
				Console.WriteLine(item.Value);
			}
		}
	}
}
