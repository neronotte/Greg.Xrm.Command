using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Greg.Xrm.Command.Services.Views
{
	public class ViewXmlValidator : IViewXmlValidator
	{
		private readonly XmlSchemaSet fetchSchema;
		private readonly XmlSchemaSet layoutSchema;

		public ViewXmlValidator()
		{
			fetchSchema = LoadSchema("Fetch.xsd");
			layoutSchema = LoadSchema("LayoutXml.xsd");
		}

		public void ValidateFetchXml(XDocument document) => Validate(document, fetchSchema, "fetch", "FetchXML");

		public void ValidateLayoutXml(XDocument document) => Validate(document, layoutSchema, "grid", "LayoutXML");

		private XmlSchemaSet LoadSchema(string fileName)
		{
			var resourceName = $"Greg.Xrm.Command.Services.Views.Schemas.{fileName}";
			using var stream = typeof(ViewXmlValidator).Assembly.GetManifestResourceStream(resourceName)
				?? throw new InvalidOperationException($"Embedded view schema '{fileName}' was not found.");
			using var reader = XmlReader.Create(stream, new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null
			});
			var schemas = new XmlSchemaSet { XmlResolver = null };
			schemas.Add(null, reader);
			schemas.Compile();
			return schemas;
		}

		private static void Validate(XDocument document, XmlSchemaSet schema, string rootName, string label)
		{
			if (document.Root?.Name != rootName)
				throw new ArgumentException($"{label} must have a <{rootName}> root element.");

			document.Validate(schema, (_, args) =>
			{
				var location = args.Exception.LineNumber > 0
					? $" at line {args.Exception.LineNumber}, position {args.Exception.LinePosition}"
					: string.Empty;
				throw new ArgumentException($"{label} does not match the view schema{location}: {args.Message}", args.Exception);
			});
		}
	}
}
