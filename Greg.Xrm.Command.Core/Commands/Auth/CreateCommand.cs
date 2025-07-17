using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Greg.Xrm.Command.Commands.Auth
{
	[Command("auth", "create", HelpText = "Create and store authentication profiles on this computer. Can be also used to update an existing authentication profile.")]
	public class CreateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", HelpText = "The name you want to give to this authentication profile (maximum 30 characters).")]
		[Required]
		public string? Name { get; set; }

		[Option("conn", "cs", HelpText = "The [connection string](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect) that will be used to connect to the dataverse.")]
		public string? ConnectionString { get; set; }


		[Option("environment", "env", HelpText = "If you want to connect to your environment via OAuth, specify the environment URL here.")]
		public string? EnvironmentUrl { get; set; }


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!string.IsNullOrWhiteSpace(ConnectionString) && !string.IsNullOrWhiteSpace(EnvironmentUrl))
			{
				yield return new ValidationResult("You cannot specify both a connection string and an environment URL. Please choose one.", [nameof(ConnectionString), nameof(EnvironmentUrl)]);
			}
			if (string.IsNullOrWhiteSpace(ConnectionString) && string.IsNullOrWhiteSpace(EnvironmentUrl))
			{
				yield return new ValidationResult("You must specify either a connection string or an environment URL.", [nameof(ConnectionString), nameof(EnvironmentUrl)]);
			}

			if (!string.IsNullOrWhiteSpace(EnvironmentUrl))
			{
				// Regex pattern for validating Dynamics 365 environment URLs
				// Pattern: http://something.crm\d+.dynamics.com with optional leading /
				// something can be any combination of characters valid in a classic URL
				var pattern = @"^https://[a-zA-Z0-9\-_.]+\.crm\d*\.dynamics\.com/?$";
				var regex = new Regex(pattern);
				
				if (!regex.IsMatch(EnvironmentUrl))
				{
					yield return new ValidationResult("The environment URL must be in the format: https://something.crm[number].dynamics.com (with optional trailing slash).", [nameof(EnvironmentUrl)]);
				}
			}
		}



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("You have two alternative options to create an authentication profile:");
			writer.WriteLine();
			writer.WriteLine("- Providing a full Connection String");
			writer.WriteLine("- Simply specifying an Environment URL");

			writer.WriteLine();

			writer.WriteLine("### Connection String").WriteLine();
			writer.WriteLine("This is the preferred method for most users. Supports all authentication methods valid for your environment.");
			writer.WriteLine("The valid options for the connection string definition are documented in this [Microsoft Learn article](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect)");
			writer.WriteLine();
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("auth create -n MyProfile -cs \"AuthType=ClientSecret;Url=https://contosotest.crm.dynamics.com;ClientId={AppId};ClientSecret={ClientSecret}\"");
			writer.WriteLine("auth create -n MyProfile -cs \"AuthType=Office365;Username=jsmith@contoso.onmicrosoft.com;Password=passcode;Url=https://contoso.crm.dynamics.com\"");
			writer.WriteLine("auth create -n MyProfile -cs \"AuthType=OAuth;Username=jsmith@contoso.onmicrosoft.com;Password=passcode;Url=https://contosotest.crm.dynamics.com;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto");
			writer.WriteCodeBlockEnd();
			writer.WriteLine();

			writer.WriteLine("### Environment URL").WriteLine();
			writer.WriteLine("This is the preferred method for users who want to connect via OAuth. It will automatically use the OAuth flow to authenticate.");
			writer.WriteLine();
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("auth create -n MyProfile -env \"https://contosotest.crm.dynamics.com\"");
			writer.WriteCodeBlockEnd();
			writer.WriteLine();
		}
	}
}
