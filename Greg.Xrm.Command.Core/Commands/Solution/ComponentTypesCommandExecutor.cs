using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class ComponentTypesCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IAnsiConsole ansiConsole
		) : ICommandExecutor<ComponentTypesCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ComponentTypesCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);



			var request = new RetrieveAttributeRequest
			{
				EntityLogicalName = "solutioncomponent",
				LogicalName = "componenttype",
				RetrieveAsIfPublished = true
			};


			var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(request, cancellationToken);


			var values = ((PicklistAttributeMetadata)response.AttributeMetadata).OptionSet.Options
				.Select(x => new ComponentTypeInfo(x.Label.UserLocalizedLabel.Label, x.Value ?? 0))
				.OrderBy(x => x.Value)
				.ToList();

			output.WriteLine();

			if (command.Format == ComponentTypesCommand.OutputFormat.Json)
			{
				var dict = values.ToDictionary(x => x.Value, x => x.Name);
				var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
				output.WriteLine(json);
			}
			else
			{
				var table = new Spectre.Console.Table();
				table.RoundedBorder().BorderColor(Color.Blue);
				table.AddColumn("Name");
				table.AddColumn("Value");

				foreach (var value in values)
				{
					table.AddRow(value.Name, value.Value.ToString());
				}
				ansiConsole.Write(table);
			}

			return CommandResult.Success();
		}

		record ComponentTypeInfo(string Name, int Value);
	}
}
