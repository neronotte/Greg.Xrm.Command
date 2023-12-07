using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Auth
{
	public class ListCommandExecutor : ICommandExecutor<ListCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public ListCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}

        public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			var connections = await organizationServiceRepository.GetAllConnectionDefinitionsAsync();

			if (connections.ConnectionStrings.Count == 0)
			{
				return CommandResult.Fail("No authentication profiles found.");
			}

			output.WriteLine("The following authentication profiles are stored on this computer:");

			var padding = connections.ConnectionStrings.Max(_ => _.Key.Length) + 4;

			foreach (var item in connections.ConnectionStrings.OrderBy(x =>x.Key))
			{
				var name = item.Key;
				if (name.Equals(connections.CurrentConnectionStringKey, StringComparison.InvariantCultureIgnoreCase))
				{
					name += "*";
				}
				name = name.PadRight(padding);

				output.Write("  ");
				output.Write(name);
				output.WriteLine(item.Value);
			}

			return CommandResult.Success();
		}
	}
}
