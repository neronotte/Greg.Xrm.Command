using System.Xml;
using System.Xml.Schema;

namespace Greg.Xrm.Command.Services.Forms
{
	/// <summary>Validates form definitions using the embedded FormXML schema and its ribbon dependencies.</summary>
	public class FormXmlValidator : IFormXmlValidator
	{
		private readonly Lazy<XmlSchemaSet> schemas = new(CreateSchemas);

		public bool TryValidate(string formXml, out IReadOnlyList<string> errors)
		{
			var messages = new List<string>();
			errors = messages;
			if (string.IsNullOrWhiteSpace(formXml))
			{
				messages.Add("Form XML is empty.");
				return false;
			}

			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				ValidationType = ValidationType.Schema,
				Schemas = schemas.Value
			};
			settings.ValidationEventHandler += (_, e) =>
			{
				if (e.Severity == XmlSeverityType.Error)
					messages.Add($"Line {e.Exception.LineNumber}, position {e.Exception.LinePosition}: {e.Message}");
			};

			try
			{
				using var textReader = new StringReader(formXml);
				using var reader = XmlReader.Create(textReader, settings);
				reader.MoveToContent();
				if (reader.LocalName != "form" || reader.NamespaceURI.Length != 0)
					messages.Add("The input XML must contain one <form> root element without a namespace.");
				while (reader.Read()) { }
			}
			catch (XmlException ex)
			{
				messages.Add(ex.Message);
			}

			return messages.Count == 0;
		}

		private static XmlSchemaSet CreateSchemas()
		{
			var assembly = typeof(FormXmlValidator).Assembly;
			string[] names = ["FormXml.xsd", "RibbonCore.xsd", "RibbonTypes.xsd", "RibbonWSS.xsd"];
			var loaded = new Dictionary<string, XmlSchema>();
			foreach (var name in names)
			{
				using var stream = assembly.GetManifestResourceStream($"Greg.Xrm.Command.Services.Forms.Schemas.{name}")
					?? throw new InvalidOperationException($"Embedded schema {name} not found.");
				loaded.Add(name, XmlSchema.Read(stream, null)
					?? throw new InvalidOperationException($"Unable to read schema {name}."));
			}

			// Resolve includes from embedded resources, so validation never depends on external files or network access.
			foreach (var schema in loaded.Values)
				foreach (XmlSchemaExternal include in schema.Includes)
					include.Schema = loaded[include.SchemaLocation!];

			var schemas = new XmlSchemaSet { XmlResolver = null };
			schemas.Add(loaded["FormXml.xsd"]);
			schemas.Compile();
			return schemas;
		}
	}
}
