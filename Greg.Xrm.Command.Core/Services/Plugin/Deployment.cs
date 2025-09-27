namespace Greg.Xrm.Command.Services.Plugin
{
public partial class PluginRegistrationToolkit
	{
		/// <summary>
		/// The plugin deployment mode
		/// </summary>
		public enum Deployment
		{
			/// <summary>
			/// ServerOnly = 0
			/// </summary>
			ServerOnly = 0,

			/// <summary>
			/// OfflineOnly = 1
			/// </summary>
			OfflineOnly = 1,

			/// <summary>
			/// Both = 2
			/// </summary>
			Both = 2
		}
	}
}
