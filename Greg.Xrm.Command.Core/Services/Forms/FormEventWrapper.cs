using System.Xml.Linq;

namespace Greg.Xrm.Command.Services.Forms
{
	/// <summary>
	/// Encapsulates an <see cref="XElement"/> representing a dataverse form, manipulating
	/// the formLibraries and events sections so that the resulting document has the same
	/// structure the form designer writes.
	/// </summary>
	public class FormEventWrapper(XElement form) : IFormEventWrapper
	{

		/// <summary>
		/// Ensures the given webresource is referenced in the formLibraries section.
		/// Returns true when the document has been changed.
		/// </summary>
		public bool EnsureLibrary(string libraryName)
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
		public bool EnsureHandler(string eventName, string? field, string libraryName, string functionName, bool passExecutionContext)
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

		/// <summary>
		/// Removes the given handler from the given event. Event containers that
		/// remain empty are pruned, so the document ends up the same way the
		/// designer would leave it. For the onchange event, <paramref name="field"/>
		/// identifies the column being watched.
		/// Returns true when the document has been changed, false when the handler
		/// was not registered.
		/// </summary>
		public bool RemoveHandler(string eventName, string? field, string libraryName, string functionName)
		{
			var events = form.Element("events");
			var eventElement = events?.Elements("event")
				.FirstOrDefault(e => string.Equals(e.Attribute("name")?.Value, eventName, StringComparison.OrdinalIgnoreCase)
					&& (field == null || string.Equals(e.Attribute("attribute")?.Value, field, StringComparison.OrdinalIgnoreCase)));

			var handlers = eventElement?.Element("Handlers");
			var handler = handlers?.Elements("Handler")
				.FirstOrDefault(h => string.Equals(h.Attribute("functionName")?.Value, functionName, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(h.Attribute("libraryName")?.Value, libraryName, StringComparison.OrdinalIgnoreCase));

			if (handler == null)
			{
				return false;
			}

			handler.Remove();

			if (!handlers!.Elements().Any())
			{
				eventElement!.Remove();
			}

			if (!events!.Elements().Any())
			{
				events.Remove();
			}

			return true;
		}

		/// <summary>
		/// Returns true when any handler of any event still references the given library.
		/// </summary>
		public bool IsLibraryReferenced(string libraryName)
		{
			return form.Element("events")?
				.Descendants("Handler")
				.Any(h => string.Equals(h.Attribute("libraryName")?.Value, libraryName, StringComparison.OrdinalIgnoreCase)) ?? false;
		}

		/// <summary>
		/// Removes the given webresource from the formLibraries section, pruning
		/// the section when it remains empty.
		/// Returns true when the document has been changed.
		/// </summary>
		public bool RemoveLibrary(string libraryName)
		{
			var libraries = form.Element("formLibraries");
			var library = libraries?.Elements("Library")
				.FirstOrDefault(l => string.Equals(l.Attribute("name")?.Value, libraryName, StringComparison.OrdinalIgnoreCase));

			if (library == null)
			{
				return false;
			}

			library.Remove();

			if (!libraries!.Elements().Any())
			{
				libraries.Remove();
			}

			return true;
		}

		public XElement ToXElement()
		{
			return form;
		}
	}
}
