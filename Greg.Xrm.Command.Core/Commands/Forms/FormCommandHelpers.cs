using Greg.Xrm.Command.Commands.Forms.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Forms
{
	/// <summary>
	/// Logic shared by the commands of the forms group.
	/// </summary>
	internal static class FormCommandHelpers
	{
		/// <summary>
		/// Picks the main form to work on from the list of main forms of a table.
		/// When the table has more than one main form (or a form name has been
	/// provided explicitly), it must identify exactly one form; name matching is case-insensitive.
		/// </summary>
		public static bool TryGetForm(IOutput output, string tableName, string formName, List<Form> formList, out Form? form, out CommandResult? result)
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
				if (!string.IsNullOrWhiteSpace(formName) && !formList[0].name.Equals(formName, StringComparison.OrdinalIgnoreCase))
				{
					result = CommandResult.Fail($"Main form <{formName}> not found for table <{tableName}>");
					return false;
				}

				form = formList[0];
				output.WriteLine($"Main form found: {form.name}");
				return true;
			}

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

		/// <summary>
		/// Creates the temporary holding solution used to update a form through
		/// a solution roundtrip. The publisher of the given solution (or of the
		/// current default solution) is used for the temporary solution.
		/// </summary>
		public static async Task<(bool, CommandResult?, ITemporarySolution?)> CreateHoldingSolutionAsync(
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			IOutput output,
			IOrganizationServiceAsync2 crm,
			string? currentSolutionName)
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
			output.WriteLine("Done", ConsoleColor.Green);

			return (true, null, solution);
		}
	}
}
