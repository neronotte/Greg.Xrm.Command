namespace Greg.Xrm.Command.Commands.WebResources.ApplyIconsRules
{
	class RuleToMatchNameEnding : IIconFinder
	{
		public string? Find(IReadOnlyCollection<string> icons, string tableName, string publisherPrefix)
		{
			if (icons == null) throw new ArgumentNullException(nameof(icons));
			if (tableName == null) throw new ArgumentNullException(nameof(tableName));
			if (publisherPrefix == null) throw new ArgumentNullException(nameof(publisherPrefix));

			var icon = icons.FirstOrDefault(x => x.EndsWith($"/{tableName}.svg", StringComparison.OrdinalIgnoreCase));
			return icon;
		}
	}
}
