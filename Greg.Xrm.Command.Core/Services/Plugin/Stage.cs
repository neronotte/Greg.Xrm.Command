namespace Greg.Xrm.Command.Services.Plugin
{
public partial class PluginRegistrationToolkit
	{
		/// <summary>
		/// The plugin stage
		/// </summary>
		public enum Stage
		{
			/// <summary>
			/// PreValidation = 10
			/// </summary>
			PreValidation = 10,

			/// <summary>
			/// PreOperation = 20
			/// </summary>
			PreOperation = 20,

			/// <summary>
			/// PostOperation = 40
			/// </summary>
			PostOperation = 40
		}
	}
}
