
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Greg.Xrm.Command.Commands.Solution.Model;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ListCommandExecutor(
	IOutput output,
	IOrganizationServiceRepository organizationServiceRepository)
	
	: ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			List<SolutionDto> solutionList;

			try
			{
				output.Write("Retrieving solutions list...");
				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("uniquename", "friendlyname", "version", "isvisible", "ismanaged", "createdon", "modifiedon");

				if (command.Type == ListCommand.SolutionType.Managed)
				{
					query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, true);
				}
				else if (command.Type == ListCommand.SolutionType.Unmanaged)
				{
					query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
				}

				if (!command.Hidden)
				{
					query.Criteria.AddCondition("isvisible", ConditionOperator.Equal, true);
				}

				var publisherLink = query.AddLink("publisher", "publisherid", "publisherid");
				publisherLink.EntityAlias = "p";
				publisherLink.Columns.AddColumns("uniquename", "friendlyname", "customizationprefix");
				query.AddOrder("uniquename", OrderType.Ascending);

				var result = await crm.RetrieveMultipleAsync(query);

				output.WriteLine("Done", ConsoleColor.Green);

				solutionList = result.Entities.Select(SolutionDto.FromEntity).ToList();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("Error while checking solution existence: " + ex.Message, ex);
			}

			if (solutionList.Count == 0)
			{
				return CommandResult.Fail("No solutions found in the current environment.");
			}

			output.WriteLine($"Found {solutionList.Count} solutions.", ConsoleColor.Green);






			if (command.OrderBy == ListCommand.OutputOrder.Name)
			{
				solutionList = solutionList.OrderBy(s => s.UniqueName).ToList();
			}
			else if (command.OrderBy == ListCommand.OutputOrder.CreatedOn)
			{
				solutionList = solutionList
					.OrderByDescending(s => s.CreatedOn)
					.ThenBy(x => x.UniqueName)
					.ToList();
			}
			else if (command.OrderBy == ListCommand.OutputOrder.ModifiedOn)
			{
				solutionList = solutionList
					.OrderByDescending(s => s.ModifiedOn)
					.ThenBy(x => x.UniqueName)
					.ToList();
			}
			else if (command.OrderBy == ListCommand.OutputOrder.Type)
			{
				solutionList = solutionList
					.OrderBy(s => !s.IsManaged ? 0 : 1)
					.ThenBy(s => s.IsVisible ? 0 : 1)
					.ThenBy(s => s.UniqueName)
					.ToList();
			}



			if (command.Format == ListCommand.OutputFormat.Table)
			{
				PrintTable(solutionList);
			}
			else if (command.Format == ListCommand.OutputFormat.TableCompact)
			{
				PrintTableCompact(solutionList);
			}
			else
			{
				PrintJson(solutionList);
			}

			return CommandResult.Success();
		}

		private void PrintTable(List<SolutionDto> solutionList)
		{
			output.WriteLine();
			output.WriteTable(solutionList, () =>
			[
				"Unique Name",
				"Friendly Name",
				"Version",
				"Type",
				"Publisher Unique Name",
				"Publisher Friendly Name",
				"Publisher Customization Prefix",
				"Is Visible",
				"Created On",
				"Modified On",
			],
			solutionList =>
			[
				solutionList.UniqueName ?? string.Empty,
				solutionList.FriendlyName ?? string.Empty,
				solutionList.Version ?? string.Empty,
				solutionList.IsManaged ? "Managed" : "Unmanaged",
				solutionList.PublisherUniqueName ?? string.Empty,
				solutionList.PublisherFriendlyName ?? string.Empty,
				solutionList.PublisherCustomizationPrefix ?? string.Empty,
				solutionList.IsVisible.ToString(),
				solutionList.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
				solutionList.ModifiedOn.ToString("yyyy-MM-dd HH:mm:ss"),
			],
			FormatSolutionRow);
		}

		private void PrintTableCompact(List<SolutionDto> solutionList)
		{
			output.WriteLine();
			output.WriteTable(solutionList, () =>
			[
				"Unique Name",
				"Version",
				"Type",
				"Publisher Unique Name",
				"Modified On"
			],
			solutionList =>
			[
				solutionList.UniqueName ?? string.Empty,
				solutionList.Version ?? string.Empty,
				solutionList.IsManaged ? "Managed" : "Unmanaged",
				solutionList.PublisherUniqueName ?? string.Empty,
				solutionList.ModifiedOn.ToString("yyyy-MM-dd HH:mm:ss"),
			], 
			FormatSolutionRow);
		}

		static ConsoleColor? FormatSolutionRow(int index, SolutionDto solution)
		{
			if (!solution.IsVisible)
			{
				return ConsoleColor.DarkGray;
			}

			if (!solution.IsManaged)
			{
				return ConsoleColor.Cyan;
			}

			return null;
		}

		private void PrintJson(List<SolutionDto> solutionList)
		{
			var json = JsonConvert.SerializeObject(solutionList, Formatting.Indented);
			output.WriteLine().WriteLine(json);
		}
	}
}
