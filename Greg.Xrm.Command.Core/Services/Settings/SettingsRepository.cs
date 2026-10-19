using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Settings
{
	public class SettingsRepository(IStorage storage) : IAtomicSettingsRepository
	{
		public Task<T?> GetAsync<T>(string key) =>
			WithSettingsMutexAsync(key, Read<T>);

		public Task SetAsync<T>(string key, T value) =>
			WithSettingsMutexAsync(key, fileName =>
			{
				WriteFileAtomically(fileName, value);
				return true;
			});

		public Task<T> UpdateAsync<T>(string key, Func<T?, T> update)
		{
			ArgumentNullException.ThrowIfNull(update);
			return WithSettingsMutexAsync(key, fileName =>
			{
				var updatedValue = update(Read<T>(fileName));
				WriteFileAtomically(fileName, updatedValue);
				return updatedValue;
			});
		}

		private Task<T> WithSettingsMutexAsync<T>(string key, Func<string, T> action)
		{
			var fileName = Path.GetFullPath(Path.Combine(storage.GetOrCreateStorageFolder().FullName, $"{key}.json"));
			var identity = OperatingSystem.IsWindows() ? fileName.ToUpperInvariant() : fileName;
			var mutexName = "Greg.Xrm.Command.Settings." + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));

			// Mutex ownership belongs to a thread. Keep the entire critical section
			// synchronous on one worker, including acquisition and release.
			return Task.Run(() =>
			{
				using var mutex = new Mutex(false, mutexName, new NamedWaitHandleOptions
				{
					CurrentUserOnly = true,
					CurrentSessionOnly = false
				});
				try
				{
					mutex.WaitOne();
				}
				catch (AbandonedMutexException)
				{
					// Ownership was acquired after a previous writer exited. Atomic
					// replacement leaves the last committed settings file intact.
				}

				try
				{
					return action(fileName);
				}
				finally
				{
					mutex.ReleaseMutex();
				}
			});
		}

		private static T? Read<T>(string fileName)
		{
			if (!File.Exists(fileName)) return default;
			var text = File.ReadAllText(fileName);
			return typeof(T) == typeof(string) ? (T)(object)text : JsonConvert.DeserializeObject<T>(text);
		}

		private static void WriteFileAtomically<T>(string fileName, T value)
		{
			var temporaryFileName = fileName + $".{Guid.NewGuid():N}.tmp";
			try
			{
				using (var stream = CreatePrivateFile(temporaryFileName))
				{
					using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
					{
						if (typeof(T) == typeof(string)) writer.Write(value);
						else new JsonSerializer { Formatting = Formatting.Indented }.Serialize(writer, value);
					}
					stream.Flush(flushToDisk: true);
				}

				if (File.Exists(fileName) && !OperatingSystem.IsWindows())
					File.SetUnixFileMode(temporaryFileName, File.GetUnixFileMode(fileName));

				// Replace preserves the destination ACL on Windows. On Unix the
				// mode was copied above. Both files are on the same filesystem.
				ReplaceWithRetry(temporaryFileName, fileName);
			}
			finally
			{
				File.Delete(temporaryFileName);
			}
		}

		/// <summary>
		/// Moves <paramref name="source"/> over <paramref name="destination"/>, retrying briefly on transient failures.
		/// </summary>
		/// <remarks>
		/// <para>
		/// WHY: antivirus real-time protection (e.g. Microsoft Defender), search indexers and backup agents open a freshly
		/// written file for a few milliseconds without <c>FILE_SHARE_DELETE</c>. Meanwhile <c>ReplaceFile</c> fails with
		/// "Unable to remove the file to be replaced" or a sharing violation. PACX writes settings and the command
		/// history on every run, so this made commands fail at random and the SettingsRepositoryTest suite flaky.
		/// The cross-process mutex cannot prevent it: the other process is not PACX.
		/// </para>
		/// <para>
		/// SAFE TO RETRY: such a failure leaves both files untouched (verified: the destination keeps its old content,
		/// the replacement file is still there, and the same call succeeds once the handle is released). The total
		/// wait is about one second; after that the last error is propagated and the caller deletes the temporary file,
		/// so the previously committed value is preserved. A genuine permission problem therefore fails about one second
		/// later than before, which is acceptable for a settings write. The delay runs on the thread that owns the mutex,
		/// as the mutex requires.
		/// </para>
		/// <para>
		/// The destination is checked on every attempt: if it no longer exists (deleted meanwhile, or removed by a
		/// <c>ReplaceFile</c> that failed half-way) the replacement is moved into place, so the new value is not lost.
		/// </para>
		/// </remarks>
		internal static void ReplaceWithRetry(string source, string destination, int maxAttempts = 10, int baseDelayMilliseconds = 25)
		{
			for (var attempt = 1; ; attempt++)
			{
				try
				{
					if (File.Exists(destination))
						File.Replace(source, destination, null);
					else
						File.Move(source, destination);
					return;
				}
				catch (Exception ex) when (attempt < maxAttempts && IsTransientFileError(ex))
				{
					Thread.Sleep(baseDelayMilliseconds * attempt);
				}
			}
		}

		// Locks held by other processes surface as IOException (sharing violation, unable to remove) or, for
		// ReplaceFile, as UnauthorizedAccessException. Missing paths are not transient: retrying cannot fix them.
		private static bool IsTransientFileError(Exception ex) =>
			ex is UnauthorizedAccessException
			|| (ex is IOException and not FileNotFoundException and not DirectoryNotFoundException and not PathTooLongException);


		private static FileStream CreatePrivateFile(string fileName)
		{
			if (OperatingSystem.IsWindows())
			{
				using var identity = WindowsIdentity.GetCurrent();
				var security = new FileSecurity();
				security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
				security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl, AccessControlType.Allow));
				return FileSystemAclExtensions.Create(new FileInfo(fileName), FileMode.CreateNew,
					FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.None, security);
			}

			return new FileStream(fileName, new FileStreamOptions
			{
				Mode = FileMode.CreateNew,
				Access = FileAccess.Write,
				Share = FileShare.None,
				UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
			});
		}
	}
}
