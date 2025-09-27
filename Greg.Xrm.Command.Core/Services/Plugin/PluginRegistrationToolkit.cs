using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	/// <summary>
	/// Simple toolkit to register plugins via code
	/// </summary>
	public partial class PluginRegistrationToolkit(IOrganizationServiceAsync2 crm, IOutput output)
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


			// IF not, register plugin


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


		/// <summary>
		/// Unregisters a plugin step.
		/// </summary>
		/// <param name="plugin">The plugin type.</param>
		/// <param name="messageName">The message to register for.</param>
		/// <param name="targetEntityName">The entity which the plugin is attached.</param>
		/// <param name="throwIfMoreThanOneFound"><c>True</c> to throw an exception if there is more than one plugin matching the specified criteria.</param>
		/// <param name="throwIfNoneFound"><c>True</c> to throw an exception if there is no plugin matching the specified criteria.</param>
		public void UnregisterPluginStep(
			PluginType pluginType,
			SdkMessage sdkMessage,
			SdkMessageFilter? sdkMessageFilter,
			bool throwIfMoreThanOneFound = false,
			bool throwIfNoneFound = false)
		{
			this.Trace("*** UNREGISTER ***{0}Plugin: {1},{0}Message: {2},{0}EntityName: {3},{0}Throw if more than one found: {4},{0}Throw if none found: {5}",
				Environment.NewLine,
				pluginType.name!,
				sdkMessage.name,
				sdkMessageFilter?.primaryobjecttypecode ?? "any",
				throwIfMoreThanOneFound,
				throwIfNoneFound);


			this.Trace("Retrieve SdkMessageProcessingStep...");
			var sdkMessageProcessingStepList = this.RetrieveSdkMessageProcessingSteps(sdkMessage.name, sdkMessageFilter?.primaryobjecttypecode, pluginType);
			this.Trace("Retrieve SdkMessageProcessingStep...COMPLETED");

			if (throwIfMoreThanOneFound && sdkMessageProcessingStepList.Length > 1)
			{
				var message = string.Format("Found {0} plugins matching the specified criteria", sdkMessageProcessingStepList.Length);
				this.Trace(message);
				throw new InvalidOperationException(message);
			}


			if (throwIfNoneFound && sdkMessageProcessingStepList.Length == 0)
			{
				this.Trace("Found no plugins matching the specified criteria");
				throw new InvalidOperationException("Found no plugins matching the specified criteria");
			}

			if (sdkMessageProcessingStepList.Length == 0)
			{
				// Nessun plugin, indietro.
				this.Trace("Found no plugins matching the specified criteria. Moving forward...");
				return;
			}


			this.Trace("Retrieve SdkMessageProcessingStepImage...");
			var imageList = this.RetrieveSdmMessageProcessingStepImages(sdkMessageProcessingStepList);
			this.Trace("Retrieve SdkMessageProcessingStepImage...COMPLETED");

			if (imageList.Length == 0)
			{
				this.Trace("Found no images related to the current plugin.");
			}

			foreach (var entity in imageList)
			{
				this.Trace("Deleting image {0}", entity.Id);
				crm.Delete(entity.LogicalName, entity.Id);
				this.Trace("Deleting image {0}...COMPLETED", entity.Id);
			}
			foreach (var entity in sdkMessageProcessingStepList)
			{
				this.Trace("Deleting step {0}", entity.Id);
				crm.Delete(entity.LogicalName, entity.Id);
				this.Trace("Deleting step {0}...COMPLETED", entity.Id);
			}
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
			if (pluginType == null)
				throw new ArgumentNullException("pluginType");
			if (sdkMessageFilter == null)
				throw new ArgumentNullException("sdkMessageFilter");
			if (rank <= 0)
				throw new ArgumentOutOfRangeException("rank");

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

		private Entity CreateImage(string targetEntityName, EntityReference sdkMessageProcessingStep, ImageType imageType, string? name)
		{
			name = name ?? targetEntityName + "_" + (imageType == ImageType.PreImage ? "pre" : "post");

			var image = new Entity("sdkmessageprocessingstepimage");
			image["sdkmessageprocessingstepid"] = sdkMessageProcessingStep;
			image["messagepropertyname"] = "Target";
			image["name"] = name;
			image["entityalias"] = name;
			image["imagetype"] = new OptionSetValue((int)imageType);

			image.Id = crm.Create(image);
			return image;
		}



		private Entity[] RetrieveSdkMessageProcessingSteps(string messageName, string? targetEntityName, PluginType pluginType)
		{
			var query = new QueryExpression("sdkmessageprocessingstep");

			var linkPluginType = query.AddLink("plugintype", "plugintypeid", "plugintypeid");
			linkPluginType.LinkCriteria.AddCondition("plugintypeid", ConditionOperator.Equal, pluginType.Id);

			var linkMessage = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid");
			linkMessage.LinkCriteria.AddCondition("name", ConditionOperator.Equal, messageName);

			var linkFilter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid");
			linkFilter.LinkCriteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, targetEntityName);

			var linkFilterMessage = linkFilter.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid");
			linkFilterMessage.LinkCriteria.AddCondition("name", ConditionOperator.Equal, messageName);

			query.ColumnSet.AllColumns = false;

			var sdkMessageProcessingStepList = crm.RetrieveMultiple(query);
			return sdkMessageProcessingStepList.Entities.ToArray();
		}

		private Entity[] RetrieveSdmMessageProcessingStepImages(Entity[] sdkMessageProcessingStepList)
		{
			var stepIdList = sdkMessageProcessingStepList.Select(x => x.Id)
														 .Cast<object>()
														 .ToArray();

			var query = new QueryExpression("sdkmessageprocessingstepimage");
			query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.In, stepIdList);
			query.ColumnSet.AllColumns = false;

			var imageList = crm.RetrieveMultiple(query);
			return imageList.Entities.ToArray();
		}
	}
}
