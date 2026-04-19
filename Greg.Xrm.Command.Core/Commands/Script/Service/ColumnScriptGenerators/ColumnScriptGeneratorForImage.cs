using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForImage(ImageAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateBase(script, "image");
			script.Append(" --maxSizeInKB ").Append(field.MaxSizeInKB);
			script.Append(" --storeOnlyThumbnailImage ").Append(!field.CanStoreFullImage.GetValueOrDefault());
		}
	}
}
