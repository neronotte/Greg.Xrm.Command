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

			var accessToken = await ResolveAccessTokenAsync(
				() => serviceClient.CurrentAccessToken,
				organizationServiceRepository.GetCurrentAccessTokenAsync);
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


		/// <summary>
		/// Chooses the bearer token used to authenticate on the Dataverse TDS (SQL) endpoint (issue #217).
		/// </summary>
		/// <remarks>
		/// <para>
		/// FIRST CHOICE: the token of the very <see cref="ServiceClient"/> that runs this query
		/// (<see cref="ServiceClient.CurrentAccessToken"/>). It belongs to the same profile and environment as the
		/// rest of the command, and the SDK refreshes it when it is about to expire, so SQL queries keep working in
		/// long-running commands. Before #217 the token always came from the repository, which builds a SECOND
		/// connection and, for OAuth profiles, could return a non-refreshable token from <c>tokens.json</c>.
		/// Verified on 2026-10-18: TDS accepts the OAuth token returned by this property, also after a refresh.
		/// </para>
		/// <para>
		/// FALLBACK (unchanged pre-#217 behavior): the repository token. Needed because
		/// <see cref="ServiceClient.CurrentAccessToken"/> is EMPTY for clients built from a fixed-token callback,
		/// which is how PACX still connects in <see cref="TokenCacheMode.LegacyPacxCache"/> mode (systems without
		/// secure token storage). The repository returns the token saved in <c>tokens.json</c> in that case.
		/// </para>
		/// </remarks>
		/// <param name="getClientToken">Reads the token of the client running the query.</param>
		/// <param name="getRepositoryToken">Pre-#217 source of the token, evaluated only if the client has none.</param>
		/// <returns>The token to use, or <c>null</c>/empty if none is available.</returns>
		internal static async Task<string?> ResolveAccessTokenAsync(Func<string?> getClientToken, Func<Task<string?>> getRepositoryToken)
		{
			var clientToken = getClientToken();
			if (!string.IsNullOrEmpty(clientToken))
				return clientToken;

			return await getRepositoryToken();
		}
	}
}
