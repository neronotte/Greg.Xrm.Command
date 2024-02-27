namespace Greg.Xrm.Command.Commands.WebResources.ApplyIconsRules
{
	public class IconFinder : IIconFinder
	{
		private readonly List<IIconFinder> rules = new();

        public IconFinder()
        {
            this.rules.Add(new RuleToMatchTableName());
			this.rules.Add(new RuleToMatchTableNameWithPrefix());
			this.rules.Add(new RuleToMatchImagesFolder());
			this.rules.Add(new RuleToMatchNameEnding());
		}


        public string? Find(IReadOnlyCollection<string> icons, string tableName, string publisherPrefix)
		{
			foreach (var rule in this.rules)
			{
				var icon = rule.Find(icons, tableName, publisherPrefix);
				if (icon != null) return icon;
			}
			return null;
		}
	}
}
