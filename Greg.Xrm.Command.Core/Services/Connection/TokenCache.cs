using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Connection
{
    public class TokenCache : Dictionary<string, TokenDefinition>
    {
		public void Set(string name, Uri serviceUri, string token)
		{
			this[name] = new TokenDefinition(serviceUri, token);
		}


		public async Task SaveAsync()
		{
			var tokenCachePath = GetTokenCachePath();
			var tokenCacheFile = Path.Combine(tokenCachePath, "tokens.json");
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			await File.WriteAllTextAsync(tokenCacheFile, json);		
		}



		public static string GetTokenCachePath()
        {
			var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			folderPath = Path.Combine(folderPath, "Greg.Xrm.Command");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			folderPath = Path.Combine(folderPath, "tokenCache");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			return folderPath;
		}


		private static async Task<TokenCache> GetOrCreateAsync()
		{
			var tokenCachePath = GetTokenCachePath();
			var tokenCacheFile = Path.Combine(tokenCachePath, "tokens.json");
			if (!File.Exists(tokenCacheFile)) return new TokenCache();
			var json = await File.ReadAllTextAsync(tokenCacheFile);
			return JsonConvert.DeserializeObject<TokenCache>(json) ?? [];	
		}


		public static async Task<TokenDefinition?> TryGetAccessTokenAsync(string connectionName)
		{
			var tokenCache = await GetOrCreateAsync();
			if (!tokenCache.TryGetValue(connectionName, out var accessToken)) return null;
			if (accessToken == null || !accessToken.IsValid) return null;
			return accessToken;
		}


		public static async Task SaveAccessTokenAsync(string name, Uri serviceUri, string currentAccessToken)
		{
			var tokenCache = await GetOrCreateAsync();
			tokenCache.Set(name, serviceUri, currentAccessToken);

			await tokenCache.SaveAsync();
		}

		public static async Task ClearAccessTokenAsync(string connectionName)
		{
			var tokenCache = await GetOrCreateAsync();
			if (!tokenCache.ContainsKey(connectionName)) return;

			tokenCache.Remove(connectionName);
			await tokenCache.SaveAsync();
		}
	}
}
