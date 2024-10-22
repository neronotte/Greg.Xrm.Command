using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "setEnvImage", HelpText=  "Sets the image that will be shown in the top left corner of the title bar. This setting applies for all MDAs of a given environment.")]
	[Alias("wr", "setLogo")]
	[Alias("wr", "setOrgImage")]
	public class SetEnvImageCommand
	{
		[Option("name", "n", HelpText = "The unique name of the web resource to set as the organization image. Must be a .png or .jpg image up to 200x50px.")]
		[Required]
		public string WebResourceUniqueName { get; set; } = string.Empty;


		[Option("cloneTheme", "c", HelpText = "If true, the theme will be cloned and the new image will be set as the organization image.", DefaultValue =false)]
		public bool CloneTheme { get; set; } = false;
	}
}
