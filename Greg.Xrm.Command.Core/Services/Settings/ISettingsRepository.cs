namespace Greg.Xrm.Command.Services.Settings
{
	public interface ISettingsRepository
	{
		Task<T?> GetAsync<T>(string key);
		Task SetAsync<T>(string key, T value);
	}
}
