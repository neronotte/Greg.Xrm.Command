using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using System.Xml.Linq;

namespace Greg.Xrm.Command.Commands.Views
{
    public class AddPowerAppGridControlCommandExecutor(
		IOutput output, 
		IOrganizationServiceRepository organizationServiceRepository,
		IViewRetrieverService viewRetriever,
		IPublishXmlBuilder publishXmlBuilder) : ICommandExecutor<AddPowerAppGridControlCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AddPowerAppGridControlCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			var (result, view) = await viewRetriever.GetByNameAsync(crm, command.QueryType, command.ViewName, command.TableName);
			if (view == null) return result;

			if (view.layoutxml == null)
			{
				return CommandResult.Fail("No layoutxml found for the view");
			}

			XDocument doc;
			XElement? controlDescriptions = null;
			try
			{
				doc = XDocument.Parse(view.layoutxml);

				controlDescriptions = doc.Document?.Root?.Element("controlDescriptions");
				if (controlDescriptions != null)
				{
					if (!command.Force)
					{
						return CommandResult.Fail("A custom control already exists on the view. Use the --force argument to update it.");
					}

					output.WriteLine("A custom control already exists for the view. We'll remove it to apply the required changes", ConsoleColor.Yellow);
					controlDescriptions.Remove();
				}
			}
			catch(Exception ex)
			{
				return CommandResult.Fail("Failed to parse the view layoutxml", ex);
			}

			var staticAttribute = new XAttribute("static", "true");


			var parameters = new List<XElement>();

			parameters.Add(new XElement("data-set",
				new XAttribute("name", "Items"),
				new XElement("ViewId", Guid.Empty.ToString("D"))
			));
			parameters.Add(new XElement("AccessibleLabel",
				staticAttribute,
				new XAttribute("type", "SingleLine.Text"),
				command.AccessibleLabel
			));
			parameters.Add(Boolean("EnableEditing", command.EnableEditing));
			parameters.Add(Boolean("DisableChildItemsEditing", command.DisableChildItemsEditing));
			parameters.Add(Boolean("EnableFiltering", command.EnableFiltering));
			parameters.Add(Boolean("EnableSorting", command.EnableSorting));
			parameters.Add(Boolean("EnableGrouping", command.EnableGrouping));
			parameters.Add(Boolean("EnableAggregation", command.EnableAggregation));
			parameters.Add(Boolean("EnableColumnMoving", command.EnableColumnMoving));
			parameters.Add(Boolean("EnableMultipleSelection", command.EnableMultipleSelection));
			parameters.Add(Boolean("EnableRangeSelection", command.EnableRangeSelection));
			parameters.Add(Boolean("EnableJumpBar", command.EnableJumpBar));
			parameters.Add(Boolean("EnablePagination", command.EnablePagination));
			parameters.Add(Boolean("EnableDropdownColor", command.EnableDropdownColor));
			parameters.Add(Boolean("EnableStatusIcons", command.EnableStatusIcons));
			parameters.Add(Boolean("EnableTypeIcons", command.EnableTypeIcons));
			parameters.Add(new XElement("NavigationTypesAllowed",
				staticAttribute,
				new XAttribute("type", "Enum"),
				command.NavigationTypesAllowed.ToString().ToLowerInvariant().Replace("only", "Only")
			));
			parameters.Add(new XElement("ReflowBehavior",
				staticAttribute,
				new XAttribute("type", "Enum"),
				command.ReflowBehavior.ToString()
			));
			parameters.Add(Boolean("ShowAvatar", command.ShowAvatar));
			parameters.Add(new XElement("NumberOfListColumns",
				staticAttribute,
				new XAttribute("type", "Whole.None"),
				command.NumberOfListColumns.ToString()
			));
			parameters.Add(Boolean("ContextualLookupColumnFilters", command.ContextualLookupColumnFilters));
			parameters.Add(Boolean("LookupFilterBeginsWith", command.LookupFilterBeginsWith));
			parameters.Add(Boolean("UseFirstColumnForLookupEdits", command.UseFirstColumnForLookupEdits));
			parameters.Add(new XElement("GridCustomizerControlFullName",
				staticAttribute,
				new XAttribute("type", "SingleLine.Text"),
				command.GridCustomizerControlFullName
			));
			parameters.Add(Boolean("EnableStatusColumn", true));

			controlDescriptions = new XElement("controlDescriptions",
				new XElement("controlDescription",
					new XElement("customControl",
						new XAttribute("id", "{E7A81278-8635-4D9E-8D4D-59480B391C5B}"), //magic string: need to check if it is the same in all environments
						new XElement("parameters")
					),
					Enumerable.Range(0, 3).Select(i => new XElement("customControl",
						new XAttribute("formFactor", i.ToString()),
						new XAttribute("name", "Microsoft.PowerApps.PowerAppsOneGrid"),
						new XElement("parameters",
							parameters
						)
					))
				)
			);

			doc.Document?.Root?.Add(controlDescriptions);

			output.WriteLine();
			output.WriteLine("--- New Layout XML ---");
			output.WriteLine(doc.ToString());
			output.WriteLine("----------------------");
			output.WriteLine();

			try
			{
				output.Write("Saving the updated view layout...");
				view.layoutxml = doc.ToString(SaveOptions.DisableFormatting);
				await view.SaveOrUpdateAsync(crm);

				publishXmlBuilder.AddTable(view.returnedtypecode);

				var request = publishXmlBuilder.Build();
				await crm.ExecuteAsync(request);

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Failed to save the updated view layout: " + ex.Message, ex);
			}


			return CommandResult.Success();
		}


		private static XElement Boolean(string name, bool value)
		{
			return new XElement(name,
				new XAttribute("static", "true"),
				new XAttribute("type", "Enum"),
				value ? "yes" : "no"
			);
		}

//		const string Template = @"
//< controlDescriptions>
//	<controlDescription>
//		<customControl id=""{E7A81278-8635-4D9E-8D4D-59480B391C5B}"">
//			<parameters/>
//		</customControl>
//		<customControl formFactor=""0"" name=""Microsoft.PowerApps.PowerAppsOneGrid"">
//			<parameters>
//				<data-set name=""Items"">
//					<ViewId>00000000-0000-0000-0000-000000000000</ViewId>
//				</data-set>
//				<AccessibleLabel static=""true"" type=""SingleLine.Text""/>
//				<EnableEditing static=""true"" type=""Enum"">no</EnableEditing>
//				<DisableChildItemsEditing static=""true"" type=""Enum"">no</DisableChildItemsEditing>
//				<EnableFiltering static=""true"" type=""Enum"">yes</EnableFiltering>
//				<EnableSorting static=""true"" type=""Enum"">yes</EnableSorting>
//				<EnableGrouping static=""true"" type=""Enum"">no</EnableGrouping>
//				<EnableAggregation static=""true"" type=""Enum"">no</EnableAggregation>
//				<EnableColumnMoving static=""true"" type=""Enum"">no</EnableColumnMoving>
//				<EnableMultipleSelection static=""true"" type=""Enum"">yes</EnableMultipleSelection>
//				<EnableRangeSelection static=""true"" type=""Enum"">yes</EnableRangeSelection>
//				<EnableJumpBar static=""true"" type=""Enum"">no</EnableJumpBar>
//				<EnablePagination static=""true"" type=""Enum"">yes</EnablePagination>
//				<EnableDropdownColor static=""true"" type=""Enum"">yes</EnableDropdownColor>
//				<EnableStatusIcons static=""true"" type=""Enum"">yes</EnableStatusIcons>
//				<EnableTypeIcons static=""true"" type=""Enum"">no</EnableTypeIcons>
//				<NavigationTypesAllowed static=""true"" type=""Enum"">all</NavigationTypesAllowed>
//				<ReflowBehavior static=""true"" type=""Enum"">Reflow</ReflowBehavior>
//				<ShowAvatar static=""true"" type=""Enum"">yes</ShowAvatar>
//				<NumberOfListColumns static=""true"" type=""Whole.None"">3</NumberOfListColumns>
//				<ContextualLookupColumnFilters static=""true"" type=""Enum"">yes</ContextualLookupColumnFilters>
//				<LookupFilterBeginsWith static=""true"" type=""Enum"">no</LookupFilterBeginsWith>
//				<UseFirstColumnForLookupEdits static=""true"" type=""Enum"">no</UseFirstColumnForLookupEdits>
//				<GridCustomizerControlFullName static=""true"" type=""SingleLine.Text""/>
//				<EnableStatusColumn static=""true"" type=""Enum"">yes</EnableStatusColumn>
//			</parameters>
//		</customControl>
//		<customControl formFactor=""1"" name=""Microsoft.PowerApps.PowerAppsOneGrid"">
//			<parameters>
//				<data-set name=""Items"">
//					<ViewId>00000000-0000-0000-0000-000000000000</ViewId>
//				</data-set>
//				<AccessibleLabel static=""true"" type=""SingleLine.Text""/>
//				<EnableEditing static=""true"" type=""Enum"">no</EnableEditing>
//				<DisableChildItemsEditing static=""true"" type=""Enum"">no</DisableChildItemsEditing>
//				<EnableFiltering static=""true"" type=""Enum"">yes</EnableFiltering>
//				<EnableSorting static=""true"" type=""Enum"">yes</EnableSorting>
//				<EnableGrouping static=""true"" type=""Enum"">no</EnableGrouping>
//				<EnableAggregation static=""true"" type=""Enum"">no</EnableAggregation>
//				<EnableColumnMoving static=""true"" type=""Enum"">no</EnableColumnMoving>
//				<EnableMultipleSelection static=""true"" type=""Enum"">yes</EnableMultipleSelection>
//				<EnableRangeSelection static=""true"" type=""Enum"">yes</EnableRangeSelection>
//				<EnableJumpBar static=""true"" type=""Enum"">no</EnableJumpBar>
//				<EnablePagination static=""true"" type=""Enum"">yes</EnablePagination>
//				<EnableDropdownColor static=""true"" type=""Enum"">yes</EnableDropdownColor>
//				<EnableStatusIcons static=""true"" type=""Enum"">yes</EnableStatusIcons>
//				<EnableTypeIcons static=""true"" type=""Enum"">no</EnableTypeIcons>
//				<NavigationTypesAllowed static=""true"" type=""Enum"">all</NavigationTypesAllowed>
//				<ReflowBehavior static=""true"" type=""Enum"">Reflow</ReflowBehavior>
//				<ShowAvatar static=""true"" type=""Enum"">yes</ShowAvatar>
//				<NumberOfListColumns static=""true"" type=""Whole.None"">3</NumberOfListColumns>
//				<ContextualLookupColumnFilters static=""true"" type=""Enum"">yes</ContextualLookupColumnFilters>
//				<LookupFilterBeginsWith static=""true"" type=""Enum"">no</LookupFilterBeginsWith>
//				<UseFirstColumnForLookupEdits static=""true"" type=""Enum"">no</UseFirstColumnForLookupEdits>
//				<GridCustomizerControlFullName static=""true"" type=""SingleLine.Text""/>
//				<EnableStatusColumn static=""true"" type=""Enum"">yes</EnableStatusColumn>
//			</parameters>
//		</customControl>
//		<customControl formFactor=""2"" name=""Microsoft.PowerApps.PowerAppsOneGrid"">
//			<parameters>
//				<data-set name=""Items"">
//					<ViewId>00000000-0000-0000-0000-000000000000</ViewId>
//				</data-set>
//				<AccessibleLabel static=""true"" type=""SingleLine.Text""/>
//				<EnableEditing static=""true"" type=""Enum"">no</EnableEditing>
//				<DisableChildItemsEditing static=""true"" type=""Enum"">no</DisableChildItemsEditing>
//				<EnableFiltering static=""true"" type=""Enum"">yes</EnableFiltering>
//				<EnableSorting static=""true"" type=""Enum"">yes</EnableSorting>
//				<EnableGrouping static=""true"" type=""Enum"">no</EnableGrouping>
//				<EnableAggregation static=""true"" type=""Enum"">no</EnableAggregation>
//				<EnableColumnMoving static=""true"" type=""Enum"">no</EnableColumnMoving>
//				<EnableMultipleSelection static=""true"" type=""Enum"">yes</EnableMultipleSelection>
//				<EnableRangeSelection static=""true"" type=""Enum"">yes</EnableRangeSelection>
//				<EnableJumpBar static=""true"" type=""Enum"">no</EnableJumpBar>
//				<EnablePagination static=""true"" type=""Enum"">yes</EnablePagination>
//				<EnableDropdownColor static=""true"" type=""Enum"">yes</EnableDropdownColor>
//				<EnableStatusIcons static=""true"" type=""Enum"">yes</EnableStatusIcons>
//				<EnableTypeIcons static=""true"" type=""Enum"">no</EnableTypeIcons>
//				<NavigationTypesAllowed static=""true"" type=""Enum"">all</NavigationTypesAllowed>
//				<ReflowBehavior static=""true"" type=""Enum"">Reflow</ReflowBehavior>
//				<ShowAvatar static=""true"" type=""Enum"">yes</ShowAvatar>
//				<NumberOfListColumns static=""true"" type=""Whole.None"">3</NumberOfListColumns>
//				<ContextualLookupColumnFilters static=""true"" type=""Enum"">yes</ContextualLookupColumnFilters>
//				<LookupFilterBeginsWith static=""true"" type=""Enum"">no</LookupFilterBeginsWith>
//				<UseFirstColumnForLookupEdits static=""true"" type=""Enum"">no</UseFirstColumnForLookupEdits>
//				<GridCustomizerControlFullName static=""true"" type=""SingleLine.Text""/>
//				<EnableStatusColumn static=""true"" type=""Enum"">yes</EnableStatusColumn>
//			</parameters>
//		</customControl>
//	</controlDescription>
//</controlDescriptions>
//";
    }
}
