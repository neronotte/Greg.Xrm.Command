using System.Security.AccessControl;
using System.Security.Principal;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Settings
{
	[TestClass]
	public class SettingsRepositoryTest
	{
		private string folder = null!;
		private SettingsRepository CreateRepository() => new(new TestStorage(folder));

		[TestInitialize]
		public void Initialize() => folder = Path.Combine(Path.GetTempPath(), "pacx-settings-test-" + Guid.NewGuid().ToString("N"));

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
		}

		[TestMethod]
		public async Task ConcurrentUpdatesAcrossInstancesRetainEveryChange()
		{
			await Task.WhenAll(Enumerable.Range(0, 40).Select(_ =>
				CreateRepository().UpdateAsync<int>("counter", value => value + 1)));
			Assert.AreEqual(40, await CreateRepository().GetAsync<int>("counter"));
		}

		[TestMethod]
		public async Task SerializationFailurePreservesCommittedFileAndReleasesMutex()
		{
			var repository = CreateRepository();
			await repository.SetAsync("setting", new { Value = "original" });
			var original = await File.ReadAllBytesAsync(Path.Combine(folder, "setting.json"));
			await Assert.ThrowsAsync<JsonSerializationException>(() => repository.SetAsync("setting", new FailingValue()));
			CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(Path.Combine(folder, "setting.json")));
			Assert.IsEmpty(Directory.GetFiles(folder, "*.tmp"));
			await repository.SetAsync("setting", 42);
			Assert.AreEqual(42, await repository.GetAsync<int>("setting"));
		}

		[TestMethod]
		public async Task ReplacingFilePreservesPermissions()
		{
			var repository = CreateRepository();
			await repository.SetAsync("setting", 1);
			var file = new FileInfo(Path.Combine(folder, "setting.json"));
			if (OperatingSystem.IsWindows())
			{
				var before = file.GetAccessControl();
				await repository.SetAsync("setting", 2);
				var after = file.GetAccessControl();

				// Windows may add the SDDL AI control flag during replacement.
				// Verify protection and every access rule instead of the SDDL text.
				Assert.IsTrue(before.AreAccessRulesProtected);
				Assert.AreEqual(before.AreAccessRulesProtected, after.AreAccessRulesProtected);
				var beforeRules = before.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
				var afterRules = after.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
				Assert.AreEqual(beforeRules.Length, afterRules.Length);
				for (var i = 0; i < beforeRules.Length; i++)
				{
					var expected = beforeRules[i];
					var actual = afterRules[i];
					Assert.AreEqual(expected.IdentityReference.Value, actual.IdentityReference.Value);
					Assert.AreEqual(expected.FileSystemRights, actual.FileSystemRights);
					Assert.AreEqual(expected.AccessControlType, actual.AccessControlType);
					Assert.AreEqual(expected.InheritanceFlags, actual.InheritanceFlags);
					Assert.AreEqual(expected.PropagationFlags, actual.PropagationFlags);
					Assert.AreEqual(expected.IsInherited, actual.IsInherited);
				}
			}
			else
			{
				var privateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
				Assert.AreEqual(privateMode, File.GetUnixFileMode(file.FullName));
				var existingMode = privateMode | UnixFileMode.GroupRead;
				File.SetUnixFileMode(file.FullName, existingMode);
				await repository.SetAsync("setting", 2);
				Assert.AreEqual(existingMode, File.GetUnixFileMode(file.FullName));
			}
		}

		[TestMethod]
		public async Task RawStringSettingsRemainRawText()
		{
			var repository = CreateRepository();
			await repository.SetAsync("path", "a path with spaces");
			Assert.AreEqual("a path with spaces", await repository.GetAsync<string>("path"));
		}

		private sealed class TestStorage(string path) : IStorage
		{
			public DirectoryInfo GetOrCreateStorageFolder() => Directory.CreateDirectory(path);
		}

		private sealed class FailingValue
		{
			public string Value => throw new InvalidOperationException("Serialization failed deliberately.");
		}
	}
}
