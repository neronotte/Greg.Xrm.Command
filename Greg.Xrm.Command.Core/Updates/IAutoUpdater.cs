namespace Greg.Xrm.Command.Updates
{
	public interface IAutoUpdater
	{
		string CurrentVersion { get; }
		string? NextVersion { get; }
		bool UpdateRequired { get; }
		Task<bool> CheckForUpdatesAsync();
		Task LaunchUpdateAsync();
	}
}
