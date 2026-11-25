namespace Greg.Xrm.Command.Services.Forms
{
	public interface IFormXmlValidator
	{
		bool TryValidate(string formXml, out IReadOnlyList<string> errors);
	}
}
