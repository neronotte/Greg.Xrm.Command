using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "image", HelpText = "Creates an image column.")]
	public class CreateImageCommand : BaseCreateCommand, ICanProvideUsageExample
	{
		[Option("maxSizeInKB", "maxKb", HelpText = "For File or Image type columns indicates the maximum size in KB for the column. Do not provide a value if you want to stay with the default (32Mb for file columns, 10Mb for image columns). The value must be lower than 10485760 (1Gb) for file columns, and lower than 30720 (30Mb) for image columns.")]
		public int? MaxSizeInKB { get; set; }

		[Option("storeOnlyThumbnailImage", "thumb", HelpText = "For Image type columns indicates if the column stores only thumbnail-sized images.", DefaultValue = false)]
		public bool? StoreThumbnail { get; set; } = false;

		public void WriteUsageExamples(MarkdownWriter writer)
		{

			writer.WriteCodeBlock(@"# Creates a simple file column
pacx column create --type File -t tableName -n columnName

# specifies the max allowed size in KB (10 MB)
pacx column create --type File -t tableName -n columnName --maxSizeInKB 10240
pacx column create --type File -t tableName -n columnName -maxKb 10240
", "Powershell");


			writer.WriteParagraph("You can also specify if the image column should store only thumbnail-sized images using the `--canStoreOnlyThumbnailImage` option.");
			writer.WriteCodeBlock(@"# Create image column that stores only thumbnail-sized images
pacx column create --type Image -t tableName -n columnName --storeOnlyThumbnailImage
pacx column create --type Image -t tableName -n columnName -thumb
", "Powershell");

			writer.WriteParagraph("To generate a thumbnail-sized image, Dataverse will crop and resize the image to a square shape according to the following rules:");

			writer.WriteList(
			"Images with at least one side larger than 144 pixels are cropped on center to 144x144.",
			"Images with both sides smaller than 144 are cropped square to their smallest side."
			);

			writer.WriteParagraph("See [this article](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/image-column-data?tabs=sdk#resize-rules-for-thumbnail-sized-images) to get more info.");
		}
	}
}
