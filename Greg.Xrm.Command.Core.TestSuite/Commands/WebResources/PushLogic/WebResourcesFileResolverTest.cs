namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	[TestClass]
	public class WebResourcesFileResolverTest
	{
		private WebResourceFilesResolver resolver;
		
		[TestInitialize]
		public void Initialize()
		{
			this.resolver = new WebResourceFilesResolver();
		}


		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Resolve_WithNullPrefix_ThrowsException()
		{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			resolver.ResolveFiles(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		}



		[TestMethod]
		public void Resolve_WithSingleFileInRootFolder_ReturnsTheFile()
		{
			var root = Utils.CreateLocalTempFolder();
			var filename = "greg_file.js";
			Utils.CreateFile(root, filename);


			try
			{
				var folders = new WebResourceFolders(root, root, "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(1, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, filename), file.LocalPath);
				Assert.AreEqual(filename, file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}



		[TestMethod]
		public void Resolve_With3FilesInRootFolder_ReturnsTheCorrectFiles()
		{
			var root = Utils.CreateLocalTempFolder();
			var filenames = new List<string> { "greg_file.js", "greg_common.png", "test_shouldNotAppear" };
			filenames.Sort();
			filenames.ForEach(f => Utils.CreateFile(root, f));


			try
			{
				var folders = new WebResourceFolders(root, root, "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(2, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, filenames[0]), file.LocalPath);
				Assert.AreEqual(filenames[0], file.RemotePath);

				file = files[1];
				Assert.AreEqual(Path.Combine(root, filenames[1]), file.LocalPath);
				Assert.AreEqual(filenames[1], file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}



		[TestMethod]
		public void Resolve_With1FileInSubfolder()
		{
			var root = Utils.CreateLocalTempFolder();
			var filename = "greg_\\prova.js";
			Utils.CreateFile(root, filename);


			try
			{
				var folders = new WebResourceFolders(root, root, "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(1, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, filename), file.LocalPath);
				Assert.AreEqual(filename.Replace("\\", "/"), file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}



		[TestMethod]
		public void Resolve_With1FileInSubfolder2()
		{
			var root = Utils.CreateLocalTempFolder();
			var filename = "greg_\\prova\\prova.js";
			Utils.CreateFile(root, filename);


			try
			{
				var folders = new WebResourceFolders(root, root, "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(1, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, filename), file.LocalPath);
				Assert.AreEqual(filename.Replace("\\", "/"), file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}



		[TestMethod]
		public void Resolve_complex01()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, "greg_\\excel\\page.xlsx");
			Utils.CreateFile(root, "greg_\\pages\\index.html");
			Utils.CreateFile(root, "greg_\\pages\\index.css");
			Utils.CreateFile(root, "greg_\\pages\\index.js");
			Utils.CreateFile(root, "greg_\\pages\\index.docx");
			Utils.CreateFile(root, "greg_\\scripts\\account.js");
			Utils.CreateFile(root, "greg_\\scripts\\page.docx");
			Utils.CreateFile(root, "greg_account.js");
			Utils.CreateFile(root, "account.js");


			try
			{
				var folders = new WebResourceFolders(root, Path.Combine(root, "greg_\\pages"), "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(3, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, "greg_\\pages\\index.css"), file.LocalPath);
				Assert.AreEqual("greg_\\pages\\index.css".Replace("\\", "/"), file.RemotePath);

				file = files[1];
				Assert.AreEqual(Path.Combine(root, "greg_\\pages\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\pages\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[2];
				Assert.AreEqual(Path.Combine(root, "greg_\\pages\\index.js"), file.LocalPath);
				Assert.AreEqual("greg_\\pages\\index.js".Replace("\\", "/"), file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}

		[TestMethod]
		public void Resolve_complex02()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, "greg_\\excel\\page.xlsx");
			Utils.CreateFile(root, "greg_\\pages\\index.html");
			Utils.CreateFile(root, "greg_\\pages\\home.html");
			Utils.CreateFile(root, "greg_\\pages\\index.docx");
			Utils.CreateFile(root, "greg_\\scripts\\account.js");
			Utils.CreateFile(root, "greg_\\scripts\\page.docx");
			Utils.CreateFile(root, "greg_account.js");
			Utils.CreateFile(root, "account.js");


			try
			{
				var folders = new WebResourceFolders(root, Path.Combine(root, "greg_\\pages\\*.html"), "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(2, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, "greg_\\pages\\home.html"), file.LocalPath);
				Assert.AreEqual("greg_\\pages\\home.html".Replace("\\", "/"), file.RemotePath);

				file = files[1];
				Assert.AreEqual(Path.Combine(root, "greg_\\pages\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\pages\\index.html".Replace("\\", "/"), file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}

		[TestMethod]
		public void Resolve_complex03()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, "greg_\\folder1\\index.html");
			Utils.CreateFile(root, "greg_\\folder2\\index.html");
			Utils.CreateFile(root, "greg_page.html");


			try
			{
				var folders = new WebResourceFolders(root, Path.Combine(root, "greg_\\**\\*.html"), "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(2, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder1\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder1\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[1];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder2\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder2\\index.html".Replace("\\", "/"), file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}

		[TestMethod]
		public void Resolve_complex04()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, "greg_\\folder1\\subfolder\\subfolder\\index.html");
			Utils.CreateFile(root, "greg_\\folder2\\index.html");
			Utils.CreateFile(root, "greg_\\folder2\\subfolder\\subfolder\\index.html");
			Utils.CreateFile(root, "greg_\\folder3\\index.html");
			Utils.CreateFile(root, "greg_page.html");


			try
			{
				var folders = new WebResourceFolders(root, Path.Combine(root, "greg_\\**\\*.html"), "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(4, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder1\\subfolder\\subfolder\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder1\\subfolder\\subfolder\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[1];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder2\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder2\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[2];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder2\\subfolder\\subfolder\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder2\\subfolder\\subfolder\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[3];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder3\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder3\\index.html".Replace("\\", "/"), file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}

		[TestMethod]
		public void Resolve_complex05()
		{
			var root = Utils.CreateLocalTempFolder();
			Utils.CreateFile(root, "greg_\\folder1\\subfolder\\subfolder\\index.html");
			Utils.CreateFile(root, "greg_\\folder2\\index.html");
			Utils.CreateFile(root, "greg_\\folder2\\subfolder\\subfolder\\index.html");
			Utils.CreateFile(root, "greg_\\folder3\\index.html");
			Utils.CreateFile(root, "greg_page.html");
			Utils.CreateFile(root, "greg_page.js");


			try
			{
				var folders = new WebResourceFolders(root, Path.Combine(root, "**\\*.html"), "greg");
				var files = resolver.ResolveFiles(folders);
				Assert.IsNotNull(files);
				Assert.AreEqual(5, files.Count);

				var file = files[0];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder1\\subfolder\\subfolder\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder1\\subfolder\\subfolder\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[1];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder2\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder2\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[2];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder2\\subfolder\\subfolder\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder2\\subfolder\\subfolder\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[3];
				Assert.AreEqual(Path.Combine(root, "greg_\\folder3\\index.html"), file.LocalPath);
				Assert.AreEqual("greg_\\folder3\\index.html".Replace("\\", "/"), file.RemotePath);

				file = files[4];
				Assert.AreEqual(Path.Combine(root, "greg_page.html"), file.LocalPath);
				Assert.AreEqual("greg_page.html".Replace("\\", "/"), file.RemotePath);
			}
			finally
			{
				Utils.DeleteFolder(root);
			}
		}
	}
}
