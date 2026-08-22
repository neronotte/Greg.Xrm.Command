using System.Xml.Linq;

namespace Greg.Xrm.Command.Services.Forms
{
	/// <summary>
	/// Default implementation of <see cref="IFormWrapperFactory"/>, creating
	/// <see cref="FormEventWrapper"/> instances.
	/// </summary>
	public class FormWrapperFactory : IFormWrapperFactory
	{
		public IFormEventWrapper CreateWrapper(XElement form)
		{
			return new FormEventWrapper(form);
		}
	}
}
