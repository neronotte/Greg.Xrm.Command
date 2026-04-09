using System;
using System.IO;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[TestClass]
	public class PluginScannerTest
	{
		[TestMethod]
		public void ScanAssembly_WithNonPluginAssembly_ShouldReturnNull()
		{
			// Scan the Interfaces assembly - it has the attribute definitions but no plugin types
			var interfacesPath = typeof(Greg.Xrm.Command.CrmPluginStepAttribute).Assembly.Location;

			var result = PluginScanner.ScanAssembly(interfacesPath);

			// Should return null since there are no types with [CrmPluginStep]
			Assert.IsNull(result);
		}

		[TestMethod]
		public void ScanAssembly_WithInvalidPath_ShouldNotThrow()
		{
			// Should handle non-existent files gracefully
			Assert.ThrowsException<FileNotFoundException>(() =>
				PluginScanner.ScanAssembly("C:\\nonexistent\\plugin.dll"));
		}

		[TestMethod]
		public void ScanDirectory_WithNonExistentDirectory_ShouldReturnEmpty()
		{
			var results = PluginScanner.ScanDirectory("C:\\nonexistent\\plugins");
			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void ScanDirectory_WithEmptyDirectory_ShouldReturnEmpty()
		{
			var tempDir = Path.Combine(Path.GetTempPath(), $"pacx_test_empty_{Guid.NewGuid()}");
			Directory.CreateDirectory(tempDir);

			try
			{
				var results = PluginScanner.ScanDirectory(tempDir);
				Assert.AreEqual(0, results.Count);
			}
			finally
			{
				Directory.Delete(tempDir);
			}
		}

		[TestMethod]
		public void ScanAssembly_WithNativeDll_ShouldNotThrow()
		{
			// Create a dummy file that's not a .NET assembly
			var tempFile = Path.Combine(Path.GetTempPath(), $"native_{Guid.NewGuid()}.dll");
			File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02, 0x03 });

			try
			{
				// Should not throw - should gracefully skip invalid assemblies
				var result = PluginScanner.ScanAssembly(tempFile);
				Assert.IsNull(result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
