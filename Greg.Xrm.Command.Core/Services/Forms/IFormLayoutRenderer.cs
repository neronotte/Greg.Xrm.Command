using System.Xml.Linq;
using Greg.Xrm.Command.Commands.Forms;

namespace Greg.Xrm.Command.Services.Forms
{
	public interface IFormLayoutRenderer
	{
		IEnumerable<string> Render(XElement form, LayoutDisplay display, string? formName = null);
	}
}
