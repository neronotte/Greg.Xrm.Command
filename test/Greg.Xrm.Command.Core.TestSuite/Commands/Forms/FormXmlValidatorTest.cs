using Greg.Xrm.Command.Services.Forms;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class FormXmlValidatorTest
	{
		internal const string ValidFormXml = """
			<form headerdensity="High" showImage="true">
			  <tabs><tab name="general" visible="true">
			    <labels><label description="General" languagecode="1033" /></labels>
			    <columns><column width="100%"><sections><section name="details">
			      <rows><row><cell id="{11111111-1111-1111-1111-111111111111}">
			        <labels><label description="Name" languagecode="1033" /></labels>
			        <control id="name" datafieldname="name" classid="{4273EDBD-AC1D-40D3-9FB2-095C621B552D}" />
			      </cell></row></rows>
			    </section></sections></column></columns>
			  </tab></tabs>
			  <RibbonDiffXml><CustomActions /><Templates><RibbonTemplates Id="Mscrm.Templates" /></Templates><CommandDefinitions /><RuleDefinitions><TabDisplayRules /><DisplayRules /><EnableRules /></RuleDefinitions><LocLabels /></RibbonDiffXml>
			</form>
			""";

		[TestMethod]
		public void AcceptsCompleteFormWithRibbonDependenciesAndXmlDeclaration()
		{
			var validator = new FormXmlValidator();
			Assert.IsTrue(validator.TryValidate(ValidFormXml, out var errors), string.Join(Environment.NewLine, errors));
			Assert.IsTrue(validator.TryValidate("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + ValidFormXml, out errors), string.Join(Environment.NewLine, errors));
		}

		[TestMethod]
		[DataRow("cardSections")]
		[DataRow("expandAvailableSpace")]
		[DataRow("singleComponent")]
		public void AcceptsTabContentType(string contentType)
		{
			var xml = ValidFormXml.Replace("<tab name=\"general\"", $"<tab name=\"general\" contenttype=\"{contentType}\"");
			Assert.IsTrue(new FormXmlValidator().TryValidate(xml, out var errors), string.Join(Environment.NewLine, errors));
		}

		[TestMethod]
		[DataRow("")]
		[DataRow("<form />")]
		[DataRow("<form><tabs /></form>")]
		[DataRow("<form><tabs><tab /></tabs></form>")]
		[DataRow("<form><tabs><tab><columns><column /></columns></tab></tabs></form>")]
		[DataRow("<form><tabs><tab visible=\"sometimes\"><columns><column width=\"100%\" /></columns></tab></tabs></form>")]
		[DataRow("<form xmlns=\"urn:unknown\"><tabs /></form>")]
		[DataRow("<form>")]
		[DataRow("<!DOCTYPE form [<!ENTITY tab 'general'>]><form><tabs><tab name=\"&tab;\"><columns><column width=\"100%\" /></columns></tab></tabs></form>")]
		public void RejectsInvalidStructureAndValues(string xml)
		{
			Assert.IsFalse(new FormXmlValidator().TryValidate(xml, out var errors));
			Assert.IsNotEmpty(errors);
		}

		[TestMethod]
		public void ReportsLocationAndMultipleErrors()
		{
			var xml = ValidFormXml.Replace("visible=\"true\"", "visible=\"sometimes\"").Replace("width=\"100%\"", "width=\"invalid\"");
			Assert.IsFalse(new FormXmlValidator().TryValidate(xml, out var errors));
			Assert.IsTrue(errors.Count >= 2);
			StringAssert.Contains(errors[0], "Line 2");
			StringAssert.Contains(string.Join(Environment.NewLine, errors), "width");
		}
	}
}
