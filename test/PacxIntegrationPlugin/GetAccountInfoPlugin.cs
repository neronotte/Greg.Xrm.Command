using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PacxIntegration
{
	/// <summary>
	/// Dataverse Custom API plugin for nn_GetAccountInfo (Entity-bound on account).
	/// Reads the Target EntityReference from InputParameters, retrieves the account,
	/// and returns AccountName and City as response properties.
	/// </summary>
	public class GetAccountInfoPlugin : IPlugin
	{
		private const string OutAccountName = "AccountName";
		private const string OutCity        = "City";

		public void Execute(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));

			var context = (IPluginExecutionContext)
				serviceProvider.GetService(typeof(IPluginExecutionContext));
			var factory = (IOrganizationServiceFactory)
				serviceProvider.GetService(typeof(IOrganizationServiceFactory));
			var service = factory.CreateOrganizationService(context.UserId);

			if (!context.InputParameters.Contains("Target") || context.InputParameters["Target"] is not EntityReference target)
				throw new InvalidPluginExecutionException("Required input parameter 'Target' is missing or not an EntityReference.");

			var account = service.Retrieve(
				"account", target.Id,
				new ColumnSet("name", "address1_city"));

			context.OutputParameters[OutAccountName] = account.GetAttributeValue<string>("name") ?? string.Empty;
			context.OutputParameters[OutCity]        = account.GetAttributeValue<string>("address1_city") ?? string.Empty;
		}
	}
}
