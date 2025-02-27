using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.Data;
using System.Xml;

namespace Greg.Xrm.Command.Commands.Views.Model
{
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
			bool includeLayout,
			bool includeSorting)
		{
			var tupleList = new List<Tuple<string, string>>();
			var empty = string.Empty;


			var sourceLayoutXml = new XmlDocument();
			sourceLayoutXml.LoadXml(sourceView.layoutxml!);
			var sourceCellList = sourceLayoutXml.SelectNodes("grid/row/cell") ?? XmlNodeListEmpty.Instance;



			var sourceFetchXml = new XmlDocument();
			sourceFetchXml.LoadXml(sourceView.fetchxml!);
			var sourceAttributeList = sourceFetchXml.SelectNodes("fetch/entity/attribute") ?? XmlNodeListEmpty.Instance;


			foreach (var targetView in targetViews.Where(x => x.Id != sourceView.Id))
			{
				var targetLayoytXml = new XmlDocument();
				targetLayoytXml.LoadXml(targetView.layoutxml!);


				var source = new List<string>();
				if (includeLayout)
				{
					var attribute = targetLayoytXml.SelectSingleNode("grid/row")?.Attributes?["multiobjectidfield"];
					if (attribute != null)
						empty = attribute.Value;

					// remove all the <cells> node from the target view layout xml.
					for (int count = targetLayoytXml.SelectSingleNode("grid/row")?.ChildNodes.Count ?? 0; count > 0; --count)
					{
						var childNode = targetLayoytXml.SelectSingleNode("grid/row")?.ChildNodes[count - 1];
						targetLayoytXml.SelectSingleNode("grid/row")?.RemoveChild(childNode);
					}



					foreach (XmlNode xmlNode in sourceCellList)
					{
						if (!xmlNode.Attributes["name"].Value.Contains(".") || targetView.querytype != SavedQueryQueryType.SubGrid)
						{
							source.Add(xmlNode.Attributes["name"].Value);

							var newChild = targetLayoytXml.ImportNode(xmlNode.Clone(), true);
							targetLayoytXml.SelectSingleNode("grid/row").AppendChild(newChild);
						}
					}
					targetView.layoutxml = targetLayoytXml.OuterXml;
				}








				if (!string.IsNullOrEmpty(targetView.fetchxml))
				{
					var targetFetchXml = new XmlDocument();
					targetFetchXml.LoadXml(targetView.fetchxml);
					if (includeLayout)
					{
						foreach (XmlNode node in sourceAttributeList)
						{
							if (targetFetchXml.SelectSingleNode("fetch/entity/attribute[@name='" + node.Attributes["name"].Value + "']") == null)
							{
								XmlNode newChild = targetFetchXml.ImportNode(node, true);
								targetFetchXml.SelectSingleNode("fetch/entity").AppendChild(newChild);
							}
						}
						foreach (XmlNode xmlNode in sourceCellList)
						{
							string str = xmlNode.Attributes["name"].Value;
							if (!str.Contains(".") && targetFetchXml.SelectSingleNode("fetch/entity/attribute[@name='" + str + "']") == null)
							{
								XmlElement element = targetFetchXml.CreateElement("attribute");
								element.SetAttribute("name", str);
								targetFetchXml.SelectSingleNode("fetch/entity").AppendChild((XmlNode)element);
							}
						}
						if (!string.IsNullOrEmpty(sourceView.fetchxml))
						{
							foreach (XmlNode selectNode in sourceFetchXml.SelectNodes("fetch/entity/link-entity"))
							{
								string alias = selectNode.Attributes["alias"].Value;
								if (source.FirstOrDefault(c => c.StartsWith(alias + ".")) != null)
								{
									if (targetFetchXml.SelectSingleNode("fetch/entity/link-entity[@alias=\"" + alias + "\"]") == null)
									{
										XmlNode newChild = targetFetchXml.ImportNode(selectNode.Clone(), true);
										XmlAttribute attribute1 = newChild.Attributes["link-type"];
										if (attribute1 == null)
										{
											XmlAttribute attribute2 = targetFetchXml.CreateAttribute("link-type");
											attribute2.Value = "outer";
											newChild.Attributes.Append(attribute2);
										}
										else
											attribute1.Value = "outer";
										targetFetchXml.SelectSingleNode("fetch/entity").AppendChild(newChild);
									}
									XmlNode xmlNode = targetFetchXml.SelectSingleNode("fetch/entity/link-entity[@alias=\"" + alias + "\"]");
									for (int count = xmlNode.ChildNodes.Count; count > 0; --count)
									{
										if (xmlNode.ChildNodes[count - 1].Name == "attribute")
										{
											XmlNode childNode = xmlNode.ChildNodes[count - 1];
											xmlNode.RemoveChild(childNode);
										}
									}
									foreach (XmlNode childNode in selectNode.ChildNodes)
									{
										if (childNode.Name == "attribute" && xmlNode.SelectSingleNode("attribute[@name='" + childNode.Attributes["name"].Value + "']") == null)
										{
											XmlNode newChild = targetFetchXml.ImportNode(childNode.Clone(), true);
											xmlNode.AppendChild(newChild);
										}
									}
								}
							}
						}
						List<string> stringList = new List<string>();
						foreach (XmlNode selectNode1 in targetFetchXml.SelectNodes("//attribute"))
						{
							if (!(selectNode1.Attributes["name"].Value == empty))
							{
								bool flag = false;
								foreach (XmlNode selectNode2 in sourceLayoutXml.SelectNodes("grid/row/cell"))
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
							if (str.Contains("."))
							{
								XmlNode oldChild = targetFetchXml.SelectSingleNode("fetch/entity/link-entity[@alias='" + str.Split('.')[0] + "']/attribute[@name='" + str.Split('.')[1] + "']");
								targetFetchXml.SelectSingleNode("fetch/entity/link-entity[@alias='" + oldChild.ParentNode.Attributes["alias"].Value + "']").RemoveChild(oldChild);
							}
							else
							{
								XmlNode oldChild = targetFetchXml.SelectSingleNode("fetch/entity/attribute[@name='" + str + "']");
								targetFetchXml.SelectSingleNode("fetch/entity").RemoveChild(oldChild);
							}
						}
						foreach (XmlNode selectNode in targetFetchXml.SelectNodes("fetch/entity/link-entity"))
						{
							if (selectNode != null && selectNode.ChildNodes.Count == 0)
								targetFetchXml.SelectSingleNode("fetch/entity").RemoveChild(selectNode);
						}
					}
					if (includeSorting)
					{
						var xmlNodeList3 = sourceFetchXml.SelectNodes("fetch/entity/order") ?? XmlNodeListEmpty.Instance;
						var xmlNodeList4 = targetFetchXml.SelectNodes("fetch/entity/order") ?? XmlNodeListEmpty.Instance;
						for (int count = xmlNodeList4.Count; count > 0; --count)
						{
							var oldChild = xmlNodeList4[count - 1];
							xmlNodeList4[count - 1].ParentNode.RemoveChild(oldChild);
						}
						foreach (XmlNode node in xmlNodeList3)
						{
							var newChild = targetFetchXml.ImportNode(node, true);
							targetFetchXml.SelectSingleNode("fetch/entity").AppendChild(newChild);
						}
					}
					targetView.fetchxml = targetFetchXml.OuterXml;
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
}
