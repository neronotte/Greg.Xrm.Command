using Greg.Xrm.Command.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Greg.Xrm.Command.Commands.WebResources.ProjectFile
{
	public class ProjectFileV1
	{
		public string Version { get; protected set; } = "1";


		public List<WebResourceMap> ExternalReferences { get; protected set; } = [];

		public bool ContainsExternalReference(WebResourceMap map)
		{
			if (map == null) return false;

			return ExternalReferences.Any(x => string.Equals(x.Source, map.Source, StringComparison.OrdinalIgnoreCase)) 
				|| ExternalReferences.Any(x => string.Equals( x.Target, map.Target, StringComparison.OrdinalIgnoreCase));
		}
	}


	public class WebResourceMap
	{
		public WebResourceMap(string source, string target, WebResourceType type)
		{
			Source = source;
			Target = target;
			Type = type;
		}
		protected WebResourceMap()
		{
			Source = string.Empty;
			Target = string.Empty;
			Type = WebResourceType.Data;
		}


		public string Source { get; protected set; }
		
		public string Target { get; protected set; } 

		[JsonConverter(typeof(StringEnumConverter))]
		public WebResourceType Type { get; protected set; }
	}
}
