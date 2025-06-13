using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.Data;
using System.Xml;

namespace Greg.Xrm.Command.Commands.Views.Model
{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
	/// <summary>
	/// The code of the class is extracted directly from XrmToolbox's ViewLayoutReplicator tool.
	/// Thanks to Tanguy Touzard for the original code.
	/// </summary>
	public static class Replicator
	{

		public static async Task<List<Tuple<string, string>>> PropagateLayoutAsync(
			IOrganizationServiceAsync2 crm,
			SavedQuery sourceView,
			IReadOnlyList<SavedQuery> targetViews,
			  bool includeLayout = true,
			  bool includeSorting = true,
			  bool includeComponents = true)
		{
			var tupleList = new List<Tuple<string, string>>();
			string empty = string.Empty;

			foreach (var targetView in targetViews)
			{
				if (targetView.Id == sourceView.Id)
				{
					continue;
				}


				var xmlDocument1 = new XmlDocument();
				xmlDocument1.LoadXml(targetView.layoutxml!);
				
				var xmlDocument2 = new XmlDocument();
				xmlDocument2.LoadXml(sourceView.layoutxml!);

				var xmlNodeList1 = xmlDocument2.SelectNodes("grid/row/cell");
				var source = new List<string>();
				if (includeLayout)
				{
					var attribute = xmlDocument1.SelectSingleNode("grid/row")?.Attributes["multiobjectidfield"];
					if (attribute != null)
						empty = attribute.Value;
					for (int count = xmlDocument1.SelectSingleNode("grid/row").ChildNodes.Count; count > 0; --count)
					{
						var childNode = xmlDocument1.SelectSingleNode("grid/row")?.ChildNodes[count - 1];
						if (childNode != null)
							xmlDocument1.SelectSingleNode("grid/row").RemoveChild(childNode);
					}
					foreach (XmlNode xmlNode in xmlNodeList1)
					{
						if (!xmlNode.Attributes["name"].Value.Contains('.') || targetView.querytype != SavedQueryQueryType.SubGrid)
						{
							source.Add(xmlNode.Attributes["name"].Value);
							XmlNode newChild = xmlDocument1.ImportNode(xmlNode.Clone(), true);
							xmlDocument1.SelectSingleNode("grid/row").AppendChild(newChild);
						}
					}
					targetView.layoutxml = xmlDocument1.OuterXml;
				}
				if (includeComponents && targetView.querytype != SavedQueryQueryType.AdvancedSearch && targetView.querytype != SavedQueryQueryType.LookupView && targetView.querytype != SavedQueryQueryType.QuickFindSearch)
				{
					var xmlNode1 = xmlDocument1.SelectSingleNode("grid");
					var oldChild = xmlNode1.SelectSingleNode("controlDescriptions");
					if (oldChild != null)
						xmlNode1.RemoveChild(oldChild);
					var xmlNode2 = xmlDocument2.SelectSingleNode("grid/controlDescriptions");
					if (xmlNode2 != null)
					{
						XmlNode newChild = xmlDocument1.ImportNode(xmlNode2.Clone(), true);
						xmlNode1.AppendChild(newChild);
					}
					targetView.layoutxml = xmlDocument1.OuterXml;
				}


				if (!string.IsNullOrEmpty(targetView.fetchxml))
				{
					var xmlDocument3 = new XmlDocument();
					xmlDocument3.LoadXml(targetView.fetchxml);

					var xmlDocument4 = new XmlDocument();
					xmlDocument4.LoadXml(sourceView.fetchxml!);
					
					var xmlNodeList2 = xmlDocument4.SelectNodes("fetch/entity/attribute");
					if (includeLayout)
					{
						foreach (XmlNode node in xmlNodeList2)
						{
							if (xmlDocument3.SelectSingleNode("fetch/entity/attribute[@name='" + node.Attributes["name"].Value + "']") == null)
							{
								XmlNode newChild = xmlDocument3.ImportNode(node, true);
								xmlDocument3.SelectSingleNode("fetch/entity").AppendChild(newChild);
							}
						}
						foreach (XmlNode xmlNode in xmlNodeList1)
						{
							string str = xmlNode.Attributes["name"].Value;
							if (!str.Contains('.') && xmlDocument3.SelectSingleNode("fetch/entity/attribute[@name='" + str + "']") == null)
							{
								XmlElement element = xmlDocument3.CreateElement("attribute");
								element.SetAttribute("name", str);
								xmlDocument3.SelectSingleNode("fetch/entity").AppendChild((XmlNode)element);
							}
						}
						if (!string.IsNullOrEmpty(sourceView.fetchxml))
						{
							foreach (XmlNode selectNode in xmlDocument4.SelectNodes("fetch/entity/link-entity"))
							{
								string alias = selectNode.Attributes["alias"].Value;
								if (source.FirstOrDefault<string>((Func<string, bool>)(c => c.StartsWith(alias + "."))) != null)
								{
									if (xmlDocument3.SelectSingleNode("fetch/entity/link-entity[@alias=\"" + alias + "\"]") == null)
									{
										XmlNode newChild = xmlDocument3.ImportNode(selectNode.Clone(), true);
										var attribute1 = newChild.Attributes["link-type"];
										if (attribute1 == null)
										{
											var attribute2 = xmlDocument3.CreateAttribute("link-type");
											attribute2.Value = "outer";
											newChild.Attributes.Append(attribute2);
										}
										else
											attribute1.Value = "outer";
										xmlDocument3.SelectSingleNode("fetch/entity").AppendChild(newChild);
									}
									var xmlNode = xmlDocument3.SelectSingleNode("fetch/entity/link-entity[@alias=\"" + alias + "\"]");
									for (int count = xmlNode.ChildNodes.Count; count > 0; --count)
									{
										if (xmlNode.ChildNodes[count - 1].Name == "attribute")
										{
											var childNode = xmlNode.ChildNodes[count - 1];
											if (childNode != null) xmlNode.RemoveChild(childNode);
										}
									}
									foreach (XmlNode childNode in selectNode.ChildNodes)
									{
										if (childNode.Name == "attribute" && xmlNode.SelectSingleNode("attribute[@name='" + childNode.Attributes["name"].Value + "']") == null)
										{
											var newChild = xmlDocument3.ImportNode(childNode.Clone(), true);
											xmlNode.AppendChild(newChild);
										}
									}
								}
							}
						}

						var stringList = new List<string>();
						foreach (XmlNode selectNode1 in xmlDocument3.SelectNodes("//attribute"))
						{
							if (selectNode1.Attributes["name"].Value != empty)
							{
								bool flag = false;
								foreach (XmlNode selectNode2 in xmlDocument2.SelectNodes("grid/row/cell"))
								{
									if (selectNode1.ParentNode.Name == "link-entity")
									{
										if (selectNode2.Attributes["name"].Value == selectNode1.ParentNode.Attributes["alias"].Value + "." + selectNode1.Attributes["name"].Value)
										{
											flag = true;
											break;
										}
									}
									else if (selectNode1.Attributes["name"].Value == selectNode1.ParentNode.Attributes["name"].Value + "id" || selectNode2.Attributes["name"].Value == selectNode1.Attributes["name"].Value)
									{
										flag = true;
										break;
									}
								}
								if (!flag)
								{
									if (selectNode1.ParentNode.Name == "link-entity")
										stringList.Add(selectNode1.ParentNode.Attributes["alias"].Value + "." + selectNode1.Attributes["name"].Value);
									else
										stringList.Add(selectNode1.Attributes["name"].Value);
								}
							}
							else
								break;
						}
						foreach (string str in stringList)
						{
							if (str.Contains('.'))
							{
								var oldChild = xmlDocument3.SelectSingleNode("fetch/entity/link-entity[@alias='" + str.Split('.')[0] + "']/attribute[@name='" + str.Split('.')[1] + "']");
								xmlDocument3.SelectSingleNode("fetch/entity/link-entity[@alias='" + oldChild.ParentNode.Attributes["alias"].Value + "']").RemoveChild(oldChild);
							}
							else
							{
								var oldChild = xmlDocument3.SelectSingleNode("fetch/entity/attribute[@name='" + str + "']");
								if (oldChild != null)
									xmlDocument3.SelectSingleNode("fetch/entity").RemoveChild(oldChild);
							}
						}
						foreach (XmlNode selectNode in xmlDocument3.SelectNodes("fetch/entity/link-entity"))
						{
							if (selectNode != null && selectNode.ChildNodes.Count == 0)
								xmlDocument3.SelectSingleNode("fetch/entity").RemoveChild(selectNode);
						}
					}


					if (includeSorting)
					{
						var xmlNodeList3 = xmlDocument4.SelectNodes("fetch/entity/order");
						var xmlNodeList4 = xmlDocument3.SelectNodes("fetch/entity/order");
						for (int count = xmlNodeList4.Count; count > 0; --count)
						{
							var oldChild = xmlNodeList4[count - 1];
							if (oldChild != null)
								xmlNodeList4[count - 1].ParentNode.RemoveChild(oldChild);
						}
						foreach (XmlNode node in xmlNodeList3)
						{
							XmlNode newChild = xmlDocument3.ImportNode(node, true);
							xmlDocument3.SelectSingleNode("fetch/entity").AppendChild(newChild);
						}
					}
					targetView.fetchxml = xmlDocument3.OuterXml;
				}


				try
				{
					await targetView.SaveOrUpdateAsync(crm);
				}
				catch (Exception ex)
				{
					tupleList.Add(new Tuple<string, string>(targetView.name ?? string.Empty, ex.Message));
				}
			}
			return tupleList;
		}
	}
#pragma warning restore CS8602 // Dereference of a possibly null reference.
}
