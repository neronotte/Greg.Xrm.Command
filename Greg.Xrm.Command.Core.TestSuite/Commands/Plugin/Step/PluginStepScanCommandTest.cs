namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	[TestClass]
	public class PluginStepScanCommandTest
	{
		[TestMethod]
		public void ParseWithDefaultsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<PluginStepScanCommand>(
				"plugin", "step-scan",
				"-d", "C:\\plugins");

			Assert.AreEqual("C:\\plugins", command.Path);
			Assert.AreEqual("table", command.Format);
			Assert.IsFalse(command.Strict);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<PluginStepScanCommand>(
				"plugin", "step-scan",
				"-d", "C:\\plugins",
				"-f", "json",
				"--strict");

			Assert.AreEqual("C:\\plugins", command.Path);
			Assert.AreEqual("json", command.Format);
			Assert.IsTrue(command.Strict);
		}

		[TestMethod]
		public void ParseWithDirectoryPathShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<PluginStepScanCommand>(
				"plugin", "step-scan",
				"--dll", "C:\\plugins\\myplugin");

			Assert.AreEqual("C:\\plugins\\myplugin", command.Path);
		}
	}

	[TestClass]
	public class PluginStepScanCommandExecutorTest
	{
		[TestMethod]
		public async Task ExecuteWithNonExistentPathShouldFail()
		{
			var output = new OutputToMemory();
			var executor = new PluginStepScanCommandExecutor(output);

			var command = new PluginStepScanCommand
			{
				Path = "C:\\nonexistent\\path\\plugin.dll",
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(output.ToString().Contains("Path not found"));
		}

		[TestMethod]
		public async Task ExecuteWithEmptyDirectoryShouldSucceed()
		{
			var output = new OutputToMemory();
			var executor = new PluginStepScanCommandExecutor(output);
			var tempDir = Path.Combine(Path.GetTempPath(), $"pacx_test_empty_{Guid.NewGuid()}");
			Directory.CreateDirectory(tempDir);

			try
			{
				var command = new PluginStepScanCommand
				{
					Path = tempDir,
				};

				var result = await executor.ExecuteAsync(command, CancellationToken.None);

				Assert.IsTrue(result.IsSuccess);
			}
			finally
			{
				Directory.Delete(tempDir);
			}
		}
	}
}
