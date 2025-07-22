using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	public class AddCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPublishXmlBuilder publishXmlBuilder
	)
	: ICommandExecutor<AddCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AddCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			try
			{
				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();
				var isStatusCode = string.Equals("statuscode", command.ColumnName, StringComparison.OrdinalIgnoreCase);



				if (!isStatusCode)
				{
					var value = command.Value;
					if (!value.HasValue)
					{
						output.Write($"Value has not been provided, generating a new one...");
						// we need to generate the value
						value = await GenerateNextValueAsync(crm, command, cancellationToken);
						output.WriteLine("Done: " + value, ConsoleColor.Green);
					}

					output.Write($"Updating option set '{command.Name}'...");
					var request = new InsertOptionValueRequest
					{
						OptionSetName = command.Name,
						EntityLogicalName = command.TableName,
						AttributeLogicalName = command.ColumnName,
						Value = value,
					};

					if (command.DisplayName is not null)
					{
						request.Label = new Label(command.DisplayName, defaultLanguageCode);
					}

					if (command.Color is not null)
					{
						request.Parameters["Color"] = command.Color;
					}

					await crm.ExecuteAsync(request, cancellationToken);

					publishXmlBuilder.AddGlobalOptionSet(command.Name);
				}
				else
				{
					output.Write($"Updating option set '{command.TableName}.{command.ColumnName}'...");
					var request = new InsertStatusValueRequest
					{
						OptionSetName = command.Name,
						EntityLogicalName = command.TableName,
						AttributeLogicalName = command.ColumnName,
						Value = command.Value,
						StateCode = command.StateCode
					};

					if (command.DisplayName is not null)
					{
						request.Label = new Label(command.DisplayName, defaultLanguageCode);
					}

					if (command.Color is not null)
					{
						request.Parameters["Color"] = command.Color;
					}

					await crm.ExecuteAsync(request, cancellationToken);
					publishXmlBuilder.AddTable(command.TableName);
				}


				output.WriteLine("Done", ConsoleColor.Green);


				output.Write($"Publishing changes...");
				await crm.ExecuteAsync(publishXmlBuilder.Build());
				output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private async Task<int> GenerateNextValueAsync(IOrganizationServiceAsync2 crm, AddCommand command, CancellationToken cancellationToken)
		{
			var optionSetValues = await GetCurrentValuesAsync(crm, command, cancellationToken);

			var maxValue = optionSetValues
				.Where(o => o.Value.HasValue)
				.Max(o => o.Value!.Value);

			var nextValue = maxValue + 1;
			return nextValue;
		}






		private async Task<IReadOnlyCollection<OptionMetadata>> GetCurrentValuesAsync(IOrganizationServiceAsync2 crm, AddCommand command, CancellationToken cancellationToken)
		{
			if (!string.IsNullOrWhiteSpace(command.Name))
			{
				var request = new RetrieveOptionSetRequest
				{
					Name = command.Name
				};

				var response = (RetrieveOptionSetResponse)await crm.ExecuteAsync(request, cancellationToken);
				return ((OptionSetMetadata)response.OptionSetMetadata).Options;
			}
			else
			{
				var request = new RetrieveAttributeRequest
				{
					EntityLogicalName = command.TableName,
					LogicalName = command.ColumnName,
					RetrieveAsIfPublished = false,
				};

				var response = (RetrieveAttributeResponse)await crm.ExecuteAsync(request);
				
				if (response.AttributeMetadata is PicklistAttributeMetadata optionSetMetadata)
				{
					return optionSetMetadata.OptionSet.Options;
				}
				if (response.AttributeMetadata is StatusAttributeMetadata statusAttributeMetadata)
				{
					return statusAttributeMetadata.OptionSet.Options;
				}

				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"Attribute type not supported: {command.TableName}.{command.ColumnName} is a {response.AttributeMetadata.AttributeType}");
			}
		}
	}
}
