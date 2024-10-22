using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class WebResource : EntityWrapper
	{
		private WebResource(Entity entity) : base(entity)
		{
		}

		public WebResource() : base("webresource")
		{
		}


		public string name
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}

		public string displayname
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}

		public OptionSetValue webresourcetype
		{
			get => this.Get<OptionSetValue>();
			set => this.SetValue(value);
		}


		public string description
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}


		public string content
		{
			get => this.Get<string>();
			set => this.SetValue(value);
		}


		public async Task<string> GetContentAsync(IOrganizationServiceAsync2 crm)
		{
			if (this.IsNew)
				return this.content;

			var myInternal = (IEntityWrapperInternal)this;


			var image = myInternal.GetPostImage();
			if (image.Contains("content"))
			{
				return this.content;				
			}

			// lazy load the content
			var entity = await crm.RetrieveAsync(this.EntityName, this.Id, new ColumnSet("content"));

			var preImage = myInternal.GetPreImage();
			preImage["content"] = entity.GetAttributeValue<string>("content");

			return this.content;
		}

		public bool TrySetTypeFromExtension()
		{
			var type = GetTypeFromExtension(this.name);
			if (!type.HasValue) return false;

			this.webresourcetype = new OptionSetValue((int)type.Value);
			return true;
		}




		public static WebResourceType? GetTypeFromExtension(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				throw new ArgumentNullException(nameof(fileName), nameof(fileName) + " cannot be null");

			var extension = fileName;
			if (fileName.EndsWith(".css.map"))
				return WebResourceType.StyleSheet;
			if (fileName.EndsWith(".js.map"))
				return WebResourceType.Script;


			if (fileName.Contains('.'))
			{
				extension = fileName[(fileName.LastIndexOf('.') + 1)..].ToLowerInvariant();
			}

			return extension switch
			{
				"htm" or "html" => WebResourceType.WebPage,
				"css" => WebResourceType.StyleSheet,
				"js" => WebResourceType.Script,
				"xml" => WebResourceType.Data,
				"png" => WebResourceType.ImagePng,
				"jpg" or "jpeg" => WebResourceType.ImageJpg,
				"gif" => WebResourceType.ImageGif,
				"xap" => WebResourceType.Silverlight,
				"xsl" or "xslt" => WebResourceType.Xsl,
				"ico" => WebResourceType.Icon,
				"svg" => WebResourceType.ImageSvg,
				"resx" => WebResourceType.Resx,
				_ => null,
			};
		}


		public class Repository : IWebResourceRepository
		{
			private readonly IOutput output;

			public Repository(IOutput output)
            {
				this.output = output;
			}

			public async Task<List<WebResource>> GetByNameAsync(IOrganizationServiceAsync2 crm, string[] fileNames, bool fetchContent = false)
			{
				if (fileNames.Length == 0)
					return [];

				var query = new QueryExpression("webresource");
				query.ColumnSet.AddColumns("name", "displayname", "webresourcetype", "description");
				if (fetchContent)
				{
					query.ColumnSet.AddColumn("content");
				}

				query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
				query.Criteria.AddCondition("ishidden", ConditionOperator.Equal, false);
				query.Criteria.AddCondition("iscustomizable", ConditionOperator.Equal, true);
				query.Criteria.AddCondition("name", ConditionOperator.In, fileNames.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				return result.Entities.Select(e => new WebResource(e)).ToList();
			}

			public async Task<List<WebResource>> GetBySolutionAsync(IOrganizationServiceAsync2 crm, string solutionUniqueName, bool fetchContent = false)
			{
				this.output.Write($"Retrieving web resources from solution '{solutionUniqueName}'...");

				var query = new QueryExpression("webresource");
				query.ColumnSet.AddColumns("name", "displayname", "webresourcetype", "description");
				if (fetchContent)
				{
					query.ColumnSet.AddColumn("content");
				}

				query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
				query.Criteria.AddCondition("ishidden", ConditionOperator.Equal, false);
				query.Criteria.AddCondition("iscustomizable", ConditionOperator.Equal, true);

				var solutionComponentLink = query.AddLink("solutioncomponent", "webresourceid", "objectid");

				var solutionLink = solutionComponentLink.AddLink("solution", "solutionid", "solutionid");
				solutionLink.LinkCriteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);
				this.output.WriteLine("DONE", ConsoleColor.Green) ;

				return result.Entities.Select(e => new WebResource(e)).ToList();
			}
		}
	}
}
