using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Connector
{
	public class ConnectorImportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ConnectorImportCommand>
	{
		private readonly IOutput output = output ?? throw new ArgumentNullException(nameof(output));
		private readonly IOrganizationServiceRepository organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));

		public async Task<CommandResult> ExecuteAsync(ConnectorImportCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FilePath))
			{
				return CommandResult.Fail($"Connector definition not found: {command.FilePath}");
			}

			var content = await File.ReadAllTextAsync(command.FilePath, cancellationToken);

			this.output.Write("Connecting to the current Dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			this.output.WriteLine($"Importing connector from: {command.FilePath}", ConsoleColor.Cyan);

			if (command.DryRun)
			{
				this.output.WriteLine("[DRY RUN] Would import:", ConsoleColor.Yellow);
				this.output.WriteLine($"  File: {command.FilePath}");
				this.output.WriteLine($"  Size: {content.Length} bytes");
				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
					this.output.WriteLine($"  Solution: {command.SolutionUniqueName}");
				return CommandResult.Success();
			}

			try
			{
				var connector = new Entity("connector");
				connector["name"] = Path.GetFileNameWithoutExtension(command.FilePath);
				connector["openapidefinition"] = content;
				connector["connectortype"] = new OptionSetValue(1); // Custom

				this.output.Write("Creating custom connector in Dataverse...");
				var connectorId = await crm.CreateAsync(connector, cancellationToken);
				this.output.WriteLine("Done", ConsoleColor.Green);
				this.output.WriteLine($"Connector ID: {connectorId}");

				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
				{
					var request = new Microsoft.Crm.Sdk.Messages.AddSolutionComponentRequest
					{
						ComponentId = connectorId,
						ComponentType = 371, // Connector
						SolutionUniqueName = command.SolutionUniqueName,
						AddRequiredComponents = true
					};
					await crm.ExecuteAsync(request, cancellationToken);
					this.output.WriteLine($"Added connector to solution: {command.SolutionUniqueName}", ConsoleColor.Green);
				}

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Connector import error: {ex.Message}", ex);
			}
		}
	}

	public class ConnectorExportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ConnectorExportCommand>
	{
		private readonly IOutput output = output ?? throw new ArgumentNullException(nameof(output));
		private readonly IOrganizationServiceRepository organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));

		public async Task<CommandResult> ExecuteAsync(ConnectorExportCommand command, CancellationToken cancellationToken)
		{
			this.output.Write("Connecting to the current Dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync(cancellationToken);
			this.output.WriteLine("Done", ConsoleColor.Green);

			this.output.WriteLine($"Exporting connector: {command.ConnectorName}", ConsoleColor.Cyan);

			try
			{
				var query = new QueryExpression("connector");
				query.ColumnSet.AddColumns("name", "openapidefinition");
				query.Criteria.AddCondition("name", ConditionOperator.Equal, command.ConnectorName);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (result.Entities.Count == 0)
				{
					return CommandResult.Fail($"Connector '{command.ConnectorName}' not found.");
				}

				var connector = result.Entities[0];
				var definition = connector.GetAttributeValue<string>("openapidefinition");

				if (string.IsNullOrEmpty(definition))
				{
					return CommandResult.Fail($"Connector '{command.ConnectorName}' has no definition.");
				}

				await File.WriteAllTextAsync(command.OutputPath, definition, cancellationToken);
				this.output.WriteLine($"Connector definition exported to: {command.OutputPath}", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Connector export error: {ex.Message}", ex);
			}
		}
	}

	public class ConnectorTestCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ITokenProvider tokenProvider,
		IHttpClientFactory httpClientFactory) : ICommandExecutor<ConnectorTestCommand>
	{
		private readonly IOutput output = output ?? throw new ArgumentNullException(nameof(output));
		private readonly IOrganizationServiceRepository organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
		private readonly ITokenProvider tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
		private readonly IHttpClientFactory httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

		public async Task<CommandResult> ExecuteAsync(ConnectorTestCommand command, CancellationToken cancellationToken)
		{
			this.output.Write("Connecting to the current Dataverse environment...");
			var crmBase = await this.organizationServiceRepository.GetCurrentConnectionAsync(cancellationToken);
			if (crmBase is not ServiceClient crm)
			{
				return CommandResult.Fail("Connector testing requires a ServiceClient connection.");
			}
			this.output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var token = await this.tokenProvider.GetTokenAsync("https://api.bap.microsoft.com/", cancellationToken);
				if (string.IsNullOrEmpty(token))
				{
					return CommandResult.Fail("Failed to acquire token for Power Platform Admin API.");
				}

				var envId = crm.EnvironmentId;
				if (string.IsNullOrEmpty(envId))
				{
					var uri = crm.ConnectedOrgUriActual;
					envId = uri.Host.Split('.')[0];
				}

				using var client = this.httpClientFactory.CreateClient();
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var url = $"https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/{envId}/connectors/{command.ConnectorName}/test?api-version=2020-10-01&operationId={command.OperationName}";

				string payload = "{}";
				if (!string.IsNullOrEmpty(command.PayloadPath))
				{
					if (!File.Exists(command.PayloadPath))
						return CommandResult.Fail($"Payload file not found: {command.PayloadPath}");
					payload = await File.ReadAllTextAsync(command.PayloadPath, cancellationToken);
				}

				this.output.WriteLine($"Testing connector: {command.ConnectorName}", ConsoleColor.Cyan);
				this.output.WriteLine($"  Operation: {command.OperationName}");
				this.output.Write("Sending test request...");

				var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"), cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync(cancellationToken);
					return CommandResult.Fail($"API error ({response.StatusCode}): {error}");
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
				var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
				this.output.WriteLine("Response Received:", ConsoleColor.Cyan);
				this.output.WriteLine(resultJson);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Connector test error: {ex.Message}", ex);
			}
		}
	}

	public class ConnectorValidateCommandExecutor(
		IOutput output) : ICommandExecutor<ConnectorValidateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ConnectorValidateCommand command, CancellationToken cancellationToken)
		{
			if (!File.Exists(command.FilePath))
			{
				return CommandResult.Fail($"Connector definition not found: {command.FilePath}");
			}

			var content = await File.ReadAllTextAsync(command.FilePath, cancellationToken);

			output.WriteLine($"Validating connector: {command.FilePath}", ConsoleColor.Cyan);

			// Basic validation - check for required OpenAPI fields
			var issues = 0;
			if (!content.Contains("\"swagger\"") && !content.Contains("\"openapi\""))
			{
				output.WriteLine("  WARNING: Missing 'swagger' or 'openapi' version field", ConsoleColor.Yellow);
				issues++;
			}
			if (!content.Contains("\"info\""))
			{
				output.WriteLine("  WARNING: Missing 'info' field", ConsoleColor.Yellow);
				issues++;
			}
			if (!content.Contains("\"paths\""))
			{
				output.WriteLine("  ERROR: Missing 'paths' field", ConsoleColor.Red);
				issues++;
			}

			if (issues > 0)
			{
				output.WriteLine($"\nFound {issues} issue(s).", command.Strict ? ConsoleColor.Red : ConsoleColor.Yellow);
				return command.Strict ? CommandResult.Fail($"Validation failed: {issues} issue(s)") : CommandResult.Success();
			}

			output.WriteLine("Validation passed. No issues found.", ConsoleColor.Green);
			return CommandResult.Success();
		}
	}
}
