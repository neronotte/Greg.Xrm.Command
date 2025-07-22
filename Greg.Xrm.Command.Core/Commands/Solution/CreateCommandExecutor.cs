using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class CreateCommandExecutor : ICommandExecutor<CreateCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public CreateCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}

		public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			var friendlyName = GetFriendlyName(command);
			var uniqueName = GetUniqueName(command);



			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				this.output.Write("Check if there is already a solution with unique name ").Write(uniqueName, ConsoleColor.Yellow).Write("...");

				var query = new QueryExpression("solution");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
				query.TopCount = 1;
				query.NoLock = true;

				var exists = (await crm.RetrieveMultipleAsync(query)).Entities.Count > 0;

				if (exists)
				{
					this.output.WriteLine("Found", ConsoleColor.Red);
					return CommandResult.Fail("A solution with the same unique name already exists");
				}
				else
				{
					this.output.WriteLine("OK", ConsoleColor.Green);
				}

				var publisherRef = await GetOrCreatePublisherAsync(crm, command);

				this.output.Write("Creating solution...");

				var solution = new Entity("solution");
				solution["friendlyname"] = friendlyName;
				solution["uniquename"] = uniqueName;
				solution["publisherid"] = publisherRef;
				solution["version"] = "1.0.0.0";
				solution["ismanaged"] = false;
				solution.Id = await crm.CreateAsync(solution);

				this.output.WriteLine("Done", ConsoleColor.Green)
					.Write("  Solution ID: ")
					.WriteLine(solution.Id, ConsoleColor.Yellow);


				
				if (command.AddApplicationRibbons)
				{
					await AddApplicationRibbons(solution, crm);
				}

				return new CreateCommandResult(solution.Id, publisherRef.Id);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}

		}

		/// <summary>
		/// https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.addsolutioncomponentrequest?view=dataverse-sdk-latest
		/// 
		/// When you use AddSolutionComponentRequest to add application ribbons to your solution, 
		/// you must make sure to include the RibbonCustomization solution component that is associated with the active solution. 
		/// Use the Active Solution.SolutionId (FD140AAE-4DF4-11DD-BD17-0019B9312238) in the query 
		/// you use to retrieve the RibbonCustomization record.
		/// </summary>
		/// <param name="solution"></param>
		/// <param name="crm"></param>
		/// <returns></returns>
		private async Task AddApplicationRibbons(Entity solution, IOrganizationServiceAsync2 crm)
		{
			this.output.Write("Adding Application Ribbons...");
			try
			{
				Guid activeSolutionId = new("FD140AAE-4DF4-11DD-BD17-0019B9312238");

				var query = new QueryExpression("ribboncustomization");
				query.ColumnSet.AddColumn("ribboncustomizationid");
				query.Criteria.AddCondition("entity", ConditionOperator.Null);
				query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, activeSolutionId);

				var result = await crm.RetrieveMultipleAsync(query);
				var ribboncustomization = result.Entities[0];

				var request = new AddSolutionComponentRequest
				{
					SolutionUniqueName = solution["uniquename"].ToString(),
					ComponentType = (int)ComponentType.RibbonCustomization,
					ComponentId = ribboncustomization.Id
				};

				await crm.ExecuteAsync(request);
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine("Failed", ConsoleColor.Red);
				this.output.WriteLine(ex.Message, ConsoleColor.Red);
				throw;
			}
		}

		private async Task<EntityReference> GetOrCreatePublisherAsync(IOrganizationServiceAsync2 crm, CreateCommand command)
		{
			var query = new QueryExpression("publisher");

			if (!string.IsNullOrWhiteSpace(command.PublisherUniqueName))
			{
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.PublisherUniqueName);
			}
			else if (!string.IsNullOrWhiteSpace(command.PublisherCustomizationPrefix))
			{
				var prefix = command.PublisherCustomizationPrefix;
				// now we need to make sure that is up to 5 char long, and contains only letters or numbers
				if (prefix.Length > 5)
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, "Publisher customization prefix should be up to 5 characters long.");
				}

				if (!prefix.IsOnlyLowercaseLettersOrNumbers())
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, "Publisher customization prefix should contain only lowercase letters or numbers.");
				}

				query.Criteria.AddCondition("customizationprefix", ConditionOperator.Equal, prefix);
			}
			else
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "One of publisher unique name or customization prefix must be provided.");
			}
			query.TopCount = 2;
			query.NoLock = true;


			this.output.Write("Checking if publisher exists...");
			var result = await crm.RetrieveMultipleAsync(query);
			if (result.Entities.Count == 1)
			{
				this.output.WriteLine("Found", ConsoleColor.Green);
				return result.Entities[0].ToEntityReference();
			}
			this.output.WriteLine("Not found, publisher needs to be created.", ConsoleColor.Yellow);

			var publisher = new Entity("publisher");
			publisher["uniquename"] = GetPublisherUniqueName(command);
			publisher["friendlyname"] = GetPublisherFriendlyName(command);
			publisher["customizationprefix"] = GetPublisherCustomizationPrefix(command);
			publisher["customizationoptionvalueprefix"] = GetPublisherOptionSetPrefix(command);

			this.output.Write("Creating publisher...");
			publisher.Id = await crm.CreateAsync(publisher);

			this.output.WriteLine("Done", ConsoleColor.Green)
					.Write("  Publisher ID: ")
					.WriteLine(publisher.Id, ConsoleColor.Yellow);

			return publisher.ToEntityReference();
		}

		private static int? GetPublisherOptionSetPrefix(CreateCommand command)
		{
			if (command.PublisherOptionSetPrefix == null) return 10000; // default value... da mettere nei settings :)
			if (command.PublisherOptionSetPrefix <= 0 || command.PublisherOptionSetPrefix > 99999)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "Publisher option set prefix must be between 1 and 99999.");

			return command.PublisherOptionSetPrefix;
		}

		private static string GetPublisherCustomizationPrefix(CreateCommand command)
		{
			var prefix = command.PublisherCustomizationPrefix;
			if (prefix != null)
			{
				// now we need to make sure that is up to 5 char long, and contains only letters or numbers
				if (prefix.Length > 5)
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, "Publisher customization prefix should be up to 5 characters long.");
				}

				if (!prefix.IsOnlyLowercaseLettersOrNumbers())
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, "Publisher customization prefix should contain only lowercase letters or numbers.");
				}

				return prefix;
			}


			prefix = (command.PublisherUniqueName ?? command.PublisherFriendlyName)
				.OnlyLettersNumbersOrUnderscore()
				.Replace("_", "");

			if (string.IsNullOrWhiteSpace(prefix))
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "Unable to extrapolate publisher customization prefix. One of publisher unique name, friendly name or customization prefix must be provided.");
			}

			if (prefix.Length <= 5) return prefix;
			return prefix.Left(3);
		}

		private static string GetPublisherFriendlyName(CreateCommand command)
		{
			if (!string.IsNullOrWhiteSpace(command.PublisherFriendlyName)) return command.PublisherFriendlyName;
			if (!string.IsNullOrWhiteSpace(command.PublisherUniqueName)) return command.PublisherUniqueName;
			if (!string.IsNullOrWhiteSpace(command.PublisherCustomizationPrefix)) return command.PublisherCustomizationPrefix;

			throw new CommandException(CommandException.CommandInvalidArgumentValue, "Unable to extrapolate publisher friendly name. One of publisher friendly name, unique name or customization prefix must be provided.");
		}

		private static string GetPublisherUniqueName(CreateCommand command)
		{
			if (!string.IsNullOrWhiteSpace(command.PublisherUniqueName))
				return command.PublisherUniqueName;

			if (!string.IsNullOrWhiteSpace(command.PublisherFriendlyName))
			{
				var uniqueName = command.PublisherFriendlyName.OnlyLettersNumbersOrUnderscore();
				if (!string.IsNullOrWhiteSpace(uniqueName))
					return uniqueName;
			}

			if (!string.IsNullOrWhiteSpace(command.PublisherCustomizationPrefix))
				return command.PublisherCustomizationPrefix;

			throw new CommandException(CommandException.CommandInvalidArgumentValue, "Unable to extrapolate publisher unique name. One of publisher friendly name, unique name or customization prefix must be provided.");
		}



		private static string GetFriendlyName(CreateCommand command)
		{
			if (string.IsNullOrWhiteSpace(command.DisplayName))
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "The display name is required");

			return command.DisplayName;
		}

		private static string GetUniqueName(CreateCommand command)
		{
			if (!string.IsNullOrWhiteSpace(command.UniqueName))
				return command.UniqueName.OnlyLettersNumbersOrUnderscore();

			return command.DisplayName.OnlyLettersNumbersOrUnderscore();
		}
	}

}
