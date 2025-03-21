using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Connection
{
	public class TokenDefinition
    {
		public TokenDefinition(Uri serviceUri, string accessToken)
		{
			if (serviceUri is null)
			{
				throw new ArgumentNullException(nameof(serviceUri));
			}

			if (string.IsNullOrEmpty(accessToken))
			{
				throw new ArgumentException($"'{nameof(accessToken)}' cannot be null or empty.", nameof(accessToken));
			}

			this.ServiceUri = serviceUri;
			this.AccessToken = accessToken;
		}

		public TokenDefinition()
		{
		}

		public Uri? ServiceUri { get; set; }
		public string? AccessToken { get; set; }

		[JsonIgnore]
		public bool IsValid => this.ServiceUri != null && !string.IsNullOrEmpty(this.AccessToken);
	}
}
