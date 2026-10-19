namespace Greg.Xrm.Command.Services.Settings
{
	/// <summary>
	/// Extends settings storage with atomic read-modify-write operations.
	/// </summary>
	public interface IAtomicSettingsRepository : ISettingsRepository
	{
		/// <summary>
		/// Reads a setting, transforms its value, and persists the result while excluding
		/// other cooperating readers and writers for the same setting across processes.
		/// </summary>
		/// <typeparam name="T">The type of the stored setting.</typeparam>
		/// <param name="key">The key identifying the setting to update.</param>
		/// <param name="update">
		/// A synchronous function invoked once with the current value, or <c>default(T)</c>
		/// when the setting does not exist. Its return value is the new value to persist.
		/// </param>
		/// <returns>A task that completes with the new value after it has been persisted.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// The exclusive lock covers reading, invoking <paramref name="update"/>, and writing.
		/// All repository instances accessing the same backing store must use the same locking
		/// protocol, including for inherited read and write operations. The callback must not
		/// synchronously wait for another repository operation on the same key.
		/// If the callback or serialization fails, the previously persisted value is preserved
		/// and the lock is released. Operation failures are propagated to the caller.
		/// </remarks>
		Task<T> UpdateAsync<T>(string key, Func<T?, T> update);
	}
}