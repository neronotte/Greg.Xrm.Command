namespace Greg.Xrm.Command.Services.Connection
{
	public class ConnectionSetting 
	{
		public string? CurrentConnectionStringKey { get; set; }

		public Dictionary<string, string> ConnectionStrings { get; set; } = new Dictionary<string, string>();


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
