using System.Security.AccessControl;
using System.Security.Principal;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Settings
{
	[TestClass]
	public class SettingsRepositoryTest
	{
		private string folder = null!;
		private IAtomicSettingsRepository CreateRepository() => new SettingsRepository(new TestStorage(folder));

		[TestInitialize]
		public void Initialize() => folder = Path.Combine(Path.GetTempPath(), "pacx-settings-test-" + Guid.NewGuid().ToString("N"));

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
		}

		[TestMethod]
		public void OriginalInterfaceRetainsLegacyContract()
		{
			CollectionAssert.AreEquivalent(
				new[] { "GetAsync", "SetAsync" },
				typeof(ISettingsRepository).GetMethods().Select(method => method.Name).ToArray());
		}

		[TestMethod]
		public async Task ConcurrentUpdatesAcrossInstancesRetainEveryChange()
		{
			await Task.WhenAll(Enumerable.Range(0, 40).Select(_ =>
				CreateRepository().UpdateAsync<int>("counter", value => value + 1)));
			Assert.AreEqual(40, await CreateRepository().GetAsync<int>("counter"));
		}

		[TestMethod]
		public async Task UpdateInvokesCallbackOnceWithDefaultAndReturnsPersistedValue()
		{
			var repository = CreateRepository();
			var invocationCount = 0;
			var result = await repository.UpdateAsync<int>("counter", value =>
			{
				invocationCount++;
				Assert.AreEqual(0, value);
				return 42;
			});

			Assert.AreEqual(1, invocationCount);
			Assert.AreEqual(42, result);
			Assert.AreEqual(result, await repository.GetAsync<int>("counter"));
		}

		[TestMethod]
		public async Task UpdateCallbackFailurePreservesCommittedValueAndReleasesMutex()
		{
			var repository = CreateRepository();
			await repository.SetAsync("counter", 42);

			await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync<int>("counter", _ =>
				throw new InvalidOperationException("Update failed deliberately.")));

			Assert.AreEqual(42, await repository.GetAsync<int>("counter"));
			Assert.AreEqual(43, await repository.UpdateAsync<int>("counter", value => value + 1));
		}

		[TestMethod]
		public void UpdateRejectsNullCallback()
		{
			Assert.Throws<ArgumentNullException>(() => CreateRepository().UpdateAsync<int>("counter", null!));
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

		// Antivirus/indexers briefly open freshly written files without FILE_SHARE_DELETE: this caused random
		// "Unable to remove the file to be replaced" failures. A handle without Delete sharing reproduces it.
		private FileStream HoldLikeAnAntivirus(string key) =>
			new(Path.Combine(folder, key + ".json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

		[TestMethod]
		public async Task SetRetriesWhileAnotherProcessBrieflyHoldsTheFile()
		{
			if (!OperatingSystem.IsWindows()) return; // Unix rename is not blocked by open handles.

			var repository = CreateRepository();
			await repository.SetAsync("setting", 1);

			Task write;
			using (HoldLikeAnAntivirus("setting"))
			{
				write = repository.SetAsync("setting", 2);
				await Task.Delay(150);
				Assert.IsFalse(write.IsCompleted, "Sanity check: the write must be blocked while the file is held.");
			}
			await write;

			Assert.AreEqual(2, await repository.GetAsync<int>("setting"));
			Assert.IsEmpty(Directory.GetFiles(folder, "*.tmp"));
		}

		[TestMethod]
		public async Task SetFailsWithoutLosingDataWhenTheFileStaysLocked()
		{
			if (!OperatingSystem.IsWindows()) return;

			var repository = CreateRepository();
			await repository.SetAsync("setting", 1);

			using (HoldLikeAnAntivirus("setting"))
			{
				await Assert.ThrowsAsync<IOException>(() => repository.SetAsync("setting", 2));
			}

			Assert.AreEqual(1, await repository.GetAsync<int>("setting"));
			Assert.IsEmpty(Directory.GetFiles(folder, "*.tmp"));
		}

		[TestMethod]
		public void ReplaceMovesTheReplacementWhenTheDestinationDisappeared()
		{
			Directory.CreateDirectory(folder);
			var source = Path.Combine(folder, "setting.json.tmp");
			var destination = Path.Combine(folder, "setting.json");
			File.WriteAllText(source, "new");

			SettingsRepository.ReplaceWithRetry(source, destination);

			Assert.AreEqual("new", File.ReadAllText(destination));
			Assert.IsFalse(File.Exists(source));
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
