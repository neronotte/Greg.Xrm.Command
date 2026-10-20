using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.Views
{
	public interface IViewXmlEditor
	{
		string GetTableName(string fetchXml);
		(string FetchXml, string LayoutXml) CreateFromFetchXml(string fetchXml, EntityMetadata metadata);
		(string FetchXml, string LayoutXml) SetFetchXml(string? layoutXml, string fetchXml, string tableName);
		string SetFilter(string? fetchXml, string filterXml);
		(string FetchXml, string LayoutXml) SetColumns(string? fetchXml, string? layoutXml, string columnsText);
		(string FetchXml, string LayoutXml, IReadOnlyList<string> UnusedAttributes) SetView(string fetchXml, string layoutXml, string tableName);
	}
}
