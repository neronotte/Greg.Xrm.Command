namespace Greg.Xrm.Command
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class AliasAttribute : Attribute
	{
        public AliasAttribute(params string[] verbs)
		{
			this.Verbs = verbs ?? Array.Empty<string>();
			this.ExpandedVerbs = string.Join(" ", this.Verbs);
		}

		/// <summary>
		/// Gets the list of verbs that can be used to invoke the command
		/// </summary>
		public string[] Verbs { get; }

		/// <summary>
		/// Gets the list of verbs that can be used to invoke the command, as a single string
		/// </summary>
		public string ExpandedVerbs { get; }


		public override string ToString()
		{
			return ExpandedVerbs;
		}
	}
}
