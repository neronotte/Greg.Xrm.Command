using Microsoft.Identity.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Services.Forms
{
	/// <summary>
	/// Manages OAuth2 token acquisition for Microsoft Forms API access.
	/// Handles both Client Credentials (user forms) and ROPC (group forms) flows.
	/// </summary>
	public interface IFormsTokenManager
	{
		/// <summary>
		/// Gets a valid access token, refreshing if necessary.
		/// </summary>
		Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Clears the cached token and forces a fresh acquisition on next call.
		/// </summary>
		void InvalidateToken();
	}

	public class FormsTokenManager : IFormsTokenManager
	{
		private readonly IPublicClientApplication _publicClientApp;
		private readonly IConfidentialClientApplication _confidentialClientApp;
		private readonly string[] _scopes;
		private readonly bool _useRopc;
		private readonly string? _username;
		private readonly string? _password;
		private readonly object _lock = new();
		private string? _cachedToken;
		private DateTime _tokenExpiry;

		public FormsTokenManager(
			string tenantId,
			string clientId,
			string? clientSecret = null,
			string? username = null,
			string? password = null,
			bool useRopc = false)
		{
			_scopes = new[] { "https://forms.office.com/.default" };
			_useRopc = useRopc;
			_username = username;
			_password = password;

			if (useRopc)
			{
				_publicClientApp = PublicClientApplicationBuilder
					.Create(clientId)
					.WithAuthority($"https://login.microsoftonline.com/{tenantId}")
					.Build();
			}
			else
			{
				_confidentialClientApp = ConfidentialClientApplicationBuilder
					.Create(clientId)
					.WithClientSecret(clientSecret!)
					.WithAuthority($"https://login.microsoftonline.com/{tenantId}")
					.Build();
			}

			_tokenExpiry = DateTime.MinValue;
		}

		public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
		{
			lock (_lock)
			{
				if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
				{
					return _cachedToken;
				}
			}

			AuthenticationResult result;

			if (_useRopc && !string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
			{
				// ROPC flow for group forms
				result = await _publicClientApp
					.AcquireTokenByUsernamePassword(_scopes, _username, _password)
					.ExecuteAsync(cancellationToken);
			}
			else
			{
				// Client Credentials flow for user forms
				result = await _confidentialClientApp
					.AcquireTokenForClient(_scopes)
					.ExecuteAsync(cancellationToken);
			}

			lock (_lock)
			{
				_cachedToken = result.AccessToken;
				_tokenExpiry = result.ExpiresOn.UtcDateTime;
			}

			return result.AccessToken;
		}

		public void InvalidateToken()
		{
			lock (_lock)
			{
				_cachedToken = null;
				_tokenExpiry = DateTime.MinValue;
			}
		}
	}
}
