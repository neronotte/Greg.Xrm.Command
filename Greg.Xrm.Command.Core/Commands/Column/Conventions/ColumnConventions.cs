namespace Greg.Xrm.Command.Commands.Column.Conventions
{
	public class ColumnConventions
	{
		public const string StorageKey = "column-conventions";

		public string SimpleOptionSetSuffix { get; set; } = "code";

		public string MultiselectOptionSetSuffix { get; set; } = "codes";

		public CasingStyle Casing { get; set; } = CasingStyle.Lower;
	}
}
