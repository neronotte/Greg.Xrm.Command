using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	[TestClass]
	public class FolderResolverTest
	{
		private FolderResolver resolver;

		[TestInitialize]
		public void Initialize()
		{
			this.resolver = new FolderResolver();
		}



		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Resolve_WithoutPublisher_ShouldThrowArgumentNullException()
		{
			this.resolver.ResolveFrom(null, string.Empty);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Resolve_WithFullyQualifiedPath_WithoutProjectRoot_ShouldThrowArgumentNullException()
		{
			var path = @"c:\temp\folder";
			this.resolver.ResolveFrom(path, "greg");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Resolve_WithDefaultPath_WithoutProjectRoot_ShouldThrowArgumentNullException()
		{
			this.resolver.ResolveFrom(null, "greg");
		}


		[TestMethod]
		public void Resolve_WithFullyQualifiedPath_ShouldReturnFolder()
		{
			var root = Utils.CreateTempFolder();
			Utils.CreateFile(root, ".wr.pacx", string.Empty);
			Utils.CreateFolder(root, "greg_\\images");
			Utils.CreateFolder(root, "greg_\\script");
			Utils.CreateFolder(root, "greg_\\src");

			try
			{
				var currentFolder = Path.Combine(root, "greg_\\script");

				var result = this.resolver.ResolveFrom(currentFolder, "greg");

				Assert.IsNotNull(result);
				Assert.AreEqual("greg", result.PublisherPrefix);
				Assert.AreEqual(currentFolder, result.RequestedPath);
				Assert.AreEqual(root, result.ProjectRootPath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}


		[TestMethod]
		public void Resolve_WithRelativePath_ShouldReturnFolder()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, ".wr.pacx", string.Empty);
			Utils.CreateFolder(root, "greg_\\images");
			Utils.CreateFolder(root, "greg_\\script");
			Utils.CreateFolder(root, "greg_\\src");

			var currentFolderName = root[(root.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];

			try
			{
				var currentFolder = currentFolderName + "\\greg_\\script";

				var result = this.resolver.ResolveFrom(currentFolder, "greg");

				Assert.IsNotNull(result);
				Assert.AreEqual("greg", result.PublisherPrefix);
				Assert.AreEqual(Path.Combine(Environment.CurrentDirectory, currentFolder), result.RequestedPath);
				Assert.AreEqual(root, result.ProjectRootPath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}


		[TestMethod]
		public void Resolve_WithRootPath_ShouldReturnFolder()
		{
			var currentDir = Environment.CurrentDirectory;
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, ".wr.pacx", string.Empty);
			Utils.CreateFolder(root, "greg_\\images");
			Utils.CreateFolder(root, "greg_\\script");
			Utils.CreateFolder(root, "greg_\\src");



			try
			{
				Environment.CurrentDirectory = Path.Combine(root, "greg_\\script");

				var result = this.resolver.ResolveFrom("$", "greg");

				Assert.IsNotNull(result);
				Assert.AreEqual("greg", result.PublisherPrefix);
				Assert.AreEqual(root, result.RequestedPath);
				Assert.AreEqual(root, result.ProjectRootPath);
			}
			finally
			{
				Environment.CurrentDirectory = currentDir;
				Utils.DeleteFolder(root);
			}
		}


		[TestMethod]
		public void Resolve_WithRootPathAndSubfolder_ShouldReturnFolder()
		{
			var currentDir = Environment.CurrentDirectory;
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, ".wr.pacx", string.Empty);
			Utils.CreateFolder(root, "greg_\\images");
			Utils.CreateFolder(root, "greg_\\script");
			Utils.CreateFolder(root, "greg_\\src");

			try
			{
				Environment.CurrentDirectory = Path.Combine(root, "greg_\\script");

				var result = this.resolver.ResolveFrom("$\\pages", "greg");

				Assert.IsNotNull(result);
				Assert.AreEqual("greg", result.PublisherPrefix);
				Assert.AreEqual(Path.Combine(root, "pages"), result.RequestedPath);
				Assert.AreEqual(root, result.ProjectRootPath);
			}
			finally
			{
				Environment.CurrentDirectory = currentDir;
				Utils.DeleteFolder(root);
			}
		}


		[TestMethod]
		public void Resolve_WithSpecialChars_ShouldReturnFolder01()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, ".wr.pacx", string.Empty);
			Utils.CreateFolder(root, "greg_\\images");
			Utils.CreateFolder(root, "greg_\\script");
			Utils.CreateFolder(root, "greg_\\src");

			try
			{
				var folder = Path.Combine(root, "greg_\\**\\*.txt");
				var result = this.resolver.ResolveFrom(folder, "greg");

				Assert.IsNotNull(result);
				Assert.AreEqual("greg", result.PublisherPrefix);
				Assert.AreEqual(folder, result.RequestedPath);
				Assert.AreEqual(root, result.ProjectRootPath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}


		[TestMethod]
		public void Resolve_WithSpecialChars_ShouldReturnFolder02()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, ".wr.pacx", string.Empty);
			Utils.CreateFolder(root, "greg_\\images");
			Utils.CreateFolder(root, "greg_\\script");
			Utils.CreateFolder(root, "greg_\\src");

			try
			{
				var folder = Path.Combine(root, "**\\script\\*.txt");
				var result = this.resolver.ResolveFrom(folder, "greg");

				Assert.IsNotNull(result);
				Assert.AreEqual("greg", result.PublisherPrefix);
				Assert.AreEqual(folder, result.RequestedPath);
				Assert.AreEqual(root, result.ProjectRootPath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}


		[TestMethod]
		public void Resolve_WithSpecialChars_ShouldReturnFolder03()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, ".wr.pacx", string.Empty);
			Utils.CreateFolder(root, "greg_\\images");
			Utils.CreateFolder(root, "greg_\\script");
			Utils.CreateFolder(root, "greg_\\src");

			try
			{
				var folder = Path.Combine(root, "**");
				var result = this.resolver.ResolveFrom(folder, "greg");

				Assert.IsNotNull(result);
				Assert.AreEqual("greg", result.PublisherPrefix);
				Assert.AreEqual(root, result.RequestedPath);
				Assert.AreEqual(root, result.ProjectRootPath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}
	}
}
