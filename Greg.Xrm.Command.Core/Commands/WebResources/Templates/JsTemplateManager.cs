using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.WebResources.Templates
{
    public class JsTemplateManager : IJsTemplateManager
	{
		private readonly ISettingsRepository settingsRepository;

		public JsTemplateManager(ISettingsRepository settingsRepository)
        {
			this.settingsRepository = settingsRepository;
		}


		public async Task<string> GetTemplateForAsync(JavascriptWebResourceType type, bool global)
		{
			var templateTypeName = "Template.Js." + type.ToString();

			var template = await this.settingsRepository.GetAsync<string>(templateTypeName);
			if (string.IsNullOrWhiteSpace(template))
			{
				template = GetDefaultTemplateFor(type, global);
				await this.settingsRepository.SetAsync(templateTypeName, template);
			}
			return template;
		}



		private static string GetDefaultTemplateFor(JavascriptWebResourceType type, bool global)
		{
			if (type == JavascriptWebResourceType.Form)
			{
				return Properties.Resources.TemplateJsForm;
			}
			if (type == JavascriptWebResourceType.Ribbon && global)
			{
				return Properties.Resources.TemplateJsRibbonGlobal;
			}
			if (type == JavascriptWebResourceType.Ribbon && !global)
			{
				return Properties.Resources.TemplateJsRibbonTable;
			}
			if (type == JavascriptWebResourceType.Other)
			{
				return Properties.Resources.TemplateJsOther;
			}

			throw new NotSupportedException($"The web resource type <{type}> is not supported.");
		}
	}
}
