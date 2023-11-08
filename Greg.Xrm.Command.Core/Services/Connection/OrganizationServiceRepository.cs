using Greg.Xrm.Command.Services.Settings;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Services.Connection
{
	public class OrganizationServiceRepository : IOrganizationServiceRepository
	{
		private readonly ISettingsRepository settings;

		public OrganizationServiceRepository(ISettingsRepository settings)
        {
			this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}


		public string GetTokenCachePath()
		{
			var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			folderPath = Path.Combine(folderPath, "Greg.Xrm.Command");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			folderPath = Path.Combine(folderPath, "tokenCache");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			return folderPath;
		}




		public async Task<ConnectionSetting> GetAllConnectionDefinitionsAsync()
		{
			var connectionStrings = await this.settings.GetAsync<ConnectionSetting>("connections");
			return connectionStrings ?? new ConnectionSetting();
		}



		public async Task<IOrganizationServiceAsync2> GetCurrentConnectionAsync()
		{
			var connectionStrings = await this.settings.GetAsync<ConnectionSetting>("connections") 
				?? throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");
			
			if (!connectionStrings.TryGetCurrentConnectionString(out var connectionString))
				throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			return new ServiceClient(connectionString + ";TokenCacheStorePath=" + GetTokenCachePath());
		}

		public async Task SetConnectionAsync(string name, string connectionString)
		{
			try
			{
				var crm = new ServiceClient(connectionString + ";TokenCacheStorePath=" + GetTokenCachePath());
				await crm.ExecuteAsync(new WhoAmIRequest());


				var connectionStrings = await this.settings.GetAsync<ConnectionSetting>("connections");
				connectionStrings ??= new ConnectionSetting();

				connectionStrings.ConnectionStrings[name] = connectionString;
				connectionStrings.CurrentConnectionStringKey = name;

				await this.settings.SetAsync("connections", connectionStrings);
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				throw new CommandException(CommandException.ConnectionInvalid, "Dataverse connection has not been set yet.", ex);
			}
		}

		public async Task SetDefaultAsync(string name)
		{
			var connectionStrings = await this.settings.GetAsync<ConnectionSetting>("connections");
			if (connectionStrings == null)
				throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			if (!connectionStrings.Exists(name))
				throw new CommandException(CommandException.ConnectionInvalid, "Invalid connection name: " + name);

			if (connectionStrings.CurrentConnectionStringKey == name)
				return; // already set as default

			connectionStrings.CurrentConnectionStringKey = name;
			await this.settings.SetAsync("connections", connectionStrings);
		}

		public async Task SetDefaultSolutionAsync(string uniqueName)
		{
			var connectionStrings = await this.settings.GetAsync<ConnectionSetting>("connections");
			if (connectionStrings == null)
				throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			if (connectionStrings.CurrentConnectionStringKey == null)
				throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			connectionStrings.DefaultSolutions[connectionStrings.CurrentConnectionStringKey] = uniqueName;
			await this.settings.SetAsync("connections", connectionStrings);
		}

		public async Task<string?> GetCurrentDefaultSolutionAsync()
		{
			var connectionStrings = await this.settings.GetAsync<ConnectionSetting>("connections");
			if (connectionStrings == null)
				throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			if (connectionStrings.CurrentConnectionStringKey == null)
				throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			if (!connectionStrings.DefaultSolutions.TryGetValue(connectionStrings.CurrentConnectionStringKey, out var uniqueName))
				return null;

			return uniqueName;
		}
	}
}
