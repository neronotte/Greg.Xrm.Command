using System.Xml.Linq;

namespace Greg.Xrm.Command.Services.Forms
{
	/// <summary>
	/// Creates <see cref="IFormEventWrapper"/> instances that wrap a given form <see cref="XElement"/>.
	/// </summary>
	public interface IFormWrapperFactory
	{
		/// <summary>
		/// Creates a new <see cref="IFormEventWrapper"/> that wraps the given form.
		/// </summary>
		IFormEventWrapper CreateWrapper(XElement form);
	}
}
