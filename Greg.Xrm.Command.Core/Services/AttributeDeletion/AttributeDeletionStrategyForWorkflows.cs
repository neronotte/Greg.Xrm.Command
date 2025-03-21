using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
	public class AttributeDeletionStrategyForWorkflows(
		IOutput output,
		IWorkflowRepository workflowRepository,
		IProcessTriggerRepository processTriggerRepository
	)
		: AttributeDeletionStrategyBase
	{
		public override ComponentType ComponentType => ComponentType.Workflow;


		protected override async Task HandleInternalAsync(
			IOrganizationServiceAsync2 crm,
			AttributeMetadata attribute,
			IReadOnlyList<Dependency> dependencies)
		{
			var workflowList = await workflowRepository.GetByIdsAsync(crm, dependencies.Select(x => x.dependentcomponentobjectid));


			var i = 0;
			foreach (var workflow in workflowList)
			{
				++i;
				output.Write($"Updating workflow {i}/{workflowList.Count} {workflow.Id}...");

				var clone = new Workflow(workflow.Id)
				{
					xaml = RemoveParentXmlNodesWithTagValue(workflow.xaml, "mxswa:ActivityReference AssemblyQualifiedName=\"Microsoft.Crm.Workflow.Activities.StepComposite,", "mcwb:Control", "DataFieldName", attribute.LogicalName, "mxswa:ActivityReference")
				};

				var unsupportedXml = RemoveXmlNodesWithTagValue(workflow.xaml, "mxswa:GetEntityProperty", "Attribute", attribute.LogicalName);
				if (clone.xaml != unsupportedXml)
				{
					output.WriteLine("Failed. ", ConsoleColor.Red)
						.WriteLine("Attribute is used in a Business Rules Get Entity Property. This is unsupported for manual deletion. Delete the Business Rule ", ConsoleColor.Red)
						.Write(workflow.name, ConsoleColor.Red)
						.Write(" manually to be able to delete the attribute.", ConsoleColor.Red)
						.WriteLine();

					continue;
				}

				var activate = workflow.statecode?.Value == (int)Workflow.State.Activated;
				if (activate)
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
					var triggers = await processTriggerRepository.GetByWorkflowIdAsync(crm, workflow.Id);
					foreach (var trigger in triggers)
					{
						await crm.DeleteAsync(trigger.EntityName, trigger.Id);
					}

					if (workflow.triggeronupdateattributelist != null)
					{
						var newValue = RemoveValueFromCsvValues(workflow.triggeronupdateattributelist, attribute.LogicalName);
						clone.triggeronupdateattributelist = newValue;
					}

					await clone.SaveOrUpdateAsync(crm);
				}
				finally
				{
					if (activate)
					{
						await crm.ExecuteAsync(new SetStateRequest
						{
							EntityMoniker = workflow.ToEntityReference(),
							State = new OptionSetValue((int)Workflow.State.Activated),
							Status = new OptionSetValue((int)Workflow.Status.Activated)
						});
					}
				}

				output.WriteLine("Done", ConsoleColor.Green);
			}
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
