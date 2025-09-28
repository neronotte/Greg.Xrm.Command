using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Services.Plugin
{
	/// <summary>
	/// Simple toolkit to register plugins via code
	/// </summary>
	public partial class PluginRegistrationToolkit(
		IOrganizationServiceAsync2 crm, 
		IOutput output)
	{

		private void Trace(string message, params object[] args)
		{
			output.WriteLine(string.Format(message, args), ConsoleColor.DarkGray);
		}


		/// <summary>
		/// Registers a plugin with the specified parameters.
		/// </summary>
		/// <param name="plugin">The plugin type.</param>
		/// <param name="messageName">The message to register for.</param>
		/// <param name="targetEntityName">The entity which the plugin should be attached to.</param>
		/// <param name="stage">The stage in which the plugin should be executed.</param>
		/// <param name="mode">Syncronous / Asyncronous</param>
		/// <param name="deployment">The default deployment mode.</param>
		/// <param name="filteringAttributes">The list of attributes to be used as filter for the plugin.</param>
		/// <param name="rank">The rank of the plugin, used to sort the plugin execution pipeline</param>
		/// <param name="withPreImage"><c>True</c> if a PreImage should be created too.</param>
		/// <param name="withPostImage"><c>True</c> if a PreImage should be created too.</param>
		/// <returns>The step entity.</returns>
		public SdkMessageProcessingStep RegisterPluginStep(
			PluginType pluginType,
			SdkMessage sdkMessage,
			SdkMessageFilter? sdkMessageFilter,
			Stage stage,
			Mode mode,
			Deployment deployment,
			string[] filteringAttributes,
			int rank,
			string? description,
			string? unsecureConfig,
			string? secureConfig,
			bool withPreImage,
			bool withPostImage,
			string? preImageName,
			string? postImageName)
		{
			this.Trace("*** REGISTER ***{0}Plugin: {1},{0}Message: {2},{0}EntityName: {3},{0}Stage: {4},{0}Mode: {5},{0}Deployment: {6},{0}Filtering attributes: {7},{0}Rank: {8},{0}PreImage: {9},{0}PostImage: {10}",
				Environment.NewLine,
				pluginType.Id,
				sdkMessage.name,
				sdkMessageFilter?.primaryobjecttypecode ?? "any",
				stage,
				mode,
				deployment,
				string.Join(", ", filteringAttributes),
				rank,
				withPreImage,
				withPostImage);



			this.Trace("Create SdkMessageProcessingStep...");
			var sdkMessageProcessingStep = CreateSdkMessageProcessingStep(pluginType, sdkMessage, sdkMessageFilter, filteringAttributes, mode, stage, deployment, rank, description, unsecureConfig, secureConfig);
			this.Trace("Create SdkMessageProcessingStep...COMPLETED");

			var stuffToDelete = new List<EntityReference>
			{
				sdkMessageProcessingStep.ToEntityReference()
			};
			try
			{
				if (sdkMessageFilter != null && withPreImage)
				{
					this.Trace("Create PreImage...");
					var image = CreateImage(sdkMessageFilter.primaryobjecttypecode, sdkMessageProcessingStep.ToEntityReference(), ImageType.PreImage, preImageName);
					stuffToDelete.Add(image.ToEntityReference());
					this.Trace("Create PreImage...COMPLETED");
				}

				if (sdkMessageFilter != null && withPostImage)
				{
					this.Trace("Create PostImage...");
					var image = CreateImage(sdkMessageFilter.primaryobjecttypecode, sdkMessageProcessingStep.ToEntityReference(), ImageType.PostImage, postImageName);
					stuffToDelete.Add(image.ToEntityReference());
					this.Trace("Create PostImage...COMPLETED");
				}
			}
			catch
			{
				for(var i = stuffToDelete.Count - 1; i >= 0; i--)
				{
					var entity = stuffToDelete[i];
					this.Trace("Deleting {0} {1} due to error during registration", entity.LogicalName, entity.Id);
					crm.Delete(entity.LogicalName, entity.Id);
					this.Trace("Deleting {0} {1} due to error during registration...COMPLETED", entity.LogicalName, entity.Id);
				}

				throw;
			}

			return sdkMessageProcessingStep;
		}




		private SdkMessageProcessingStep CreateSdkMessageProcessingStep(
			PluginType pluginType, 
			SdkMessage sdkMessage, 
			SdkMessageFilter? sdkMessageFilter, 
			string[] filteringAttributes, 
			Mode mode, 
			Stage stage, 
			Deployment deployment, 
			int rank, 
			string? description, 
			string? unsecureConfig, 
			string? secureConfig)
		{
			ArgumentNullException.ThrowIfNull(pluginType);
			ArgumentNullException.ThrowIfNull(sdkMessageFilter);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rank);

			var filter = sdkMessageFilter?.primaryobjecttypecode ?? "any Entity";

			var stepName = $"{pluginType.name}: {sdkMessage.name} of {filter}";

			EntityReference? configRef = null;
			if (!string.IsNullOrWhiteSpace(secureConfig))
			{
				var config = new Entity("sdkmessageprocessingstepsecureconfig");
				config["secureconfig"] = secureConfig;
				config.Id = crm.Create(config);

				configRef = config.ToEntityReference();
			}

			var entity = new SdkMessageProcessingStep
			{
				asyncautodelete = false,
				mode = new OptionSetValue((int)mode),
				name = stepName,
				eventhandler = pluginType.ToEntityReference(),
				plugintypeid = pluginType.ToEntityReference(),
				rank = rank,
				sdkmessageid = sdkMessage.ToEntityReference(),
				stage = new OptionSetValue((int)stage),
				supporteddeployment = new OptionSetValue((int)deployment),
				sdkmessagefilterid = sdkMessageFilter?.ToEntityReference(),
				invocationsource = new OptionSetValue(1),
				filteringattributes = filteringAttributes == null ? null : string.Join(",", filteringAttributes),
				description = description,
				configuration = unsecureConfig,
				sdkmessageprocessingstepsecureconfigid = configRef
			};

			entity.SaveOrUpdate(crm);
			return entity;
		}

		private SdkMessageProcessingStepImage CreateImage(string targetEntityName, EntityReference sdkMessageProcessingStep, ImageType imageType, string? name)
		{
			name ??= targetEntityName + "_" + (imageType == ImageType.PreImage ? "pre" : "post");

			var image = new SdkMessageProcessingStepImage
			{
				sdkmessageprocessingstepid = sdkMessageProcessingStep,
				messagepropertyname = "Target",
				name = name,
				entityalias = name,
				imagetype = new OptionSetValue((int)imageType)
			};

			image.SaveOrUpdate(crm);

			return image;
		}
	}
}
