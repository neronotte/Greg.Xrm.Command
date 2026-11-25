using System.Xml.Linq;

namespace Greg.Xrm.Command.Services.Views
{
	public interface IViewXmlValidator
	{
		void ValidateFetchXml(XDocument document);
		void ValidateLayoutXml(XDocument document);
	}
}
