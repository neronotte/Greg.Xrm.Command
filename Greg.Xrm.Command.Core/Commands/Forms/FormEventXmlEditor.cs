using System.Xml.Linq;
using System.Xml.XPath;

namespace Greg.Xrm.Command.Commands.Forms
{
	/// <summary>
	/// Manipulates the formLibraries and events sections of a formxml document,
	/// producing the same structure the form designer writes.
	/// </summary>
	public static class FormEventXmlEditor
	{
		/// <summary>
		/// Ensures the given webresource is referenced in the formLibraries section.
		/// Returns true when the document has been changed.
		/// </summary>
		public static bool EnsureLibrary(XElement form, string libraryName)
		{
			var libraries = form.Element("formLibraries");
			if (libraries == null)
			{
				libraries = new XElement("formLibraries");

				// in exported forms the events section comes before formLibraries
				var events = form.Element("events");
				if (events != null)
				{
					events.AddAfterSelf(libraries);
				}
				else
				{
					form.Add(libraries);
				}
			}

			var alreadyThere = libraries.Elements("Library")
				.Any(l => string.Equals(l.Attribute("name")?.Value, libraryName, StringComparison.OrdinalIgnoreCase));
			if (alreadyThere)
			{
				return false;
			}

			libraries.Add(new XElement("Library",
				new XAttribute("name", libraryName),
				new XAttribute("libraryUniqueId", Guid.NewGuid().ToString("B"))));
			return true;
		}

		/// <summary>
		/// Ensures the given handler is registered on the given event.
		/// For the onchange event, <paramref name="field"/> identifies the column to watch.
		/// Returns true when the document has been changed, false when the same
		/// function of the same library is already registered on the event with
		/// the same passExecutionContext setting.
		/// </summary>
		public static bool EnsureHandler(XElement form, string eventName, string? field, string libraryName, string functionName, bool passExecutionContext)
		{
			var events = form.Element("events");
			if (events == null)
			{
				events = new XElement("events");

				// in exported forms the events section comes before formLibraries
				var libraries = form.Element("formLibraries");
				if (libraries != null)
				{
					libraries.AddBeforeSelf(events);
				}
				else
				{
					form.Add(events);
				}
			}

			var eventElement = events.Elements("event")
				.FirstOrDefault(e =>
					string.Equals(e.Attribute("name")?.Value, eventName, StringComparison.OrdinalIgnoreCase)
					&& (field == null || string.Equals(e.Attribute("attribute")?.Value, field, StringComparison.OrdinalIgnoreCase)));

			if (eventElement == null)
			{
				eventElement = new XElement("event",
					new XAttribute("name", eventName),
					new XAttribute("application", "false"),
					new XAttribute("active", "false"));

				if (field != null)
				{
					eventElement.SetAttributeValue("attribute", field);
				}

				events.Add(eventElement);
			}

			var handlers = eventElement.Element("Handlers");
			if (handlers == null)
			{
				handlers = new XElement("Handlers");
				eventElement.Add(handlers);
			}

			var existing = handlers.Elements("Handler")
				.FirstOrDefault(h => string.Equals(h.Attribute("functionName")?.Value, functionName, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(h.Attribute("libraryName")?.Value, libraryName, StringComparison.OrdinalIgnoreCase));
			if (existing != null)
			{
				// the handler is already registered, but the requested
				// passExecutionContext setting may differ from the stored one
				var requested = passExecutionContext ? "true" : "false";
				if (string.Equals(existing.Attribute("passExecutionContext")?.Value, requested, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				existing.SetAttributeValue("passExecutionContext", requested);
				return true;
			}

			handlers.Add(new XElement("Handler",
				new XAttribute("functionName", functionName),
				new XAttribute("libraryName", libraryName),
				new XAttribute("handlerUniqueId", Guid.NewGuid().ToString("B")),
				new XAttribute("enabled", "true"),
				new XAttribute("parameters", ""),
				new XAttribute("passExecutionContext", passExecutionContext ? "true" : "false")));
			return true;
		}
	}
}
