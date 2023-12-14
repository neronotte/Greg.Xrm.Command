using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services
{
	// Credits @darylabar
	public class AttributeDeletionService : IAttributeDeletionService
	{
		private readonly IOutput output;
		private readonly IWorkflowRepository workflowRepository;
		private readonly IProcessTriggerRepository processTriggerRepository;

		public AttributeDeletionService(
			IOutput output,
			IWorkflowRepository workflowRepository,
			IProcessTriggerRepository processTriggerRepository)
		{
			this.output = output;
			this.workflowRepository = workflowRepository;
			this.processTriggerRepository = processTriggerRepository;
		}




		public async Task DeleteAttributeAsync(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, DependencyList dependencies, bool? simulation = false)
		{
			await UpdateCharts(crm, attribute, dependencies.OfType(ComponentType.SavedQueryVisualization), simulation.GetValueOrDefault()); // mancano le userQueryVisualization
			await UpdateViews(crm, attribute, dependencies.OfType(ComponentType.SavedQuery), simulation.GetValueOrDefault());
			await UpdateForms(crm, attribute, dependencies.OfType(ComponentType.SystemForm), simulation.GetValueOrDefault());
			await UpdatePluginSteps(crm, attribute, dependencies.OfType(ComponentType.SDKMessageProcessingStep), simulation.GetValueOrDefault());
			await UpdatePluginImages(crm, attribute, dependencies.OfType(ComponentType.SDKMessageProcessingStepImage), simulation.GetValueOrDefault());
			await UpdateRelationships(crm, attribute, dependencies.OfType(ComponentType.Relationship), simulation.GetValueOrDefault());
			await UpdateMappings(crm, dependencies.OfType(ComponentType.AttributeMap), simulation.GetValueOrDefault());
			await UpdateWorkflows(crm, attribute, dependencies.OfType(ComponentType.Workflow), simulation.GetValueOrDefault());
			await PublishAll(crm);
		}



		private async Task UpdateCharts(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyCollection<Dependency> dependencies, bool simulation)
		{
			if (dependencies.Count == 0)
				return;

			var result = await RetrieveDataAsync(crm, dependencies, "savedqueryvisualization", "savedqueryvisualizationid", "name", "datadescription");

			var i = 0;
			foreach (var e in result)
			{
				++i;

				this.output.Write($"Updating savedqueryvisualization {i}/{result.Count} {e.GetAttributeValue<string>("name")}...");

				e["datadescription"] = RemoveFieldFromFetchXml(e.GetAttributeValue<string>("datadescription"), attribute.LogicalName);
				if (!simulation)
				{
					await crm.UpdateAsync(e);
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}


		private async Task UpdateViews(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyList<Dependency> dependencies, bool simulation)
		{
			if (dependencies.Count == 0)
				return;

			var result = await RetrieveDataAsync(crm, dependencies, "savedquery", "savedqueryid", "name", "querytype", "fetchxml", "layoutxml", "returnedtypecode");

			var i = 0;
			foreach (var e in result)
			{
				++i;

				this.output.Write($"Updating savedquery {i}/{result.Count} {e.GetAttributeValue<string>("name")}...");

				e["fetchxml"] = RemoveFieldFromFetchXml(e.GetAttributeValue<string>("fetchxml"), attribute.LogicalName);

				if (e.GetAttributeValue<string>("layoutxml") is not null)
				{
					e["layoutxml"] = RemoveFieldFromFetchXml(e.GetAttributeValue<string>("layoutxml"), attribute.LogicalName);
				}

				if (!simulation)
				{
					await crm.UpdateAsync(e);
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}



		private async Task UpdateForms(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyList<Dependency> dependencies, bool simulation)
		{
			/*
             * <row>
             *   <cell id="{056d159e-9144-d809-378b-9e04a7626953}" showlabel="true" locklevel="0">
             *     <labels>
             *       <label description="Points" languagecode="1033" />
             *     </labels>
             *     <control id="new_points" classid="{4273EDBD-AC1D-40d3-9FB2-095C621B552D}" datafieldname="new_points" disabled="true" />
             *   </cell>
             * </row>
             */


			if (dependencies.Count == 0)
				return;

			var result = await RetrieveDataAsync(crm, dependencies, "systemform", "formid", "name", "formxml");

			var i = 0;
			foreach (var e in result)
			{
				++i;
				this.output.Write($"Updating systemform {i}/{result.Count} {e.GetAttributeValue<string>("name")}...");

				var xml = e.GetAttributeValue<string>("formxml");
				var dataFieldStart = "datafieldname=\"" + attribute.LogicalName + "\"";
				var index = xml.IndexOf(dataFieldStart, StringComparison.OrdinalIgnoreCase);
				while (index >= 0)
				{
					index = xml.LastIndexOf("<cell ", index, StringComparison.OrdinalIgnoreCase);
					var cellEnd = xml.IndexOf("</cell>", index, StringComparison.OrdinalIgnoreCase) + "</cell>".Length;
					xml = xml.Remove(index, cellEnd - index);

					index = xml.IndexOf(dataFieldStart, index, StringComparison.OrdinalIgnoreCase);
				}
				e["formxml"] = xml;

				if (!simulation)
				{
					await crm.UpdateAsync(e);
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}



		private async Task UpdatePluginSteps(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyList<Dependency> dependencies, bool simulation)
		{
			if (dependencies.Count == 0)
				return;

			var result = await RetrieveDataAsync(crm, dependencies, "sdkmessageprocessingstep", "sdkmessageprocessingstepid", "name", "filteringattributes");

			var i = 0;
			foreach (var e in result)
			{
				++i;
				this.output.Write($"Updating sdkmessageprocessingstep {i}/{result.Count} {e.GetAttributeValue<string>("name")}...");

				e["filteringattributes"] = RemoveValueFromCsvValues(e.GetAttributeValue<string>("filteringattributes"), attribute.LogicalName);

				if (!simulation)
				{
					await crm.UpdateAsync(e);
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}



		private async Task UpdatePluginImages(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyList<Dependency> dependencies, bool simulation)
		{
			if (dependencies.Count == 0)
				return;

			var result = await RetrieveDataAsync(crm, dependencies, "sdkmessageprocessingstepimage", "sdkmessageprocessingstepimageid", "name", "attributes1");

			var i = 0;
			foreach (var e in result)
			{
				++i;
				this.output.Write($"Updating sdkmessageprocessingstepimage {i}/{result.Count} {e.GetAttributeValue<string>("name")}...");

				e["attributes1"] = RemoveValueFromCsvValues(e.GetAttributeValue<string>("attributes1"), attribute.LogicalName);

				if (!simulation)
				{
					await crm.UpdateAsync(e);
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}



		private async Task UpdateRelationships(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyList<Dependency> dependencies, bool simulation)
		{
			if (dependencies.Count == 0)
				return;

			var request = new RetrieveEntityRequest
			{
				EntityFilters = EntityFilters.Relationships,
				LogicalName = attribute.EntityLogicalName
			};

			this.output.Write($"Searching for relationships dependent by this column...");

			var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
			var tableMetadata = response.EntityMetadata;

			if (tableMetadata is null)
			{
				this.output.WriteLine("Done. No relationship found.", ConsoleColor.Green);
				return;
			}

			var oneToManyRelationships = tableMetadata.OneToManyRelationships
				.Where(r => r.ReferencedAttribute == attribute.LogicalName || r.ReferencingAttribute == attribute.LogicalName)
				.Select(r => r.SchemaName)
				.ToArray();
			var manyToManyRelationships = tableMetadata.ManyToManyRelationships
				.Where(r => r.Entity1IntersectAttribute == attribute.LogicalName || r.Entity2IntersectAttribute == attribute.LogicalName)
				.Select(r => r.SchemaName)
				.ToArray();

			var relationshipList = oneToManyRelationships.Union(manyToManyRelationships).ToList();

			this.output.WriteLine($"Done. Found {relationshipList.Count} relationships to delete.", ConsoleColor.Green);



			var i = 0;
			foreach (var relationship in relationshipList)
			{
				++i;
				this.output.Write($"Deleting relationship {i}/{relationshipList.Count} {relationship}...");

				if (!simulation)
				{
					await crm.ExecuteAsync(new DeleteRelationshipRequest
					{
						Name = relationship
					});
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}



		private async Task UpdateMappings(IOrganizationServiceAsync2 crm, IReadOnlyList<Dependency> dependencies, bool simulation)
		{
			if (dependencies.Count == 0)
				return;

			var ids = dependencies.Select(x => x.dependentcomponentobjectid).Cast<object>().ToArray();

			var query = new QueryExpression("attributemap");
			query.ColumnSet.AllColumns = false;
			query.Criteria.AddCondition("attributemapid", ConditionOperator.In, ids);
			query.Criteria.AddCondition("ismanaged", ConditionOperator.NotEqual, 1);
			query.Criteria.AddCondition("issystem", ConditionOperator.NotEqual, 1);
			query.NoLock = true;

			var result = await crm.RetrieveMultipleAsync(query);


			var i = 0;
			foreach (var e in result.Entities)
			{
				++i;
				this.output.Write($"Deleting attributemap {i}/{result.Entities.Count} {e.Id}...");
				if (!simulation)
				{
					await crm.DeleteAsync(e.LogicalName, e.Id);
				}
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}



		private async Task UpdateWorkflows(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, IReadOnlyList<Dependency> dependencies, bool simulation)
		{
			if (dependencies.Count == 0) return;

			var workflowList = await this.workflowRepository.GetByIdsAsync(crm, dependencies.Select(x => x.dependentcomponentobjectid));


			var i = 0;
			foreach (var workflow in workflowList)
			{
				++i;
				this.output.Write($"Updating workflow {i}/{workflowList.Count} {workflow.Id}...");

				var clone = new Workflow(workflow.Id)
				{
					xaml = RemoveParentXmlNodesWithTagValue(workflow.xaml, "mxswa:ActivityReference AssemblyQualifiedName=\"Microsoft.Crm.Workflow.Activities.StepComposite,", "mcwb:Control", "DataFieldName", attribute.LogicalName, "mxswa:ActivityReference")
				};

				var unsupportedXml = RemoveXmlNodesWithTagValue(workflow.xaml, "mxswa:GetEntityProperty", "Attribute", attribute.LogicalName);
				if (clone.xaml != unsupportedXml)
				{
					this.output.WriteLine("Failed. ", ConsoleColor.Red)
						.WriteLine("Attribute is used in a Business Rules Get Entity Property. This is unsupported for manual deletion. Delete the Business Rule ", ConsoleColor.Red)
						.Write(workflow.name, ConsoleColor.Red)
						.Write(" manually to be able to delete the attribute.", ConsoleColor.Red)
						.WriteLine();

					continue;
				}

				var activate = workflow.statecode?.Value == (int)Workflow.State.Activated;
				if (activate && !simulation)
				{
					await crm.ExecuteAsync(new SetStateRequest
					{
						EntityMoniker = workflow.ToEntityReference(),
						State = new OptionSetValue((int)Workflow.State.Draft),
						Status = new OptionSetValue((int)Workflow.Status.Draft)
					});
				}


				try
				{
					var triggers = await this.processTriggerRepository.GetByWorkflowIdAsync(crm, workflow.Id);
					if (!simulation)
					{
						foreach (var trigger in triggers)
						{
							await crm.DeleteAsync(trigger.EntityName, trigger.Id);
						}
					}

					if (workflow.triggeronupdateattributelist != null)
					{
						var newValue = RemoveValueFromCsvValues(workflow.triggeronupdateattributelist, attribute.LogicalName);
						clone.triggeronupdateattributelist = newValue;
					}

					if (!simulation)
					{
						await clone.SaveOrUpdateAsync(crm);
					}
				}
				finally
				{
					if (activate && !simulation)
					{
						await crm.ExecuteAsync(new SetStateRequest
						{
							EntityMoniker = workflow.ToEntityReference(),
							State = new OptionSetValue((int)Workflow.State.Activated),
							Status = new OptionSetValue((int)Workflow.Status.Activated)
						});
					}
				}

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}









		private async Task PublishAll(IOrganizationServiceAsync2 crm)
		{
			this.output.Write("Publishing all...");
			await crm.ExecuteAsync(new PublishAllXmlRequest());
			this.output.WriteLine("Done", ConsoleColor.Green);
		}




		public static async Task<IReadOnlyList<Entity>> RetrieveDataAsync(
			IOrganizationServiceAsync2 crm, 
			IReadOnlyCollection<Dependency> dependencies, 
			string tableName,
			string idColumn,
			params string[] columns)
		{
			if (dependencies.Count == 0)
				return Array.Empty<Entity>();

			var ids = dependencies.Select(x => x.dependentcomponentobjectid).Cast<object>().ToArray();


			var query = new QueryExpression(tableName);
			query.ColumnSet.AddColumns(columns);
			query.Criteria.AddCondition(idColumn, ConditionOperator.In, ids);
			query.NoLock = true;

			var result = await crm.RetrieveMultipleAsync(query);
			return result.Entities.ToArray();
		}


		private static string RemoveValueFromCsvValues(string csv, string value)
		{
			var values = csv.Split(',').Where(s => s != value);
			return string.Join(",", values);
		}

		private static string RemoveFieldFromFetchXml(string xml, string attName)
		{
			xml = RemoveXmlNodesWithTagValue(xml, "condition", "attribute", attName);
			xml = RemoveXmlNodesWithTagValue(xml, "attribute", "name", attName);
			xml = RemoveXmlNodesWithTagValue(xml, "cell", "name", attName); // Layout Xml from Views
			xml = RemoveXmlNodesWithTagValue(xml, "order", "attribute", attName);
			return xml;
		}




		private static string RemoveXmlNodesWithTagValue(string? xml, string nodeName, string tagName, string tagValue)
		{
			if (string.IsNullOrWhiteSpace(xml)) return string.Empty;

			var nodeStart = $"<{nodeName} ";
			const string nodeEnd = "/>";
			var nodeFullEnd = $"</{nodeName}>";

			foreach (var node in xml.SubstringAllByString(nodeStart, nodeFullEnd)
						.Where(c => c.Contains($"{tagName}=\"{tagValue}\"", StringComparison.OrdinalIgnoreCase)).ToArray())
			{
				xml = xml.Replace(nodeStart + node + nodeFullEnd, string.Empty);
			}

			foreach (var node in xml.SubstringAllByString(nodeStart, nodeEnd)
									.Where(c => !c.Contains(nodeFullEnd, StringComparison.OrdinalIgnoreCase) && c.Contains($"{tagName}=\"{tagValue}\"")).ToArray())
			{
				xml = xml.Replace(nodeStart + node + nodeEnd, string.Empty);
			}

			return xml;
		}



		private static string RemoveParentXmlNodesWithTagValue(string? xml, string parentNodeName, string nodeName, string tagName, string tagValue, string? parentNodeEndName = null)
		{
			if (string.IsNullOrWhiteSpace(xml)) return string.Empty;

			var parentNodeStart = $"<{parentNodeName} ";
			var parentNodeEnd = $"</{parentNodeEndName ?? parentNodeName}>";
			var nodeStart = $"<{nodeName} ";
			const string nodeEnd = "/>";
			var nodeFullEnd = $"</{nodeName}>";

			foreach (var parentNode in xml.SubstringAllByString(parentNodeStart, parentNodeEnd)
										  .Where(n => n.SubstringAllByString(nodeStart, nodeEnd)
													   .Exists(c => !c.Contains(nodeFullEnd, StringComparison.OrdinalIgnoreCase) && c.Contains($"{tagName}=\"{tagValue}\""))
													  ||
													  n.SubstringAllByString(nodeStart, nodeFullEnd)
													   .Exists(c => !c.Contains(nodeEnd, StringComparison.OrdinalIgnoreCase) && c.Contains($"{tagName}=\"{tagValue}\""))).ToArray())
			{
				xml = xml.Replace(parentNodeStart + parentNode + parentNodeEnd, string.Empty);
			}

			return xml;
		}
	}
}
