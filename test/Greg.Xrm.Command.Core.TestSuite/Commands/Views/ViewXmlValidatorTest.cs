using System.Xml.Linq;
using Greg.Xrm.Command.Services.Views;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ViewXmlValidatorTest
	{
		private readonly IViewXmlValidator validator = new ViewXmlValidator();

		[TestMethod]
		public void AcceptsQueryFeaturesWithoutAddingSchemaDefaults()
		{
			var document = XDocument.Parse("""
				<fetch version="1.0" mapping="logical" distinct="false">
				  <entity name="account">
				    <attribute name="name" alias="accountname"/>
				    <order attribute="name" descending="true"/>
				    <filter>
				      <condition attribute="statecode" operator="in"><value>0</value><value>1</value></condition>
				      <filter type="or"><condition attribute="name" operator="not-null"/></filter>
				    </filter>
				    <link-entity name="contact" alias="contact" from="contactid" to="primarycontactid" link-type="outer">
				      <attribute name="fullname"/>
				    </link-entity>
				  </entity>
				</fetch>
				""");
			var before = new XDocument(document);
			validator.ValidateFetchXml(document);
			Assert.IsTrue(XNode.DeepEquals(before, document));
		}

		[TestMethod]
		public void AcceptsLayoutCustomControlsAndImageProvidersWithoutChangingThem()
		{
			var document = XDocument.Parse("""
				<grid name="resultset" object="1" jump="name" select="1" icon="1" preview="0">
				  <row name="result" id="accountid">
				    <cell name="name" width="150" disableSorting="1" imageproviderwebresource="new_icons.js" imageproviderfunctionname="getIcon"/>
				  </row>
				  <controlDescriptions>
				    <controlDescription forControl="grid">
				      <customControl id="{11111111-1111-1111-1111-111111111111}" formFactor="1">
				        <parameters><EnableEditing type="Enum">1</EnableEditing><CustomParameter arbitrary="value"/></parameters>
				      </customControl>
				    </controlDescription>
				  </controlDescriptions>
				</grid>
				""");
			var before = new XDocument(document);
			validator.ValidateLayoutXml(document);
			Assert.IsTrue(XNode.DeepEquals(before, document));
		}

		[TestMethod]
		public void ReportsTheInvalidAttributeAndItsLine()
		{
			var document = XDocument.Parse("<grid>\n<row id='accountid'>\n<cell name='name' width='wide'/>\n</row>\n</grid>", LoadOptions.SetLineInfo);
			var error = Assert.ThrowsExactly<ArgumentException>(() => validator.ValidateLayoutXml(document));
			StringAssert.Contains(error.Message, "LayoutXML");
			StringAssert.Contains(error.Message, "width");
			StringAssert.Contains(error.Message, "line 3");
		}

		[TestMethod]
		[DataRow("<filter/>")]
		[DataRow("<fetch xmlns='urn:unexpected'><entity name='account'/></fetch>")]
		public void RejectsOtherSchemaRootsAndNamespaces(string xml)
		{
			Assert.ThrowsExactly<ArgumentException>(() => validator.ValidateFetchXml(XDocument.Parse(xml)));
		}

		[TestMethod]
		public void DocumentSchemaLocationCannotOverrideEmbeddedSchema()
		{
			var document = XDocument.Parse("""
				<fetch xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="https://example.invalid/accept-anything.xsd">
				  <entity name="account"><unexpected/></entity>
				</fetch>
				""");
			var error = Assert.ThrowsExactly<ArgumentException>(() => validator.ValidateFetchXml(document));
			StringAssert.Contains(error.Message, "unexpected");
		}
		[TestMethod]
		[DataRow("savedqueryid")]
		[DataRow("userqueryid")]
		public void AcceptsViewIdentifiersReturnedByDataverse(string attribute)
		{
			var document = XDocument.Parse($"<fetch {attribute}='11111111-1111-1111-1111-111111111111'><entity name='account'><attribute name='name'/></entity></fetch>");
			validator.ValidateFetchXml(document);
		}

		[TestMethod]
		[DataRow("0")]
		[DataRow("1")]
		[DataRow("true")]
		[DataRow("false")]
		public void AcceptsBooleanGridFlags(string value)
		{
			var document = XDocument.Parse($"<grid select='{value}' icon='{value}' preview='{value}'><row id='accountid'><cell name='name'/></row></grid>");
			validator.ValidateLayoutXml(document);
		}

		[TestMethod]
		public void AcceptsImageProviderAttributeCasingReturnedByDataverse()
		{
			var document = XDocument.Parse("<grid><row id='accountid'><cell name='name' imageProviderWebresource=''/></row></grid>");
			validator.ValidateLayoutXml(document);
		}

		[TestMethod]
		[DataRow("savedqueryid")]
		[DataRow("userqueryid")]
		public void RejectsMalformedViewIdentifiers(string attribute)
		{
			var document = XDocument.Parse($"<fetch {attribute}='not-a-guid'><entity name='account'/></fetch>");
			Assert.ThrowsExactly<ArgumentException>(() => validator.ValidateFetchXml(document));
		}

		[TestMethod]
		[DataRow("<grid icon='yes'><row id='accountid'><cell name='name'/></row></grid>")]
		[DataRow("<grid><row id='accountid'><cell name='name' unexpected='value'/></row></grid>")]
		public void CompatibilityDeclarationsStillRejectInvalidLayoutAttributes(string layout)
		{
			Assert.ThrowsExactly<ArgumentException>(() => validator.ValidateLayoutXml(XDocument.Parse(layout)));
		}

	}
}
