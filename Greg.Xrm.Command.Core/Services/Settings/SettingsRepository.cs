using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Settings
{
	public class SettingsRepository(IStorage storage) : ISettingsRepository
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

				if (File.Exists(fileName))
				{
					if (!OperatingSystem.IsWindows())
						File.SetUnixFileMode(temporaryFileName, File.GetUnixFileMode(fileName));

					// Replace preserves the destination ACL on Windows. On Unix the
					// mode was copied above. Both files are on the same filesystem.
					File.Replace(temporaryFileName, fileName, null);
				}
				else
				{
					File.Move(temporaryFileName, fileName);
				}
			}
			finally
			{
				File.Delete(temporaryFileName);
			}
		}

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
