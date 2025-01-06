using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class SetEnvImageCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWebResourceRepository webResourceRepository) : ICommandExecutor<SetEnvImageCommand>
	{


		public async Task<CommandResult> ExecuteAsync(SetEnvImageCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			output.Write($"Retrieving the webresource called <{command.WebResourceUniqueName}>...");
			WebResource logo;
			try
			{
				var webResourceList = await webResourceRepository.GetByNameAsync(crm, new[] { command.WebResourceUniqueName }, false);

				if (webResourceList.Count == 0)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					return CommandResult.Fail("The webresource with the specified name does not exists");
				}

				logo = webResourceList[0];
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			if (logo.webresourcetype?.Value != (int)WebResourceType.ImagePng &&
				logo.webresourcetype?.Value != (int)WebResourceType.ImageGif &&
				logo.webresourcetype?.Value != (int)WebResourceType.ImageJpg)
			{
				return CommandResult.Fail($"The webresource type {logo.GetFormattedType()} is not supported for the logo");
			}




			output.Write("Retrieving the current theme...");
			Entity? theme;
			try
			{
				var query = new QueryExpression("theme");
				query.ColumnSet.AllColumns = true;
				query.Criteria.AddCondition("isdefaulttheme", ConditionOperator.Equal, true);
				query.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(query);

				theme = result.Entities.FirstOrDefault();
				if (theme == null)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					return CommandResult.Fail("No default theme found");
				}

				if (command.CloneTheme || !theme.GetAttributeValue<bool>("type"))
				{
					var oldName = theme.GetAttributeValue<string>("name");

					theme = theme.Clone("themeid", "type", "isdefaulttheme");
					theme["type"] = true; // custom theme
					theme["name"] = $"{oldName} - Copy";
				}

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}



			output.Write("Setting the logo...");
			try
			{
				theme["logoid"] = logo.ToEntityReference();

				if (theme.Id == Guid.Empty)
				{
					theme.Id = await crm.CreateAsync(theme);
				}
				else
				{
					await crm.UpdateAsync(theme);
				}
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			output.Write("Publishing the theme...");
			try
			{

				var request = new PublishThemeRequest
				{
					Target = theme.ToEntityReference()
				};

				await crm.ExecuteAsync(request);

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}


			return CommandResult.Success();
		}
	}
}
