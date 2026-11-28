using System.Data.Common;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.PowerPlatform.Dataverse.Client.Auth;
using Microsoft.Xrm.Sdk.Organization;

namespace Greg.Xrm.Command.Services.Connection
{
	internal static class FlowAuthentication
	{
		private static readonly string[] Scopes = ["https://service.flow.microsoft.com/.default"];
		private const string SdkSampleClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
		private const string SdkSampleRedirectUri = "app://58145B91-0C36-4500-8554-080854F2AC97";
		private static readonly string SdkVersion =
			typeof(OrganizationDetail).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "Unknown";

		internal static async Task<AuthenticationResult> AcquireAsync(
			string connectionString, ISecureTokenStorageProbe secureStorage, CancellationToken cancellationToken)
		{
			DbConnectionStringBuilder profile;
			try
			{
				profile = new DbConnectionStringBuilder { ConnectionString = connectionString };
			}
			catch (ArgumentException)
			{
				throw InvalidProfile("The active authentication profile has an invalid connection string.");
			}

			var authType = Get(profile, OAuthConnectionString.AuthTypeKeys)?.ToUpperInvariant();
			if (authType is not ("OAUTH" or "CLIENTSECRET" or "CERTIFICATE"))
				throw InvalidProfile("Power Automate requires an OAuth, ClientSecret, or Certificate authentication profile.");

			var clientId = Get(profile, "ClientId", "AppId", "ApplicationId");
			if (clientId is null && authType != "OAUTH")
				throw InvalidProfile("The active authentication profile is missing a client ID.");

			var username = Get(profile, OAuthConnectionString.UsernameKeys)?.Trim();
			if (authType == "OAUTH" && username is null)
				throw InvalidProfile("Power Automate could not identify the Dataverse user for the active profile. Recreate it or configure its Username before retrying.");

			var authority = await ResolveAuthorityAsync(profile, cancellationToken);
			if (authType == "OAUTH")
				return await AcquireUserTokenAsync(profile, authority, clientId, username!, secureStorage, cancellationToken);

			var builder = ConfidentialClientApplicationBuilder.Create(clientId!)
				.WithAuthority(authority)
				.WithLegacyCacheCompatibility(false);
			if (authType == "CLIENTSECRET")
			{
				var secret = Get(profile, "ClientSecret", "Secret")
					?? throw InvalidProfile("The active authentication profile is missing its client secret.");
				return await builder.WithClientSecret(secret).Build()
					.AcquireTokenForClient(Scopes).ExecuteAsync(cancellationToken);
			}

			using var certificate = FindCertificate(profile);
			return await builder.WithCertificate(certificate, sendX5C: true).Build()
				.AcquireTokenForClient(Scopes).ExecuteAsync(cancellationToken);
		}

		private static async Task<string> ResolveAuthorityAsync(DbConnectionStringBuilder profile, CancellationToken cancellationToken)
		{
			// Ask the Dataverse environment for its tenant, just as the SDK does. The Flow host
			// is shared across tenants and cannot identify the tenant of the active profile.
			if (!Uri.TryCreate(Get(profile, "ServiceUri", "Service Uri", "Url", "Server"), UriKind.Absolute, out var serviceUri)
				|| serviceUri.Scheme != Uri.UriSchemeHttps)
				throw InvalidProfile("The active authentication profile must specify an HTTPS Dataverse URL.");

			var endpoint = new UriBuilder(serviceUri);
			if (!endpoint.Path.EndsWith("/web", StringComparison.OrdinalIgnoreCase))
				endpoint.Path = endpoint.Path.TrimEnd('/') + "/web";
			if (!endpoint.Query.Contains("SDKClientVersion=", StringComparison.Ordinal))
				endpoint.Query = endpoint.Query.TrimStart('?') is { Length: > 0 } query
					? $"{query}&SDKClientVersion={SdkVersion}" : $"SDKClientVersion={SdkVersion}";

			using var http = new HttpClient();
			var details = await new AuthorityResolver(http).ProbeForExpectedAuthentication(endpoint.Uri)
				.WaitAsync(cancellationToken);
			if (!details.Success || details.Authority.Scheme != Uri.UriSchemeHttps)
				throw InvalidProfile("Could not resolve the sign-in authority from the active Dataverse environment.");
			return details.Authority.AbsoluteUri;
		}

		private static async Task<AuthenticationResult> AcquireUserTokenAsync(DbConnectionStringBuilder profile,
			string authority, string? clientId, string username, ISecureTokenStorageProbe secureStorage, CancellationToken cancellationToken)
		{
			var redirectUri = Get(profile, "RedirectUri", "ReplyUrl") ?? (clientId is null ? SdkSampleRedirectUri : null);
			var builder = PublicClientApplicationBuilder.Create(clientId ?? SdkSampleClientId)
				.WithAuthority(authority)
				.WithLegacyCacheCompatibility(false);
			var app = (redirectUri is null ? builder : builder.WithRedirectUri(redirectUri)).Build();
			MsalCacheHelper? cache = null;
			try
			{
				if (secureStorage.IsAvailable())
				{
					var path = Get(profile, OAuthConnectionString.TokenCacheStorePathKey) ?? TokenCache.GetMsalCacheFilePath();
					cache = await RegisterSecureCacheAsync(app.UserTokenCache, path);
				}

				string? claims;
				try
				{
					return RequireExpectedAccount(await app.AcquireTokenSilent(Scopes, username).ExecuteAsync(cancellationToken), username);
				}
				catch (MsalUiRequiredException ex)
				{
					claims = ex.Claims;
				}

				if (string.Equals(Get(profile, "LoginPrompt"), "Never", StringComparison.OrdinalIgnoreCase))
					throw InvalidProfile("Power Automate requires an interactive sign-in, which the active profile disallows.");

				var interactive = app.AcquireTokenInteractive(Scopes).WithLoginHint(username);
				if (!string.IsNullOrWhiteSpace(claims))
					interactive = interactive.WithClaims(claims);
				return RequireExpectedAccount(await interactive.ExecuteAsync(cancellationToken), username);
			}
			finally
			{
				cache?.UnregisterCache(app.UserTokenCache);
			}
		}

		private static AuthenticationResult RequireExpectedAccount(AuthenticationResult result, string username)
		{
			if (!string.Equals(username, result.Account?.Username, StringComparison.OrdinalIgnoreCase))
				throw InvalidProfile("The Power Automate sign-in did not match the account configured in the active profile.");
			return result;
		}

		private static async Task<MsalCacheHelper?> RegisterSecureCacheAsync(ITokenCache tokenCache, string path)
		{
			// Match the Dataverse SDK's FileBackedTokenCacheHints so the same MSAL refresh
			// tokens are available on Windows, Linux and macOS. Never enable unprotected storage.
			var host = Path.GetFileNameWithoutExtension(string.Join('_',
				AppDomain.CurrentDomain.FriendlyName.Split(Path.GetInvalidFileNameChars())));
			var fullPath = Path.GetFullPath(path);
			var properties = new StorageCreationPropertiesBuilder(Path.GetFileName(fullPath), Path.GetDirectoryName(fullPath))
				.WithLinuxKeyring($"{host}.dvserviceclient.tokencache", MsalCacheHelper.LinuxKeyRingDefaultCollection,
					$"Token Storage for {host}", new("Version", SdkVersion), new("ProductGroup", host))
				.WithMacKeyChain($"{host}_service", $"{host}_account")
				.Build();
			try
			{
				var helper = await MsalCacheHelper.CreateAsync(properties);
				helper.RegisterCache(tokenCache);
				return helper;
			}
			catch (MsalCachePersistenceException)
			{
				return null;
			}
		}

		private static X509Certificate2 FindCertificate(DbConnectionStringBuilder profile)
		{
			var thumbprint = Get(profile, "CertificateThumbprint", "Thumbprint")
				?? throw InvalidProfile("The active authentication profile is missing its certificate thumbprint.");
			var storeName = Get(profile, "CertificateStoreName", "StoreName") ?? nameof(StoreName.My);
			foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
			{
				try
				{
					using var store = new X509Store(storeName, location);
					store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
					X509Certificate2? selected = null;
					foreach (var certificate in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false))
					{
						if (selected is null && certificate.HasPrivateKey)
							selected = certificate;
						else
							certificate.Dispose();
					}
					if (selected is not null)
						return selected;
				}
				catch (CryptographicException)
				{
					// The store may not exist at both locations.
				}
			}
			throw InvalidProfile("The active profile's certificate and private key were not found in the configured store.");
		}

		private static string? Get(DbConnectionStringBuilder profile, params string[] aliases)
		{
			foreach (var alias in aliases)
				if (profile.TryGetValue(alias, out var value) && value is string text && !string.IsNullOrWhiteSpace(text))
					return text;
			return null;
		}

		private static CommandException InvalidProfile(string message) => new(CommandException.ConnectionInvalid, message);
	}
}
