namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public class ImportActionSetEnvironmentValue : IImportAction
	{
		private readonly string uniqueName;
		private readonly string value;

		public ImportActionSetEnvironmentValue(string uniqueName, string value)
		{
			this.uniqueName = uniqueName;
			this.value = value;
		}
	}
}
