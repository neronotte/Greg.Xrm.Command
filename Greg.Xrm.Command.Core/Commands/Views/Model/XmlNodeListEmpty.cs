using System.Collections;
using System.Xml;

namespace Greg.Xrm.Command.Commands.Views.Model
{
	class XmlNodeListEmpty : XmlNodeList
	{
		public static XmlNodeListEmpty Instance { get; } = new XmlNodeListEmpty();


		private XmlNodeListEmpty()
		{
			
		}

		public override int Count => 0;

		public override IEnumerator GetEnumerator()
		{
			return new List<XmlNode>().GetEnumerator();
		}

		public override XmlNode? Item(int index)
		{
			return null;
		}
	}
}
