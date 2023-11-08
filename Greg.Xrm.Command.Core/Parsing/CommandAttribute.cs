namespace Greg.Xrm.Command
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class CommandAttribute : Attribute
	{
		public CommandAttribute(params string[] verbs)
		{
			this.Verbs = verbs ?? Array.Empty<string>();
		}

		public string[] Verbs { get; }

		public string? HelpText { get; set; }
    }
}
