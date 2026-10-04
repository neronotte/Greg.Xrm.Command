namespace Greg.Xrm.Command.Services.Settings
{
	public interface ISettingsRepository
	{
		Task<T?> GetAsync<T>(string key);
		Task SetAsync<T>(string key, T value);

		/// <summary>Updates a setting while excluding other readers and writers across processes.</summary>
		Task<T> UpdateAsync<T>(string key, Func<T?, T> update);
	}
}
