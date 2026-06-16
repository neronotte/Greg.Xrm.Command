using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PacxIntegration
{
	/// <summary>
	/// Dataverse Custom API plugin for nn_GetContactInfo (Entity-bound on contact).
	/// Reads the Target EntityReference from InputParameters, retrieves the contact,
	/// and returns FullName and Email as response properties.
	/// </summary>
	public class GetContactInfoPlugin : IPlugin
	{
		private const string OutFullName = "FullName";
		private const string OutEmail    = "Email";

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

			var contact = service.Retrieve(
				"contact", target.Id,
				new ColumnSet("fullname", "emailaddress1"));

			context.OutputParameters[OutFullName] = contact.GetAttributeValue<string>("fullname") ?? string.Empty;
			context.OutputParameters[OutEmail]    = contact.GetAttributeValue<string>("emailaddress1") ?? string.Empty;
		}
	}
}
