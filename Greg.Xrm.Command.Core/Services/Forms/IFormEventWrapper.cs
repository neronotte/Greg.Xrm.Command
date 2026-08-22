using System.Xml.Linq;

namespace Greg.Xrm.Command.Services.Forms
{
	/// <summary>
	/// Encapsulates an <see cref="XElement"/> representing a dataverse form and
	/// exposes an object-oriented API to manipulate its formLibraries and events sections.
	/// </summary>
	public interface IFormEventWrapper
	{
		/// <summary>
		/// Ensures the given webresource is referenced in the formLibraries section.
		/// Returns true when the document has been changed.
		/// </summary>
		bool EnsureLibrary(string libraryName);

		/// <summary>
		/// Ensures the given handler is registered on the given event.
		/// For the onchange event, <paramref name="field"/> identifies the column to watch.
		/// Returns true when the document has been changed, false when the same
		/// function of the same library is already registered on the event with
		/// the same passExecutionContext setting.
		/// </summary>
		bool EnsureHandler(string eventName, string? field, string libraryName, string functionName, bool passExecutionContext);

		/// <summary>
		/// Removes the given handler from the given event. Event containers that
		/// remain empty are pruned, so the document ends up the same way the
		/// designer would leave it. For the onchange event, <paramref name="field"/>
		/// identifies the column being watched.
		/// Returns true when the document has been changed, false when the handler
		/// was not registered.
		/// </summary>
		bool RemoveHandler(string eventName, string? field, string libraryName, string functionName);

		/// <summary>
		/// Returns true when any handler of any event still references the given library.
		/// </summary>
		bool IsLibraryReferenced(string libraryName);

		/// <summary>
		/// Removes the given webresource from the formLibraries section, pruning
		/// the section when it remains empty.
		/// Returns true when the document has been changed.
		/// </summary>
		bool RemoveLibrary(string libraryName);

		/// <summary>
		/// Returns the wrapped, possibly modified, <see cref="XElement"/>.
		/// </summary>
		XElement ToXElement();
	}
}
