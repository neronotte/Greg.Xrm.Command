using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.Ribbon
{
	public class RibbonDiffService(IRibbonComponentRepository repository) : IRibbonDiffService
	{
		private static readonly Lazy<XmlSchemaSet> ApplicationSchema = new(() => LoadSchema(false));
		private static readonly Lazy<XmlSchemaSet> TableSchema = new(() => LoadSchema(true));

		// Microsoft Dataverse schemas, version 9.0.0.2090:
		// https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/ribbon-core-schema
		// https://download.microsoft.com/download/B/9/7/B97655A4-4E46-4E51-BA0A-C669106D563F/Schemas.zip
		private static XmlSchemaSet LoadSchema(bool tableRibbon)
		{
			static XmlSchema ReadSchema(string name)
			{
				using var stream = typeof(RibbonDiffService).Assembly.GetManifestResourceStream(
					$"Greg.Xrm.Command.Services.Ribbon.Schemas.{name}")
					?? throw new InvalidOperationException($"Embedded ribbon schema {name} was not found.");
				using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
				return XmlSchema.Read(reader, null)!;
			}

			var core = ReadSchema("RibbonCore.xsd");
			foreach (XmlSchemaInclude include in core.Includes)
				include.Schema = ReadSchema(include.SchemaLocation!);
			if (tableRibbon)
			{
				// The standalone root defaults to the global type; entity ribbons use this type in customizations.xml.
				var root = core.Items.OfType<XmlSchemaElement>().Single(e => e.Name == "RibbonDiffXml");
				root.SchemaTypeName = new XmlQualifiedName("RibbonEntityDiffXmlType");
			}
			var schemas = new XmlSchemaSet { XmlResolver = null };
			schemas.Add(core);
			schemas.Compile();
			return schemas;
		}

		public void ValidateXml(XElement ribbon, string tableName)
		{
			if (ribbon.Name != "RibbonDiffXml")
				throw new XmlSchemaValidationException("The input file must have a RibbonDiffXml root element.");
			var schemas = string.IsNullOrWhiteSpace(tableName) ? ApplicationSchema.Value : TableSchema.Value;
			var document = new XDocument(ribbon);
			document.Validate(schemas, (_, error) =>
			{
				var exception = error.Exception;
				throw new XmlSchemaValidationException(
					$"Invalid RibbonDiffXml at line {exception.LineNumber}, position {exception.LinePosition}: {error.Message}", exception);
			});
		}

		private const int MaximumEntities = 5;
		private const int MaximumComponents = MaximumEntities + 1; // Five tables and one application ribbon.
		private const int DoNotIncludeSubcomponents = 1;
		private const int IncludeAsShellOnly = 2;

		public async Task<string?> ValidateSolutionAsync(
			IOrganizationServiceAsync2 crm, Guid solutionId, string solutionName,
			Guid componentId, ComponentType componentType, CancellationToken cancellationToken)
		{
			// Fetch one row more than the largest permitted solution. Seven rows already prove it is unsafe.
			var components = await repository.GetSolutionComponentsAsync(crm, solutionId, MaximumComponents + 1, cancellationToken);

			var tables = new List<RibbonComponent>();
			var ribbons = new List<RibbonComponent>();
			foreach (var component in components)
			{
				var type = component.Type;
				if (type == (int)ComponentType.Entity)
				{
					tables.Add(component);
					if (tables.Count > MaximumEntities)
						return $"Solution <{solutionName}> contains more than {MaximumEntities} tables. Use a dedicated solution with at most {MaximumEntities} segmented tables and Application Ribbons. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
					if (component.RootBehavior is not (DoNotIncludeSubcomponents or IncludeAsShellOnly))
						return $"Solution <{solutionName}> contains a table with all subcomponents or unknown segmentation. Add each table without subcomponents (or as a shell), and remove other table components. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
				}
				else if (type == (int)ComponentType.RibbonCustomization)
				{
					ribbons.Add(component);
					if (ribbons.Count > 1)
						return $"Solution <{solutionName}> contains more than one ribbon customization. Only Application Ribbons is allowed. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
				}
				else
					return $"Solution <{solutionName}> contains an unsupported component type. Only segmented tables and Application Ribbons are allowed. {await DescribeComponentAsync(crm, component, components, cancellationToken)}";
			}

			if (componentType == ComponentType.Entity && !tables.Any(c => c.ObjectId == componentId))
				return $"Solution <{solutionName}> does not contain the target table. Add it without subcomponents or pass a suitable solution with --solution RibbonDiff.";

			if (componentType == ComponentType.RibbonCustomization && !ribbons.Any(c => c.ObjectId == componentId))
				return $"Solution <{solutionName}> does not contain Application Ribbons. Add that component or pass a suitable solution with --solution RibbonDiff.";

			if (ribbons.Count == 1)
			{
				var applicationRibbon = await repository.GetApplicationRibbonIdsAsync(crm, false, ribbons[0].ObjectId, cancellationToken);
				if (applicationRibbon.Count != 1)
					return $"Solution <{solutionName}> contains a ribbon customization that is not Application Ribbons. {await DescribeComponentAsync(crm, ribbons[0], components, cancellationToken)}";
			}

			return null;
		}

		private async Task<string> DescribeComponentAsync(
			IOrganizationServiceAsync2 crm, RibbonComponent component, IEnumerable<RibbonComponent> components, CancellationToken cancellationToken)
		{
			var typeNumber = component.Type;
			var typeName = typeNumber.HasValue && Enum.IsDefined(typeof(ComponentType), typeNumber.Value)
				? ((ComponentType)typeNumber.Value).ToString()
				: "Unknown";
			var objectId = component.ObjectId;
			var details = new StringBuilder($"First invalid component: type={typeName} ({typeNumber?.ToString() ?? "unknown"}), objectId={objectId}, solutionComponentId={component.Id}");
			var rootId = component.RootSolutionComponentId;
			if (rootId.HasValue)
				details.Append($", rootSolutionComponentId={rootId.Value}");
			var behavior = component.RootBehavior;
			if (behavior.HasValue)
				details.Append($", rootComponentBehavior={behavior.Value}");

			try
			{
				string? tableName = null;
				string? itemName = null;
				if (typeNumber == (int)ComponentType.Attribute && objectId != Guid.Empty)
				{
					try
					{
						var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(new RetrieveAttributeRequest
						{
							MetadataId = objectId,
							RetrieveAsIfPublished = true
						}, cancellationToken);
						tableName = response.AttributeMetadata?.EntityLogicalName;
						itemName = response.AttributeMetadata?.LogicalName;
					}
					catch (OperationCanceledException) { throw; }
					catch (Exception) { /* A removed field can still have an intact root table component. */ }
				}
				else if (typeNumber == (int)ComponentType.Entity && objectId != Guid.Empty)
				{
					var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
					{
						MetadataId = objectId,
						EntityFilters = EntityFilters.Entity,
						RetrieveAsIfPublished = true
					}, cancellationToken);
					tableName = response.EntityMetadata?.LogicalName;
				}
				else if (typeNumber is (int)ComponentType.SystemForm or (int)ComponentType.SavedQuery or (int)ComponentType.SavedQueryVisualization)
				{
					(tableName, itemName) = await repository.GetComponentNamesAsync(crm, (ComponentType)typeNumber.Value, objectId, cancellationToken);
				}
				if (tableName == null && rootId.HasValue)
				{
					var root = components.FirstOrDefault(c => c.Id == rootId.Value && c.Type == (int)ComponentType.Entity);
					if (root != null)
					{
						var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
						{
							MetadataId = root.ObjectId,
							EntityFilters = EntityFilters.Entity,
							RetrieveAsIfPublished = true
						}, cancellationToken);
						tableName = response.EntityMetadata?.LogicalName;
					}
				}
				if (!string.IsNullOrWhiteSpace(tableName)) details.Append($", table={tableName}");
				if (!string.IsNullOrWhiteSpace(itemName)) details.Append($", name={itemName}");
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception) { /* A stale component may no longer have retrievable metadata. Keep its IDs in the error. */ }
			return details.Append('.').ToString();
		}

		public async Task<(Guid componentId, ComponentType componentType)> ResolveComponentAsync(
			IOrganizationServiceAsync2 crm, string tableName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				var ribbons = await repository.GetApplicationRibbonIdsAsync(crm, true, null, cancellationToken);
				if (ribbons.Count != 1)
					throw new InvalidOperationException("Could not uniquely identify the active application ribbon customization.");
				return (ribbons[0], ComponentType.RibbonCustomization);
			}

			var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
			{
				LogicalName = tableName,
				EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity
			}, cancellationToken);
			if (response.EntityMetadata.MetadataId is not Guid metadataId)
				throw new InvalidOperationException($"Table <{tableName}> has no metadata ID.");
			return (metadataId, ComponentType.Entity);
		}

		public XElement Read(byte[] solutionBytes, string tableName)
		{
			using var stream = new MemoryStream(solutionBytes, writable: false);
			using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
			var entry = archive.GetEntry("customizations.xml") ??
				throw new InvalidOperationException("customizations.xml was not found in the exported solution.");
			using var entryStream = entry.Open();
			return new XElement(FindRibbonDiff(XDocument.Load(entryStream), tableName));
		}

		public byte[] Replace(byte[] solutionBytes, string tableName, XElement replacement)
		{
			if (replacement.Name != "RibbonDiffXml")
				throw new ArgumentException("The input file must have a RibbonDiffXml root element. The expanded XML from ribbon get cannot be imported.");
			using var archive = new SolutionZipArchive(solutionBytes);
			archive.UpdateEntryXml("customizations.xml", document =>
			{
				FindRibbonDiff(document, tableName).ReplaceWith(new XElement(replacement));
				return true;
			});
			return archive.ToArray();
		}

		private static XElement FindRibbonDiff(XDocument document, string tableName)
		{
			var root = document.Root;
			var ribbon = root?.Name != "ImportExportXml" ? null :
				string.IsNullOrWhiteSpace(tableName) ? root.Element("RibbonDiffXml") :
				root.Element("Entities")?.Elements("Entity")
					.FirstOrDefault(entity => string.Equals((string?)entity.Element("Name"), tableName, StringComparison.OrdinalIgnoreCase))
					?.Element("RibbonDiffXml");
			return ribbon ?? throw new InvalidOperationException("RibbonDiffXml was not found in the exported solution.");
		}
	}
}
