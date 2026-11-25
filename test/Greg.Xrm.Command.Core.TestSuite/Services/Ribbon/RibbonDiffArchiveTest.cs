using System.IO.Compression;
using System.Xml.Linq;
using Greg.Xrm.Command.Model;

namespace Greg.Xrm.Command.Services.Ribbon
{
	[TestClass]
	public class RibbonDiffArchiveTest
	{
		private const string Customizations = "<ImportExportXml><RibbonDiffXml><Application/></RibbonDiffXml><Entities><Entity><Name>account</Name><RibbonDiffXml><OldAction/></RibbonDiffXml></Entity><Entity><Name>contact</Name><RibbonDiffXml><OtherTable/></RibbonDiffXml></Entity></Entities></ImportExportXml>";
		private readonly IRibbonDiffService service = new RibbonDiffService(new Mock<IRibbonComponentRepository>().Object);

		private static byte[] CreateZip(string xml = Customizations)
		{
			using var stream = new MemoryStream();
			using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
			{
				using (var writer = new StreamWriter(zip.CreateEntry("customizations.xml").Open())) writer.Write(xml);
				using (var writer = new StreamWriter(zip.CreateEntry("solution.xml").Open())) writer.Write("unchanged solution metadata");
			}
			return stream.ToArray();
		}

		[TestMethod]
		[DataRow("account", "OldAction")]
		[DataRow("ACCOUNT", "OldAction")]
		[DataRow("", "Application")]
		public void ReadsTheRequestedRibbonWithoutChangingTheArchive(string table, string expected)
		{
			var zip = CreateZip(); var before = zip.ToArray();
			StringAssert.Contains(service.Read(zip, table).ToString(), expected);
			CollectionAssert.AreEqual(before, zip);
		}

		[TestMethod]
		[DataRow("account")]
		[DataRow("")]
		public void ReplacesOnlyTheSelectedRibbonAndRetainsOtherArchiveEntries(string table)
		{
			var original = CreateZip();
			var updated = service.Replace(original, table, XElement.Parse("<RibbonDiffXml><NewAction/></RibbonDiffXml>"));
			StringAssert.Contains(service.Read(updated, table).ToString(), "NewAction");
			StringAssert.Contains(service.Read(updated, "contact").ToString(), "OtherTable");
			StringAssert.Contains(service.Read(updated, table.Length == 0 ? "account" : "").ToString(), table.Length == 0 ? "OldAction" : "Application");
			using var stream = new MemoryStream(updated);
			using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
			using var reader = new StreamReader(zip.GetEntry("solution.xml")!.Open());
			Assert.AreEqual("unchanged solution metadata", reader.ReadToEnd());
			StringAssert.Contains(service.Read(original, table).ToString(), table.Length == 0 ? "Application" : "OldAction");
		}

		[TestMethod]
		[DataRow("<ImportExportXml/>")]
		[DataRow("<Unexpected><RibbonDiffXml/></Unexpected>")]
		public void MissingRibbonFailsInsteadOfReturningEmptyXml(string xml)
		{
			Assert.ThrowsExactly<InvalidOperationException>(() => service.Read(CreateZip(xml), "account"));
		}

		[TestMethod]
		public void RejectsExpandedXmlAsAReplacement()
		{
			Assert.ThrowsExactly<ArgumentException>(() => service.Replace(CreateZip(), "account", XElement.Parse("<RibbonDefinitions/>")));
		}
	}
}
