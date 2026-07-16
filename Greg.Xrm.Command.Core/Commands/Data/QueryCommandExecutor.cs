using Greg.Xrm.Command.Commands.Data.Query;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Data
{
	public class QueryCommandExecutor(
		IOutput output,
		IAnsiConsole console,
		IOrganizationServiceRepository organizationServiceRepository,
		IQueryExecutorFactory queryExecutorFactory,
		IQueryOutputFormatterFactory queryOutputFormatterFactory
		) : ICommandExecutor<QueryCommand>
	{
		public async Task<CommandResult> ExecuteAsync(QueryCommand command, CancellationToken cancellationToken)
		{

			var queryText = command.Query;
			if (string.IsNullOrWhiteSpace(queryText))
			{
				output.Write("Loading query text from file...");
				try
				{

					queryText = await File.ReadAllTextAsync(command.QueryFile!, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green).WriteLine();
				}
catch (Exception ex) when (ex is not OperationCanceledException)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					return CommandResult.Fail($"Error reading query file: {ex.Message}", ex);
				}
			}

			if (string.IsNullOrWhiteSpace(queryText))
			{
				return CommandResult.Fail("Query text is empty. Please provide a query using the --query option or specify a query file using the --query-file option.");
			}

			var panel = new Panel(queryText)
				.Header("Query")
				.RoundedBorder()
				.BorderColor(Color.SkyBlue2);
			console.Write(panel);
			console.WriteLine();





			IQueryExecutor queryExecutor;
			IQueryOutputFormatter formatter;
			try
			{
				queryExecutor = queryExecutorFactory.DetectExecutorFromQueryText(queryText);
				formatter = queryOutputFormatterFactory.BuildFormatter(command.OutputFormat, command.OutputFileName);
			}
			catch (NotSupportedException ex)
			{
				return CommandResult.Fail($"{ex.Message}", ex);
			}


			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			output.Write("Executing query...");
			IReadOnlyCollection<Entity> result;
			try
			{
				result = await queryExecutor.ExecuteQueryAsync(crm, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Error executing query: {ex.Message}", ex);
			}

			await formatter.Print(result, command.OutputFileAutoRun, cancellationToken);

			return CommandResult.Success();
		}
	}
}
