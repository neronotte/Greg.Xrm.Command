using Greg.Xrm.Command.Services.Connection;
using Microsoft.Data.SqlClient;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public class QueryExecutorSql(string sqlQuery, IOrganizationServiceRepository organizationServiceRepository) : IQueryExecutor
	{
		public async Task<IReadOnlyCollection<Entity>> ExecuteQueryAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken)
		{
			var serviceClient = crm as ServiceClient
				?? throw new InvalidOperationException("The provided IOrganizationServiceAsync2 instance is not a ServiceClient.");

			var accessToken = await organizationServiceRepository.GetCurrentAccessTokenAsync();
			if (string.IsNullOrEmpty(accessToken))
			{
				throw new InvalidOperationException(
									"The ServiceClient does not expose a valid access token. " +
									"Ensure it was configured with a token-based AuthType (for example, OAuth, ClientSecret, or Certificate).");
			}

			string sqlServerName = new Uri(serviceClient.ConnectedOrgUriActual.ToString()).Host;

			// Dataverse's TDS endpoint listens on port 5558. Without specifying it, SqlClient defaults to SQL Server's port 1433, so SQL queries will fail to connect in standard Dataverse environments.
			var connString = $"Server={sqlServerName},5558;Connect Timeout=15;";

			using var conn = new SqlConnection(connString);
			conn.AccessToken = accessToken;

			try
			{
				await conn.OpenAsync(cancellationToken);
			}
			catch (SqlException ex) when (ex.Message.IndexOf("TDS endpoint is disabled", StringComparison.OrdinalIgnoreCase) >= 0
										|| ex.Message.IndexOf("EnableSQLForCDS", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				throw new InvalidOperationException(
									"The TDS endpoint is disabled for this environment. Enable it in Power Platform Admin Center > Environment > Settings > Product > Features.", ex);
			}
			catch (SqlException ex) when (ex.Message.IndexOf("prvAllowTDSAccess", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				throw new InvalidOperationException(
									"The TDS endpoint is enabled, but the user or application lacks the 'Allow user to access TDS endpoint' privilege.", ex);
			}

			using var cmd = new SqlCommand(sqlQuery, conn);
			using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

			var rows = new List<Entity>();

			while (await reader.ReadAsync(cancellationToken))
			{
				var row = new Entity();
				for (int i = 0; i < reader.FieldCount; i++)
				{
					string columnName = reader.GetName(i);
					object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
					row[columnName] = value;
				}
				rows.Add(row);
			}

			return rows;
		}
	}
}
