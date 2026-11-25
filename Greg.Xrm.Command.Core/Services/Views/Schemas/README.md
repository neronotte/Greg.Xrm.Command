# View schemas

These schemas are embedded in the core assembly. Validation is local and never downloads schemas or resolves document-provided schema locations.

Sources retrieved on 2026-10-20:

- `Fetch.xsd`: Microsoft's [published FetchXML schema](https://learn.microsoft.com/de-de/power-apps/developer/data-platform/fetchxml-schema), including its supporting types. The English schema URL now redirects to the element reference; the linked localized page still publishes the XSD. The published schema is the baseline; view compatibility changes are listed below.
- `LayoutXml.xsd`: the view `layoutxml/grid` definition extracted from Microsoft's [customization solutions file schema](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customization-solutions-file-schema). This definition includes `controlDescriptions` and custom control parameters. The `grid` element is promoted to a document root by removing its local `minOccurs`/`maxOccurs`. `ObjectTypeCodeType` and `GuidType` are copied from the same schema; its included `TrueFalse01Type` and `BoolOrEmpty` are copied from `Fetch.xsd`. The published customization schema's `https://www.w3.org/2001/XMLSchema` namespace is corrected to the standard `http://www.w3.org/2001/XMLSchema`. Other declarations and dependencies unrelated to view layout are omitted.

Compatibility declarations verified against read-only view definitions

- Fetch root `savedqueryid` and `userqueryid` attributes, typed as the existing `guid` type. The published schema omits these attributes;
- Grid `select` uses `xs:boolean`; `icon` and `preview` use `BoolOrEmpty`. These types are taken from the grid definition in `Fetch.xsd`, which accepts both numeric and textual boolean values. All four sampled personal views contain `icon="false"`, rejected by the customization schema's narrower `0`/`1` type.
- Cell `imageProviderWebresource`, in addition to the published lowercase `imageproviderwebresource`. Three sampled personal views contain the former spelling.

See [live validation results](../../../../docs/view-schema-live-validation.md) for the sample and outcomes. These declarations are explicit; other undeclared attributes remain invalid.

To refresh, replace `Fetch.xsd` with the published XSD and repeat the layout extraction above, then preserve or reassess the compatibility declarations using the regression tests and a live sample. Keep the schemas strict: do not add catch-all elements or attributes to bypass validation. Custom-control parameter contents remain open because Microsoft's schema explicitly defines them that way.

XSD validation checks structure and declared value types. `ViewXmlEditor` separately checks the main table, displayed columns, and FetchXML/LayoutXML consistency. Neither check resolves column metadata or replaces Dataverse's server-side validation. Newer platform features may require updating these schema snapshots.
