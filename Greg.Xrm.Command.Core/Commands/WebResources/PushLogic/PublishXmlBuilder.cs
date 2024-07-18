using Microsoft.Crm.Sdk.Messages;
using System.Text;

namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
    public class PublishXmlBuilder : IPublishXmlBuilder
	{
		private readonly List<Guid> webResourceList = new();

		public void Clear()
		{
			this.webResourceList.Clear();
		}

		public void AddWebResource(Guid id)
		{
			if(!this.webResourceList.Contains(id))
				this.webResourceList.Add(id);
		}



		public PublishXmlRequest? Build()
		{
			if (this.webResourceList.Count == 0)
				return null;




			var publishXml = new StringBuilder("<importexportxml>");

			if (this.webResourceList.Count > 0)
			{
				publishXml.AppendLine();
				publishXml.AppendLine("  <webresources>");

				foreach (var id in webResourceList)
				{
					publishXml.Append("    <webresource>").Append(id).Append("</webresource>").AppendLine();
				}

				publishXml.AppendLine("  </webresources>");
			}

			publishXml.Append("</importexportxml>"); 
			
			var request = new PublishXmlRequest
			{
				ParameterXml = publishXml.ToString()
			};

			return request;
		}
	}
}
