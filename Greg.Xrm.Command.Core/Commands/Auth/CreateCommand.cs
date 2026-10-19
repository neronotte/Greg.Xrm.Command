using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Auth
{
	[Command("auth", "create", HelpText = "Create and store authentication profiles on this computer. Can be also used to update an existing authentication profile.")]
	public class CreateCommand : IValidatableObject, ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "The name you want to give to this authentication profile (maximum 30 characters).")]
		[Required]
		public string? Name { get; set; }

		[Option("environment", "env", Order = 3, HelpText = "If you want to connect to your environment via OAuth or Client ID / Secret, specify the environment URL here.")]
		public string? EnvironmentUrl { get; set; }

		[Option("applicationId", "id", Order = 4, HelpText = "The Application ID (Client ID) to authenticate with when using Client ID/Secret.")]
		public string? ApplicationId { get; set; }

		[Option("clientSecret", "s", Order = 5, HelpText = "The Client Secret to authenticate with when using Client ID/Secret.")]
		public string? ClientSecret { get; set; }

		[Option("conn", "cs", Order = 10, HelpText = "The [connection string](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect) that will be used to connect to the dataverse.")]
		public string? ConnectionString { get; set; }


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

			if (!this.EnvironmentUrl.HasData() && (this.ApplicationId.HasData() || this.ClientSecret.HasData()))
			{
				yield return new ValidationResult("You can only specify Application ID and Client Secret when also specifying an Environment URL.", [nameof(EnvironmentUrl), nameof(ApplicationId), nameof(ClientSecret)]);
			}

			if (this.ApplicationId.HasData() && !this.ClientSecret.HasData())
			{
				yield return new ValidationResult("If you specify an Application ID, you must also provide a Client Secret.", [nameof(ApplicationId), nameof(ClientSecret)]);
			}

			if (!this.ApplicationId.HasData() && this.ClientSecret.HasData())
			{
				yield return new ValidationResult("If you specify a Client Secret, you must also provide an Application ID.", [nameof(ApplicationId), nameof(ClientSecret)]);
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

			writer.WriteLine("### Client ID / Secret").WriteLine();
			writer.WriteLine("This is the preferred method for non interactive authentications.");
			writer.WriteLine();
			writer.WriteCodeBlockStart("Powershell");
			writer.WriteLine("auth create -n MyProfile -env \"https://contosotest.crm.dynamics.com\" -id \"1CF992AE-7203-4722-A53D-034D18C4593A\"  -cs \"REDACTED\"");
			writer.WriteCodeBlockEnd();
			writer.WriteLine();

			// User-facing summary of the issue #217 token caching (see docs/issue-217-auth-token-implementation-plan.md).
			writer.WriteLine("### How tokens are stored (OAuth profiles)").WriteLine();
			writer.WriteLine("For interactive OAuth profiles PACX relies on the token cache of the Dataverse SDK (MSAL):");
			writer.WriteLine();
			writer.WriteLine("- Tokens are encrypted at rest using the OS secure storage: DPAPI on Windows, the Secret Service keyring on Linux, the Keychain on macOS. The cache file is `%LOCALAPPDATA%/Greg.Xrm.Command/tokenCache/msal_token_cache.bin` (on Linux it only marks the keyring entry and stays empty).");
			writer.WriteLine("- Tokens are refreshed automatically, also while a long-running command is executing and across separate PACX commands. You sign in again only when the refresh token itself expires or is revoked.");
			writer.WriteLine("- After the first sign-in PACX stores the signed-in user (`Username`) in the profile: the SDK needs it to find your account in the cache. A `Username` you specified yourself is never changed.");
			writer.WriteLine("- Profiles created with older PACX versions ask you to sign in once after the upgrade; then they reuse the cache silently. The old plain-text token of the profile is deleted after that sign-in.");
			writer.WriteLine("- SQL queries (`pacx data query` with a `SELECT` statement) authenticate to the TDS endpoint with the same, automatically refreshed token.");
			writer.WriteLine();
			writer.WriteLine("If no secure storage is available (e.g. headless Linux without a keyring), PACX warns you when the profile is created and keeps the previous behavior: the last access token is stored in plain text, readable only by your user, and it cannot be refreshed, so commands lasting longer than about one hour may fail. For unattended or headless scenarios prefer a Client ID / Secret or Certificate profile.");
			writer.WriteLine();
		}
	}
}
