using Greg.Xrm.Command.Commands.WebResources.ApplyIconsRules;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace Greg.Xrm.Command.Commands.WebResources
{
    public class ApplyIconsCommandExecutor : ICommandExecutor<ApplyIconsCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISolutionRepository solutionRepository;
		private readonly IWebResourceRepository webResourceRepository;
		private readonly IIconFinder iconFinder;

		public ApplyIconsCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			IWebResourceRepository webResourceRepository,
			IIconFinder iconFinder)
        {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
			this.solutionRepository = solutionRepository ?? throw new ArgumentNullException(nameof(solutionRepository));
			this.webResourceRepository = webResourceRepository;
			this.iconFinder = iconFinder;
		}


        public async Task<CommandResult> ExecuteAsync(ApplyIconsCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("DONE", ConsoleColor.Green);

			var tableSolutionName = command.TableSolutionName;
			if (string.IsNullOrWhiteSpace(tableSolutionName))
			{
				tableSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (string.IsNullOrWhiteSpace(tableSolutionName))
				{
					return CommandResult.Fail("No table solution name provided and no current solution name found in the settings.");
				}
			}

			var webResourceSolutionName = command.WebResourceSolutionName;
			if (string.IsNullOrWhiteSpace(webResourceSolutionName))
			{
				webResourceSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (string.IsNullOrWhiteSpace(webResourceSolutionName))
				{
					return CommandResult.Fail("No webresource solution name provided and no current solution name found in the settings.");
				}
			}


			var tableSolution = await this.solutionRepository.GetByUniqueNameAsync(crm, tableSolutionName);
			Model.Solution? webResourceSolution;
			if (string.Equals(tableSolutionName, webResourceSolutionName, StringComparison.OrdinalIgnoreCase))
			{
				webResourceSolution = tableSolution;
			}
			else
			{
				webResourceSolution = await this.solutionRepository.GetByUniqueNameAsync(crm, webResourceSolutionName);
			}
			if (tableSolution == null)
			{
				return CommandResult.Fail($"The table solution <{tableSolutionName}> is not present in the current environment.");
			}
			if (webResourceSolution == null)
			{
				return CommandResult.Fail($"The webresource solution <{webResourceSolutionName}> is not present in the current environment.");
			}

			var tables = await GetTableNamesBySolution(crm, tableSolution);
			if (tables.Count == 0)
			{
				return CommandResult.Fail($"No tables to update found in solution <{tableSolutionName}>.");
			}


			var images = await GetImagesBySolution(crm, webResourceSolution);
			if (images.Count == 0)
			{
				return CommandResult.Fail($"No images to use found in solution <{webResourceSolutionName}>.");
			}

			var xml = new StringBuilder();
			var settings = new XmlWriterSettings
			{
				Indent = true
			};
			var writer = XmlWriter.Create(xml, settings);
			writer.WriteStartDocument();
			writer.WriteStartElement("importexportxml");
			writer.WriteStartElement("entities");

			var skippedCount = 0;
			var successCount = 0;
			var failedCount = 0;

			foreach (var table in tables)
			{
				var icon = this.iconFinder.Find(images, table.LogicalName, webResourceSolution.PublisherCustomizationPrefix ?? string.Empty);

				if (icon == null)
				{
					this.output.WriteLine($"No icon found for table <{table.LogicalName}>", ConsoleColor.Yellow);
					skippedCount++;
					continue;
				}

				try
				{
					if (command.NoAction)
					{
						this.output.WriteLine($"Table <{table.LogicalName}> will be updated with icon <{icon}>");
						successCount++;
						continue;
					}


					this.output.Write($"Updating table <{table.LogicalName}> with icon <{icon}>...");
					table.IconVectorName = icon;
					var request = new UpdateEntityRequest
					{
						Entity = table,
					};

					await crm.ExecuteAsync(request);
					this.output.WriteLine("DONE", ConsoleColor.Green);

					writer.WriteElementString("entity", table.LogicalName);
					successCount++;
				}
				catch(FaultException<OrganizationServiceFault> ex)
				{
					this.output.WriteLine("ERROR", ConsoleColor.Red);
					this.output.WriteLine(ex.Message, ConsoleColor.Red);
					failedCount++;
				}
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.WriteEndDocument();
			writer.Flush();
			writer.Dispose();

			if (command.NoAction)
			{
				if (successCount == 0)
				{
					this.output.WriteLine("No tables to update, publish won't be executed.");
				}
				else
				{
					this.output.WriteLine("Publish will be performed with the following XML:");
					this.output.WriteLine(xml.ToString());
				}

				var result = CommandResult.Success();
				result["Tables without icon"] = skippedCount;
				result["Tables to update"] = successCount;
				result["Publish succeeded"] = false;
				return result;
			}


			var publishSucceeded = false;
			if (successCount > 0)
			{
				publishSucceeded = await PublishAllCustomizationsAsync(crm, xml);
			}


			if (publishSucceeded)
			{
				var result = CommandResult.Success();
				result["Tables without icon"] = skippedCount;
				result["Tables updated"] = successCount;
				result["Tables with errors"] = failedCount;
				result["Publish succeeded"] = publishSucceeded;
				return result;
			}

			if (successCount > 0)
			{
				var result = CommandResult.Fail("Publish failed. Tables updated, but not published.");
				result["Tables without icon"] = skippedCount;
				result["Tables updated"] = successCount;
				result["Tables with errors"] = failedCount;
				result["Publish succeeded"] = publishSucceeded;
				return result;
			}

			var result1 = CommandResult.Fail("No tables updated");
			result1["Tables without icon"] = skippedCount;
			result1["Tables updated"] = successCount;
			result1["Tables with errors"] = failedCount;
			result1["Publish succeeded"] = publishSucceeded;
			return result1;
		}





		private async Task<bool> PublishAllCustomizationsAsync(IOrganizationServiceAsync2 crm, StringBuilder xml)
		{
			try
			{
				this.output.WriteLine("Publishing all customizations...");

				var request = new PublishXmlRequest
				{
					ParameterXml = xml.ToString(),
				};

				await crm.ExecuteAsync(request);

				this.output.WriteLine("DONE", ConsoleColor.Green);
				return true;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				this.output.WriteLine(ex.Message, ConsoleColor.Red);
				return false;
			}
		}

		private static bool MatchName(string name, string toFind)
		{
			if(name.Contains('/'))
			{
				var imageName = name.Split('/').LastOrDefault();
				return string.Equals(imageName, toFind + ".svg", StringComparison.OrdinalIgnoreCase);
			}

			return string.Equals(name, toFind + ".svg", StringComparison.OrdinalIgnoreCase);
		}

		private async Task<IReadOnlyList<EntityMetadata>> GetTableNamesBySolution(IOrganizationServiceAsync2 crm, Model.Solution solution)
		{
			this.output.Write($"Retrieving custom tables from solution <{solution.uniquename}>...");

			try
			{
				var query = new QueryExpression("solutioncomponent");
				query.ColumnSet.AddColumns("objectid", "componenttype");
				query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solution.Id);
				query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, (int)ComponentType.Entity);


				var result = await crm.RetrieveMultipleAsync(query);

				var tableMetadataIds = result.Entities.Select(e => e.GetAttributeValue<Guid>("objectid")).ToArray();


				var query2 = new EntityQueryExpression();
				query2.Criteria = new MetadataFilterExpression(LogicalOperator.And);
				query2.Criteria.Conditions.Add(new MetadataConditionExpression("MetadataId", MetadataConditionOperator.In, tableMetadataIds));
				query2.Criteria.Conditions.Add(new MetadataConditionExpression("IsCustomEntity", MetadataConditionOperator.Equals, true));


				var response = (RetrieveMetadataChangesResponse)await crm.ExecuteAsync(new RetrieveMetadataChangesRequest { Query = query2 });

				var tables = response.EntityMetadata
					.OfType<EntityMetadata>()
					.Where(e => string.IsNullOrWhiteSpace(e.IconVectorName))
					.Select(e => e)
					.ToList();


				this.output.WriteLine("DONE", ConsoleColor.Green);
				this.output.WriteLine($"Found {tables.Count} custom tables without icons.", ConsoleColor.Yellow);

				return tables;
			}
			catch(Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				this.output.WriteLine(ex.Message, ConsoleColor.Red);
				return Array.Empty<EntityMetadata>();
			}
		}

		private async Task<IReadOnlyCollection<string>> GetImagesBySolution(IOrganizationServiceAsync2 crm, Model.Solution solution)
		{
			try
			{
				var webResources = await this.webResourceRepository.GetBySolutionAsync(crm, solution.uniquename);


				this.output.Write($"Filtering SVG web resources...");

				var images = webResources.Where(x => x.webresourcetype.Value == (int)WebResourceType.ImageSvg)
					.Select(x => x.name)
					.ToList();


				this.output.WriteLine("DONE", ConsoleColor.Green);
				this.output.WriteLine($"Found {images.Count} SVG web resources.", ConsoleColor.Yellow);

				return images;
			}
			catch (Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				this.output.WriteLine(ex.Message, ConsoleColor.Red);
				return Array.Empty<string>();
			}
		}
	}
}
