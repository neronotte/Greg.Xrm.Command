using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column
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


        public async Task ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
        {
            this.output.Write($"Connecting to the current dataverse environment...");
            var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
            this.output.WriteLine("Done", ConsoleColor.Green);


            try
            {
                output.Write($"Deleting column ").Write(command.SchemaName, ConsoleColor.Yellow).Write(" from table ").Write(command.EntityName, ConsoleColor.Yellow).Write("...");

                var request = new DeleteAttributeRequest
                {
                    EntityLogicalName = command.EntityName,
                    LogicalName = command.SchemaName
                };

                await crm.ExecuteAsync(request);

                output.WriteLine(" Done", ConsoleColor.Green);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                output.WriteLine()
                    .Write("Error: ", ConsoleColor.Red)
                    .WriteLine(ex.Message, ConsoleColor.Red);

                if (ex.InnerException != null)
                {
                    output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
                }
            }
        }
    }
}
