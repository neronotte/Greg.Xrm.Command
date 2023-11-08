namespace Greg.Xrm.Command.Services.Connection
{
	public class ConnectionSetting 
	{
		public string? CurrentConnectionStringKey { get; set; }

		public Dictionary<string, string> ConnectionStrings { get; set; } = new Dictionary<string, string>();

		public Dictionary<string, string> DefaultSolutions { get; set; } = new Dictionary<string, string>();





		public bool Exists(string connectionName)
		{
			return this.ConnectionStrings.ContainsKey(connectionName);
		}


		public bool TryGetCurrentConnectionString(out string? connectionString)
		{
			if (string.IsNullOrWhiteSpace(this.CurrentConnectionStringKey))
			{
				connectionString = null;
				return false;
			}

			return this.ConnectionStrings.TryGetValue(this.CurrentConnectionStringKey, out connectionString);
		}
	}
}
