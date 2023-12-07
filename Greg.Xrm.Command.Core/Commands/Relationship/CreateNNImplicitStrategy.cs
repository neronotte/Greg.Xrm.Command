using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Relationship
{
    public class CreateNNImplicitStrategy : ICreateNNStrategy
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceAsync2 crm;

		public CreateNNImplicitStrategy(IOutput output, IOrganizationServiceAsync2 crm)
        {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.crm = crm ?? throw new ArgumentNullException(nameof(crm));
		}





        public async Task<CommandResult> CreateAsync(CreateNNCommand command, string currentSolutionName, int defaultLanguageCode, string publisherPrefix)
		{
			await crm.CheckManyToManyEligibilityAsync(command.Table1);
			await crm.CheckManyToManyEligibilityAsync(command.Table2);



			var intersectEntitySchemaName = GetIntersectEntitySchemaName(command, publisherPrefix);
			var table1SchemaName = command.Table1;
			var table2SchemaName = command.Table2;

			output.Write("Executing CreateManyToManyRequest...");

			var request = new CreateManyToManyRequest
			{
				SolutionUniqueName = currentSolutionName,
				IntersectEntitySchemaName = intersectEntitySchemaName,
				ManyToManyRelationship = new ManyToManyRelationshipMetadata
				{
					Entity1LogicalName = table1SchemaName,
					Entity2LogicalName = table2SchemaName,
					Entity1AssociatedMenuConfiguration = new AssociatedMenuConfiguration
					{
						Behavior = command.MenuBehavior1,
						Group = command.MenuBehavior1 == AssociatedMenuBehavior.DoNotDisplay ? null : command.MenuGroup1,
						Label = GetLabel(command.MenuBehavior1, command.MenuLabel1, defaultLanguageCode),
						Order = command.MenuOrder1
					},
					Entity2AssociatedMenuConfiguration = new AssociatedMenuConfiguration
					{
						Behavior = command.MenuBehavior2,
						Group = command.MenuBehavior2 == AssociatedMenuBehavior.DoNotDisplay ? null : command.MenuGroup2,
						Label = GetLabel(command.MenuBehavior2, command.MenuLabel2, defaultLanguageCode),
						Order = command.MenuOrder2
					},
					Entity1IntersectAttribute = CreateRelationshipColumnName(command.Table1, publisherPrefix),
					Entity2IntersectAttribute = CreateRelationshipColumnName(command.Table2, publisherPrefix),
					//IsCustomizable = new BooleanManagedProperty(true),
					//IsManaged = new BooleanManagedProperty(false),
					SecurityTypes = SecurityTypes.Append,
					IsValidForAdvancedFind = true,
					SchemaName = intersectEntitySchemaName,
					IntersectEntityName = intersectEntitySchemaName,
					Entity1NavigationPropertyName = intersectEntitySchemaName,
					Entity2NavigationPropertyName = intersectEntitySchemaName
				}
			};

			var response = (CreateManyToManyResponse)await crm.ExecuteAsync(request);

			var result = CommandResult.Success();
			result["Relationship ID"] = response.ManyToManyRelationshipId;
			return result;
		}





		private static string GetIntersectEntitySchemaName(CreateNNCommand command, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(command.IntersectSchemaName))
			{
				if (!string.IsNullOrWhiteSpace(command.IntersectSchemaNameSuffix))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The intersection table schema name suffix cannot be specified when the intersection table schema name is specified. Current value is <{command.IntersectSchemaNameSuffix}>");

				if (!command.IntersectSchemaName.StartsWith(publisherPrefix + "_"))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The intersection table schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{command.IntersectSchemaName.Split("_").FirstOrDefault()}>");

				return command.IntersectSchemaName;
			}


			var sb = new StringBuilder();
			sb.Append(publisherPrefix);
			sb.Append('_');

			var tableName = command.Table1;
			if (tableName.StartsWith(publisherPrefix + "_"))
				sb.Append(tableName.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(tableName);

			sb.Append('_');

			tableName = command.Table2;
			if (tableName.StartsWith(publisherPrefix + "_"))
				sb.Append(tableName.AsSpan(publisherPrefix.Length + 1));
			else
				sb.Append(tableName);

			if (!string.IsNullOrWhiteSpace(command.IntersectSchemaNameSuffix))
			{
				sb.Append('_');
				sb.Append(command.IntersectSchemaNameSuffix.TrimStart('_'));
			}

			return sb.ToString();
		}

		public static Label? GetLabel(AssociatedMenuBehavior behavior, string? label, int languageCode)
		{
			if (behavior == AssociatedMenuBehavior.DoNotDisplay)
				return null;

			if (string.IsNullOrWhiteSpace(label))
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "The menu label is required when the menu behavior is set to UseLabel");

			return new Label(label, languageCode);
		}


		private static string CreateRelationshipColumnName(string entityName, string publisherPrefix)
		{
			if (entityName.StartsWith(publisherPrefix + "_"))
				return $"{entityName}id";

			return $"{publisherPrefix}_{entityName}id";
		}
	}
}
