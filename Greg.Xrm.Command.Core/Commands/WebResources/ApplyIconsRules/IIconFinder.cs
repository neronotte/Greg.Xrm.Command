namespace Greg.Xrm.Command.Commands.WebResources.ApplyIconsRules
{
	public interface IIconFinder
	{
		string? Find(IReadOnlyCollection<string> icons, string tableName, string publisherPrefix);
	}
}
