using System.Xml.Linq;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class FormEventXmlEditorTest
	{
		private static XElement CreateEmptyForm() => new("form", new XElement("tabs"));

		// ── EnsureLibrary ─────────────────────────────────────────────────────

		[TestMethod]
		public void EnsureLibrary_ShouldCreateSectionAndEntry_WhenMissing()
		{
			var form = CreateEmptyForm();

			var changed = FormEventXmlEditor.EnsureLibrary(form, "myprefix_scripts.js");

			Assert.IsTrue(changed);
			var library = form.Element("formLibraries")?.Element("Library");
			Assert.IsNotNull(library);
			Assert.AreEqual("myprefix_scripts.js", library.Attribute("name")?.Value);
			Assert.IsFalse(string.IsNullOrWhiteSpace(library.Attribute("libraryUniqueId")?.Value));
		}

		[TestMethod]
		public void EnsureLibrary_ShouldDoNothing_WhenLibraryAlreadyReferenced()
		{
			var form = CreateEmptyForm();
			FormEventXmlEditor.EnsureLibrary(form, "myprefix_scripts.js");

			var changed = FormEventXmlEditor.EnsureLibrary(form, "MYPREFIX_scripts.js");

			Assert.IsFalse(changed);
			Assert.AreEqual(1, form.Element("formLibraries")!.Elements("Library").Count());
		}

		[TestMethod]
		public void EnsureLibrary_ShouldAppendToExistingSection()
		{
			var form = CreateEmptyForm();
			FormEventXmlEditor.EnsureLibrary(form, "myprefix_a.js");

			var changed = FormEventXmlEditor.EnsureLibrary(form, "myprefix_b.js");

			Assert.IsTrue(changed);
			Assert.AreEqual(2, form.Element("formLibraries")!.Elements("Library").Count());
		}

		// ── EnsureHandler ─────────────────────────────────────────────────────

		[TestMethod]
		public void EnsureHandler_ShouldCreateEventAndHandler_ForOnLoad()
		{
			var form = CreateEmptyForm();

			var changed = FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.onLoad", true);

			Assert.IsTrue(changed);
			var eventElement = form.Element("events")?.Element("event");
			Assert.IsNotNull(eventElement);
			Assert.AreEqual("onload", eventElement.Attribute("name")?.Value);
			Assert.IsNull(eventElement.Attribute("attribute"));

			var handler = eventElement.Element("Handlers")?.Element("Handler");
			Assert.IsNotNull(handler);
			Assert.AreEqual("My.Account.onLoad", handler.Attribute("functionName")?.Value);
			Assert.AreEqual("myprefix_scripts.js", handler.Attribute("libraryName")?.Value);
			Assert.AreEqual("true", handler.Attribute("enabled")?.Value);
			Assert.AreEqual("true", handler.Attribute("passExecutionContext")?.Value);
			Assert.IsFalse(string.IsNullOrWhiteSpace(handler.Attribute("handlerUniqueId")?.Value));
		}

		[TestMethod]
		public void EnsureHandler_ShouldSetAttribute_ForOnChange()
		{
			var form = CreateEmptyForm();

			var changed = FormEventXmlEditor.EnsureHandler(form, "onchange", "name", "myprefix_scripts.js", "My.Account.onNameChange", true);

			Assert.IsTrue(changed);
			var eventElement = form.Element("events")?.Element("event");
			Assert.IsNotNull(eventElement);
			Assert.AreEqual("onchange", eventElement.Attribute("name")?.Value);
			Assert.AreEqual("name", eventElement.Attribute("attribute")?.Value);
		}

		[TestMethod]
		public void EnsureHandler_ShouldBeIdempotent_ForSameFunctionAndLibrary()
		{
			var form = CreateEmptyForm();
			FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.onLoad", true);

			var changed = FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.onLoad", true);

			Assert.IsFalse(changed);
			Assert.AreEqual(1, form.Descendants("Handler").Count());
		}

		[TestMethod]
		public void EnsureHandler_ShouldAppendSecondHandler_OnSameEvent()
		{
			var form = CreateEmptyForm();
			FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.onLoad", true);

			var changed = FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.initRibbon", true);

			Assert.IsTrue(changed);
			Assert.AreEqual(1, form.Element("events")!.Elements("event").Count(), "Both handlers should live under the same event element.");
			Assert.AreEqual(2, form.Descendants("Handler").Count());
		}

		[TestMethod]
		public void EnsureHandler_ShouldCreateSeparateEvents_ForDifferentFields()
		{
			var form = CreateEmptyForm();
			FormEventXmlEditor.EnsureHandler(form, "onchange", "name", "myprefix_scripts.js", "My.Account.onNameChange", true);

			var changed = FormEventXmlEditor.EnsureHandler(form, "onchange", "telephone1", "myprefix_scripts.js", "My.Account.onPhoneChange", true);

			Assert.IsTrue(changed);
			Assert.AreEqual(2, form.Element("events")!.Elements("event").Count());
		}

		[TestMethod]
		public void EnsureHandler_ShouldRespectPassContextFalse()
		{
			var form = CreateEmptyForm();

			FormEventXmlEditor.EnsureHandler(form, "onsave", null, "myprefix_scripts.js", "My.Account.onSave", false);

			var handler = form.Descendants("Handler").Single();
			Assert.AreEqual("false", handler.Attribute("passExecutionContext")?.Value);
		}

		[TestMethod]
		public void EnsureHandler_ShouldReuseExistingEventElement_FromTheDesigner()
		{
			// simulates a form where the designer already registered another handler
			var form = new XElement("form",
				new XElement("tabs"),
				new XElement("events",
					new XElement("event",
						new XAttribute("name", "onload"),
						new XAttribute("application", "false"),
						new XAttribute("active", "false"),
						new XElement("Handlers",
							new XElement("Handler",
								new XAttribute("functionName", "Other.onLoad"),
								new XAttribute("libraryName", "myprefix_other.js"),
								new XAttribute("handlerUniqueId", "{11111111-1111-1111-1111-111111111111}"),
								new XAttribute("enabled", "true"),
								new XAttribute("parameters", ""),
								new XAttribute("passExecutionContext", "true"))))));

			var changed = FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.onLoad", true);

			Assert.IsTrue(changed);
			Assert.AreEqual(1, form.Element("events")!.Elements("event").Count());
			Assert.AreEqual(2, form.Descendants("Handler").Count());
		}

		// ── element ordering ──────────────────────────────────────────────────

		[TestMethod]
		public void EventsShouldComeBeforeFormLibraries_WhenBothAreCreated()
		{
			var form = CreateEmptyForm();

			FormEventXmlEditor.EnsureLibrary(form, "myprefix_scripts.js");
			FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.onLoad", true);

			var children = form.Elements().Select(e => e.Name.LocalName).ToList();
			var eventsIndex = children.IndexOf("events");
			var librariesIndex = children.IndexOf("formLibraries");
			Assert.IsTrue(eventsIndex >= 0 && librariesIndex >= 0);
			Assert.IsTrue(eventsIndex < librariesIndex, "The events section must precede formLibraries, like in designer-exported forms.");
		}

		[TestMethod]
		public void EventsShouldComeBeforeFormLibraries_RegardlessOfCallOrder()
		{
			var form = CreateEmptyForm();

			FormEventXmlEditor.EnsureHandler(form, "onload", null, "myprefix_scripts.js", "My.Account.onLoad", true);
			FormEventXmlEditor.EnsureLibrary(form, "myprefix_scripts.js");

			var children = form.Elements().Select(e => e.Name.LocalName).ToList();
			Assert.IsTrue(children.IndexOf("events") < children.IndexOf("formLibraries"));
		}
	}
}
