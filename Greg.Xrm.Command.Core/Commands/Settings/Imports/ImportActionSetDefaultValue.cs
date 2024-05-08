namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public class ImportActionSetDefaultValue : IImportAction
	{
		private readonly string uniqueName;
		private readonly string value;

		public ImportActionSetDefaultValue(string uniqueName, string value)
		{
			this.uniqueName = uniqueName;
			this.value = value;
		}
	}
}
