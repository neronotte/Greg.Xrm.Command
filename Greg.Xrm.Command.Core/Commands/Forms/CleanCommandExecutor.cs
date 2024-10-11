using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Greg.Xrm.Command.Commands.Forms
{
    public class CleanCommandExecutor
	(
			IOrganizationServiceRepository organizationServiceRepository,
			IOutput output,
			IFormRepository formRepository,
			ISolutionRepository solutionRepository) : ICommandExecutor<CleanCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CleanCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("DONE", ConsoleColor.Green);





			output.Write($"Retrieving main form of table {command.TableName}...");
			var formList = await formRepository.GetMainFormByTableNameAsync(crm, command.TableName);
			output.WriteLine("DONE", ConsoleColor.Green);

			if (!TryGetForm(command.TableName, command.FormName, formList, out var form, out var result))
			{
				return result ?? CommandResult.Fail("Error retrieving the form to update");
			}

			if (form == null || string.IsNullOrWhiteSpace(form.formxml))
			{
				return CommandResult.Fail("No formxml found!");
			}



			// I need to retrieve the default language code and table metadata because they are required to manipulate the form
			// (need to put the proper labels in the correct language)

			output.Write("Retrieving default language code...");
			var languageCode = await crm.GetDefaultLanguageCodeAsync();
			output.WriteLine("DONE, default language code is " + languageCode, ConsoleColor.Green);

			var table = await this.RetrieveEntityMetadataAsync(crm, command.TableName);



			var (success, result1, solution) = await CreateHoldingSolutionAsync(crm, command.SolutionName);
			if (!success) return result1 ?? CommandResult.Fail("Error creating the holding solution");
			if (solution == null) return CommandResult.Fail("Error creating the holding solution");

			using(solution)
			{
				try
				{
					// add the form to the temporary solution
					await solution.AddComponentAsync(form.Id, ComponentType.SystemForm);

					// now I need to download the solution, extract the customizations.xml file, modify it, and re-upload it
					var solutionBytes = await solution.DownloadAsync();


					if (!string.IsNullOrWhiteSpace(command.TempDir) && Directory.Exists(command.TempDir))
					{
						var fileName = Path.Combine(command.TempDir, $"{solution}_original.zip");
						await File.WriteAllBytesAsync(fileName, solutionBytes, cancellationToken);
					}

					var hasChanges = false;
					using (var archiveStream = new MemoryStream())
					{
						await archiveStream.WriteAsync(solutionBytes, 0, solutionBytes.Length, cancellationToken);


						using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, true))
						{
							var entry = archive.GetEntry("customizations.xml");
							if (entry == null)
							{
								return CommandResult.Fail("The temporary solution does not contains any <customizations.xml>");
							}

							XDocument doc;
							using (var entryStream = entry.Open())
							{
								doc = XDocument.Load(entryStream);

								var element = doc.XPathSelectElement("./ImportExportXml/Entities/Entity/FormXml/forms/systemform/form");

								hasChanges = SetTabNames(element) || hasChanges;
								hasChanges = SetSectionNames(element) || hasChanges;
								hasChanges = RemoveOwnerFromFirstTab(element) || hasChanges;
								hasChanges = CreateAdminTab(element, table, languageCode) || hasChanges;
							}
							entry.Delete();

							entry = archive.CreateEntry("customizations.xml");
							using (var entryStream = entry.Open())
							{
								doc.Save(entryStream);
							}
						}

						if (!string.IsNullOrWhiteSpace(command.TempDir) && Directory.Exists(command.TempDir))
						{
							var fileName = Path.Combine(command.TempDir, $"{solution}_updated.zip");
							using (var fileStream = new FileStream(fileName, FileMode.Create))
							{
								archiveStream.Seek(0, SeekOrigin.Begin);
								await archiveStream.CopyToAsync(fileStream, cancellationToken);
							}
						}

						

						archiveStream.Seek(0, SeekOrigin.Begin);
						var newZipBytes = archiveStream.ToArray();

						if (hasChanges)
						{
							await solution.UploadAndPublishAsync(newZipBytes, command.TableName);
						}
						else
						{
							output.WriteLine("No changes detected. The form is already up to date.", ConsoleColor.DarkGray);
						}
					}
				}
				catch (Exception ex)
				{
					output.WriteLine($"ERROR: {ex.Message}", ConsoleColor.Red);
				}
			}
			

			return CommandResult.Success();
		}



		private async Task<(bool, CommandResult?, ITemporarySolution?)> CreateHoldingSolutionAsync(IOrganizationServiceAsync2 crm, string? currentSolutionName)
		{
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return (false, CommandResult.Fail("No solution name provided and no current solution name found in the settings."), null);
				}
			}

			output.Write($"Creating temporary holding solution...");
			var currentSolution = await solutionRepository.GetByUniqueNameAsync(crm, currentSolutionName);
			if (currentSolution == null)
			{
				return (false, CommandResult.Fail($"Solution {currentSolutionName} not found"), null);
			}

			var solution = await solutionRepository.CreateTemporarySolutionAsync(crm, currentSolution.publisherid);
			output.WriteLine("DONE", ConsoleColor.Green);


			return (true, null, solution);
		}





		private bool TryGetForm(string tableName, string formName, List<Form> formList, out Form? form, out CommandResult? result)
		{
			form = null;
			result = null;

			if (formList.Count == 0)
			{
				result = CommandResult.Fail($"No main form found for table {tableName}");
				return false;
			}

			if (formList.Count == 1)
			{
				form = formList[0];
				output.WriteLine($"Main form found: {form.name}");
				return true;
			}


			// if we are here, we have more tan 1 form
			
			if (string.IsNullOrWhiteSpace(formName))
			{
				result = CommandResult.Fail($"Table <{tableName}> has more than one main form. Please specify the form name using the --form parameter.");
				return false;
			}

			formList = formList.Where(f => f.name.Equals(formName, StringComparison.OrdinalIgnoreCase)).ToList();
			if (formList.Count == 0)
			{
				result = CommandResult.Fail($"Main form <{formName}> not found for table <{tableName}>");
				return false;
			}

			if (formList.Count == 1)
			{
				form = formList[0];
				output.WriteLine($"Main form found: {form.name}");
				return true;
			}

			result = CommandResult.Fail($"Table <{tableName}> has more than one main form called <{formName}>. Please change the name of the form to uniquely identify it.");
			return false;
		}


		private async Task<EntityMetadata> RetrieveEntityMetadataAsync(IOrganizationServiceAsync2 crm, string tableName)
		{
			output.Write($"Retrieving table <{tableName}> metadata...");

			var request = new RetrieveEntityRequest
			{
				EntityFilters = EntityFilters.Attributes,
				LogicalName = tableName
			};

			var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
			output.WriteLine("DONE", ConsoleColor.Green);

			return response.EntityMetadata;
		}


		private bool SetTabNames(XElement? doc)
		{
			if (doc == null)
				throw new ArgumentNullException(nameof(doc), "The formxml document is null");

			output.Write($"Setting names on unnamed tabs...");

			var tabs = doc.XPathSelectElements("./tabs/tab")?.ToArray() ?? [];
			if (tabs.Length == 0)
			{
				output.WriteLine("ERROR. No tabs found in the formxml", ConsoleColor.Red);
				return false;
			}

			// Reading the current tab names to avoid duplicates
			var currentTabNames = tabs
				.Select(t => t.Attribute("name")?.Value)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct()
				.Select(x => x?.ToLowerInvariant())
				.ToList();

			var isFirstComment = true;

			var i = 0; 
			var updatedTabCount = 0;
			foreach (var tab in tabs)
			{
				i++;
				if (!string.IsNullOrWhiteSpace(tab.Attribute("name")?.Value))
				{
					if (isFirstComment) {
						output.WriteLine();
						isFirstComment = false;
					}
					output.WriteLine($"    Tab {i} has already a name, moving to the next one.", ConsoleColor.DarkGray);
					continue;
				}

				var tabName = tab.Element("labels")?.Elements("label")?.FirstOrDefault()?.Attribute("description")?.Value ?? string.Empty;
				if (string.IsNullOrWhiteSpace(tabName))
				{
					if (isFirstComment)
					{
						output.WriteLine();
						isFirstComment = false;
					}
					output.WriteLine($"    Tab {i} has no name, but also no label. Don't know which name to set. Moving to the next one.", ConsoleColor.Yellow);
					continue;
				}

				tabName = "tab_" + tabName.OnlyLettersNumbersOrUnderscore();

				if (currentTabNames.Contains(tabName))
				{
					if (isFirstComment)
					{
						output.WriteLine();
						isFirstComment = false;
					}
					output.WriteLine($"    Tab {i} has a name that would create a duplicate. Moving to the next one.", ConsoleColor.Yellow);
					continue;
				}

				tab.SetAttributeValue("name", tabName);
				currentTabNames.Add(tabName);
				updatedTabCount++;
			}
			output.WriteLine("DONE, updated tabs: " + updatedTabCount, ConsoleColor.Green);
			return updatedTabCount > 0;
		}






		private bool SetSectionNames(XElement? doc)
		{
			if (doc == null)
				throw new ArgumentNullException(nameof(doc), "The formxml document is null");

			output.Write($"Setting names on unnamed sections...");

			var sections = doc.XPathSelectElements("./tabs/tab/columns/column/sections/section")?.ToArray() ?? new XElement[0];
			if (sections.Length == 0)
			{
				output.WriteLine("ERROR. No section found in the formxml", ConsoleColor.Red);
				return false;
			}


			// Reading the current tab names to avoid duplicates
			var currentSectionNames = sections
				.Select(t => t.Attribute("name")?.Value)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct()
				.Select(x => x.ToLowerInvariant())
				.ToList();

			var isFirstComment = true;

			var i = 0;
			var updatedSectionCount = 0;
			foreach (var section in sections)
			{
				i++;
				if (!string.IsNullOrWhiteSpace(section.Attribute("name")?.Value))
				{
					if (isFirstComment)
					{
						output.WriteLine();
						isFirstComment = false;
					}
					output.WriteLine($"    Section {i} has already a name, moving to the next one.", ConsoleColor.DarkGray);
					continue;
				}

				var sectionName = section.Element("labels")?.Elements("label")?.FirstOrDefault()?.Attribute("description")?.Value ?? string.Empty;
				if (string.IsNullOrWhiteSpace(sectionName))
				{
					if (isFirstComment)
					{
						output.WriteLine();
						isFirstComment = false;
					}
					output.WriteLine($"    Section {i} has no name, but also no label. Don't know which name to set. Moving to the next one.", ConsoleColor.Yellow);
					continue;
				}

				var tabName = section.Parent?.Parent?.Parent?.Parent?.Attribute("name")?.Value ?? string.Empty;
				sectionName = tabName + "_sec_" + sectionName.OnlyLettersNumbersOrUnderscore();
				if (currentSectionNames.Contains(sectionName))
				{
					if (isFirstComment)
					{
						output.WriteLine();
						isFirstComment = false;
					}
					output.WriteLine($"    Tab {i} has a name that would create a duplicate. Moving to the next one.", ConsoleColor.Yellow);
					continue;
				}

				section.SetAttributeValue("name", sectionName);
				currentSectionNames.Add(tabName);
				updatedSectionCount++;
			}
			output.WriteLine("DONE, updated sections: " + updatedSectionCount, ConsoleColor.Green);
			return updatedSectionCount > 0;
		}





		private bool RemoveOwnerFromFirstTab(XElement? doc)
		{
			if (doc == null)
				throw new ArgumentNullException(nameof(doc), "The formxml document is null");

			output.Write($"Removing owner from first tab...");

			var tabs = doc.XPathSelectElements("./tabs/tab")?.ToArray() ?? [];
			if (tabs.Length == 0)
			{
				output.WriteLine("ERROR. No tabs found in the formxml", ConsoleColor.Red);
				return false;
			}

			var tab = tabs[0];

			var ownerControlNode = tab.XPathSelectElement("./columns/column/sections/section/rows/row/cell/control[@id=\"ownerid\"]");
			if (ownerControlNode == null)
			{
				output.WriteLine("NO ACTION. The owner field is not present in the first tab", ConsoleColor.DarkGray);
				return false;
			}

			var rowNode = ownerControlNode.Parent?.Parent; // here we are on the row node
			rowNode?.Remove();

			output.WriteLine("DONE", ConsoleColor.Green);
			return true;
		}






		private static XElement[] CreateXmlLabelFromAttribute(AttributeMetadata[]? attributeList, string logicalName, int languageCode)
		{
			if (attributeList == null)
			{
				return [ new XElement("label",
					new XAttribute("description",logicalName),
					new XAttribute("languagecode", languageCode)
				) ];
			}

			var attribute = Array.Find(attributeList, a => a.LogicalName == logicalName);
			if (attribute == null)
			{
				return [ new XElement("label",
					new XAttribute("description",logicalName),
					new XAttribute("languagecode", languageCode)
				) ];
			}

			var labels = attribute.DisplayName.LocalizedLabels.Select(x => new XElement("label",
				new XAttribute("description", x.Label),
				new XAttribute("languagecode", x.LanguageCode)
			)).ToArray();

			return labels;
		}

		private bool CreateAdminTab(XElement? doc, EntityMetadata table, int languageCode)
		{
			if (doc == null)
				throw new ArgumentNullException(nameof(doc), "The formxml document is null");


			var createdByLabels = CreateXmlLabelFromAttribute(table.Attributes, "createdby", languageCode);
			var createdOnLabels = CreateXmlLabelFromAttribute(table.Attributes, "createdon", languageCode);
			var modifiedByLabels = CreateXmlLabelFromAttribute(table.Attributes, "modifiedby", languageCode);
			var modifiedOnLabels = CreateXmlLabelFromAttribute(table.Attributes, "modifiedon", languageCode);
			var ownerLabels = CreateXmlLabelFromAttribute(table.Attributes, "ownerid", languageCode);

			// if there is already a tab called tab_admin, I don't need to add it again

			output.Write($"Adding the administration tab...");


			var isTabAdminPresent = doc.XPathSelectElements("./tabs/tab")?
				.Any(t => string.Equals( t.Attribute("name")?.Value, "tab_admin", StringComparison.OrdinalIgnoreCase)) ?? false;
			if (isTabAdminPresent)
			{
				output.WriteLine("NO ACTION. Tab tab_admin already in the form.", ConsoleColor.DarkGray);
				return false;
			}


			var adminTab = new XElement("tab",
				new XAttribute("name", "tab_admin"),
				new XAttribute("id", Guid.NewGuid().ToString("D")), // here it requires a GUID as 00000000-0000-0000-0000-000000000000
				new XAttribute("IsUserDefined", "0"),
				new XAttribute("locklevel", "0"),
				new XAttribute("showlabel", "true"),
				new XElement("labels",
					new XElement("label",
						new XAttribute("description", "Administration"),
						new XAttribute("languagecode", languageCode)
					)
				),
				new XElement("columns",
					new XElement("column",
						new XAttribute("width", "100%"),
						new XElement("sections",
							new XElement("section",
								new XAttribute("name", "tab_admin_sec_admin"),
								new XAttribute("id", Guid.NewGuid().ToString("D")), // here it requires a GUID as 00000000-0000-0000-0000-000000000000
								new XAttribute("IsUserDefined", "0"),
								new XAttribute("locklevel", "0"),
								new XAttribute("showlabel", "false"),
								new XAttribute("showbar", "false"),
								new XAttribute("layout", "varwidth"),
								new XAttribute("celllabelalignment", "Left"),
								new XAttribute("celllabelposition", "Left"),
								new XAttribute("columns", "11"),
								new XAttribute("labelwidth", "115"),
								new XElement("labels",
									new XElement("label",
										new XAttribute("description", "Administration"),
										new XAttribute("languagecode", languageCode) // english
									)
								),
								new XElement("rows",
									new XElement("row",
										new XElement("cell",
											new XAttribute("id", Guid.NewGuid().ToString("B")), // here it requires a GUID as {00000000-0000-0000-0000-000000000000}
											new XAttribute("locklevel", "0"),
											new XAttribute("colspan", "1"),
											new XAttribute("rowspan", "1"),
											new XElement("labels",
												createdByLabels
											),
											new XElement("control",
												new XAttribute("id", "createdby"),
												new XAttribute("classid", "{270BD3DB-D9AF-4782-9025-509E298DEC0A}"), // fixed guid
												new XAttribute("datafieldname", "createdby"),
												new XAttribute("disabled", "false")
											)
										),
										new XElement("cell",
											new XAttribute("id", Guid.NewGuid().ToString("B")), // here it requires a GUID as {00000000-0000-0000-0000-000000000000}
											new XAttribute("locklevel", "0"),
											new XAttribute("colspan", "1"),
											new XAttribute("rowspan", "1"),
											new XElement("labels",
												createdOnLabels
											),
											new XElement("control",
												new XAttribute("id", "createdon"),
												new XAttribute("classid", "{5B773807-9FB2-42DB-97C3-7A91EFF8ADFF}"), // fixed guid
												new XAttribute("datafieldname", "createdon"),
												new XAttribute("disabled", "false")
											)
										)
									),
									new XElement("row",
										new XElement("cell",
											new XAttribute("id", Guid.NewGuid().ToString("B")), // here it requires a GUID as {00000000-0000-0000-0000-000000000000}
											new XAttribute("locklevel", "0"),
											new XAttribute("colspan", "1"),
											new XAttribute("rowspan", "1"),
											new XElement("labels",
												modifiedByLabels
											),
											new XElement("control",
												new XAttribute("id", "modifiedby"),
												new XAttribute("classid", "{270BD3DB-D9AF-4782-9025-509E298DEC0A}"), // fixed guid
												new XAttribute("datafieldname", "modifiedby"),
												new XAttribute("disabled", "false")
											)
										),
										new XElement("cell",
											new XAttribute("id", Guid.NewGuid().ToString("B")), // here it requires a GUID as {00000000-0000-0000-0000-000000000000}
											new XAttribute("locklevel", "0"),
											new XAttribute("colspan", "1"),
											new XAttribute("rowspan", "1"),
											new XElement("labels",
												modifiedOnLabels
											),
											new XElement("control",
												new XAttribute("id", "modifiedon"),
												new XAttribute("classid", "{5B773807-9FB2-42DB-97C3-7A91EFF8ADFF}"), // fixed guid
												new XAttribute("datafieldname", "modifiedon"),
												new XAttribute("disabled", "false")
											)
										)
									),
									new XElement("row",
										new XElement("cell",
											new XAttribute("id", Guid.NewGuid().ToString("B")), // here it requires a GUID as {00000000-0000-0000-0000-000000000000}
											new XAttribute("locklevel", "0"),
											new XAttribute("colspan", "1"),
											new XAttribute("rowspan", "1"),
											new XElement("labels",
												ownerLabels
											),
											new XElement("control",
												new XAttribute("id", "ownerid"),
												new XAttribute("classid", "{270BD3DB-D9AF-4782-9025-509E298DEC0A}"), // fixed guid
												new XAttribute("datafieldname", "ownerid")
											)
										),
										new XElement("cell",
											new XAttribute("id", Guid.NewGuid().ToString("B")), // here it requires a GUID as {00000000-0000-0000-0000-000000000000}
											new XAttribute("locklevel", "0"),
											new XElement("labels",
												new XElement("label",
													new XAttribute("description", ""),
													new XAttribute("languagecode", languageCode) // english
												)
											)
										)
									)
								)
							)
						)
					)
				)
			);


			var tabs = doc.XPathSelectElements("./tabs")?.FirstOrDefault();
			if (tabs == null)
			{
				output.WriteLine("ERROR. No tabs node found in the formxml", ConsoleColor.Red);
				return false;
			}

			tabs.Add(adminTab);
			output.WriteLine("DONE", ConsoleColor.Green);
			return true;
		}
	}
}
