namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public class ImportActionSetAppValue : IImportAction
	{
		private readonly string uniqueName;
		private readonly string appName;
		private readonly string value;

		public ImportActionSetAppValue(string uniqueName, string appName, string value)
		{
			this.uniqueName = uniqueName;
			this.appName = appName;
			this.value = value;
		}
	}
}
