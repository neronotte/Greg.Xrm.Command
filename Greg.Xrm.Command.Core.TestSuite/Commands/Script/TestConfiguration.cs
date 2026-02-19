using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Script
{
	/// <summary>
	/// Helper class for integration test configuration
	/// </summary>
	public static class TestConfiguration
	{
		/// <summary>
		/// Creates a ServiceClient for integration testing using Client ID and Client Secret
		/// </summary>
		/// <param name="useEnvironmentVariables">If true, reads configuration from environment variables</param>
		/// <returns>Connected ServiceClient</returns>
		public static ServiceClient CreateServiceClient(bool useEnvironmentVariables = true)
		{
			string connectionString;

			if (useEnvironmentVariables)
			{
				// Read from environment variables
				var url = Environment.GetEnvironmentVariable("DATAVERSE_URL") 
					?? throw new InvalidOperationException("DATAVERSE_URL environment variable not set");
				var clientId = Environment.GetEnvironmentVariable("DATAVERSE_CLIENT_ID") 
					?? throw new InvalidOperationException("DATAVERSE_CLIENT_ID environment variable not set");
				var clientSecret = Environment.GetEnvironmentVariable("DATAVERSE_CLIENT_SECRET") 
					?? throw new InvalidOperationException("DATAVERSE_CLIENT_SECRET environment variable not set");

				connectionString = $"AuthType=ClientSecret;" +
					$"Url={url};" +
					$"ClientId={clientId};" +
					$"ClientSecret={clientSecret}";
			}
			else
			{
				// Hardcoded values for local development (NOT recommended for production)
				// Replace with your actual values
				connectionString = "AuthType=ClientSecret;" +
					"Url=https://YOUR-ORG.crm.dynamics.com;" +
					"ClientId=YOUR-CLIENT-ID;" +
					"ClientSecret=YOUR-CLIENT-SECRET";
			}

			var serviceClient = new ServiceClient(connectionString);

			if (!serviceClient.IsReady)
			{
				throw new InvalidOperationException($"Failed to connect to Dataverse: {serviceClient.LastError}");
			}

			return serviceClient;
		}

		/// <summary>
		/// Creates a ServiceClient with explicit parameters
		/// </summary>
		/// <param name="url">Dataverse environment URL</param>
		/// <param name="clientId">Azure AD Application (Client) ID</param>
		/// <param name="clientSecret">Azure AD Application Client Secret</param>
		/// <returns>Connected ServiceClient</returns>
		public static ServiceClient CreateServiceClient(string url, string clientId, string clientSecret)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentException("URL cannot be null or empty", nameof(url));
			if (string.IsNullOrWhiteSpace(clientId))
				throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
			if (string.IsNullOrWhiteSpace(clientSecret))
				throw new ArgumentException("Client Secret cannot be null or empty", nameof(clientSecret));

			var connectionString = $"AuthType=ClientSecret;" +
				$"Url={url};" +
				$"ClientId={clientId};" +
				$"ClientSecret={clientSecret}";

			var serviceClient = new ServiceClient(connectionString);

			if (!serviceClient.IsReady)
			{
				throw new InvalidOperationException($"Failed to connect to Dataverse: {serviceClient.LastError}");
			}

			return serviceClient;
		}

		/// <summary>
		/// Gets the default output directory for test files
		/// </summary>
		public static string GetTestOutputDirectory()
		{
			var baseDir = Path.Combine(Path.GetTempPath(), "PacxTests");
			if (!Directory.Exists(baseDir))
			{
				Directory.CreateDirectory(baseDir);
			}
			return baseDir;
		}
	}
}