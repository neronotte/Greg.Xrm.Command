namespace Greg.Xrm.Command.Services.Plugin
{
public partial class PluginRegistrationToolkit
	{
		/// <summary>
		/// The plugin execution mode
		/// </summary>
		public enum Mode
		{
			/// <summary>
			/// Synchronous = 0
			/// </summary>
			Sync = 0,

			/// <summary>
			/// Asynchronous = 1
			/// </summary>
			Async = 1
		}
	}
}
