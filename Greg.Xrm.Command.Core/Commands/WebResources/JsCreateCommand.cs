
namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "js", "create", HelpText = "(Preview) Creates a new Javascript webresource from a template")]
	[Alias("wr", "js", "create")]
	[Alias("wr", "create", "js")]
	[Alias("webresources", "create", "js")]
	public class JsCreateCommand
	{
		[Option("for", "f", HelpText = "Indicates if the JS web resource to create is for a form, a ribbon command, or other", DefaultValue = JavascriptWebResourceType.Form)]
		public JavascriptWebResourceType Type { get; set; } = JavascriptWebResourceType.Form;

		[Option("table", "t", HelpText = "Name of the table related to the JS. Mandatory for form JS. Optional for Ribbon JS (if not specified, is assumed as a global ribbon command). Must not be specified for Other JS.")]
		public string? TableName { get; set; }

		[Option("namespace", "ns", HelpText = "Namespace for the generated webresources. If not specified, the **uniquename** of the default solution publisher will be used.")]
		public string? Namespace { get; set; }

		[Option("solution", "s", HelpText = "The name of the solution that will contain the creted WebResource. Is used to deduct the namespace, if not explicitly set.")]
		public string? SolutionName { get; set; }
	}




	public enum JavascriptWebResourceType
	{
		Form,
		Ribbon,
		Other
	}
}
