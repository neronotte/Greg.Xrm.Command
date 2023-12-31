﻿using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Table
{
    public class DeleteCommandExecutor : ICommandExecutor<DeleteCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;

        public DeleteCommandExecutor(
            IOutput output,
            IOrganizationServiceRepository organizationServiceRepository)
        {
            this.output = output;
            this.organizationServiceRepository = organizationServiceRepository;
        }



        public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{
                output.Write("Deleting table ").Write(command.SchemaName, ConsoleColor.Yellow).Write("...");

                var request = new DeleteEntityRequest
                {
                    LogicalName = command.SchemaName
                };

                await crm.ExecuteAsync(request);

                output.WriteLine(" Done", ConsoleColor.Green);
                return CommandResult.Success();
            }
            catch (Exception ex)
            {
                return CommandResult.Fail(ex.Message, ex);
            }
        }
    }
}
