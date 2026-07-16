using Microsoft.Data.SqlClient;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public class QueryExecutorSql(string sqlQuery) : IQueryExecutor
	{
		public async Task<IReadOnlyCollection<Entity>> ExecuteQueryAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken)
		{
			var serviceClient = crm as ServiceClient
				?? throw new InvalidOperationException("The provided IOrganizationServiceAsync2 instance is not a ServiceClient.");


			string accessToken = serviceClient.CurrentAccessToken;
			if (string.IsNullOrEmpty(accessToken))
			{
throw new InvalidOperationException(
					"The ServiceClient does not expose a valid access token. " +
					"Ensure it was configured with a token-based AuthType (for example, OAuth, ClientSecret, or Certificate).");
			}

			string sqlServerName = new Uri(serviceClient.ConnectedOrgUriActual.ToString()).Host;
			var connString = $"Server={sqlServerName};Connect Timeout=15;";

			using var conn = new SqlConnection(connString);
			conn.AccessToken = accessToken;

			try
			{
				await conn.OpenAsync();
			}
			catch (SqlException ex) when (ex.Message.IndexOf("TDS endpoint is disabled", StringComparison.OrdinalIgnoreCase) >= 0
										|| ex.Message.IndexOf("EnableSQLForCDS", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				throw new InvalidOperationException(
					"Il TDS endpoint è disabilitato per questo ambiente. Abilitalo da Power Platform Admin Center > Environment > Settings > Product > Features.", ex);
			}
			catch (SqlException ex) when (ex.Message.IndexOf("prvAllowTDSAccess", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				throw new InvalidOperationException(
					"Il TDS endpoint è abilitato, ma l'utente/applicazione non ha il privilegio 'Allow user to access TDS endpoint'.", ex);
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
