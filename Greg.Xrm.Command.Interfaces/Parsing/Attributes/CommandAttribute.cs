namespace Greg.Xrm.Command
{
	/// <summary>
	/// Defines a new command
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class CommandAttribute : Attribute
	{
		/// <summary>
		/// Creates a new instance of the <see cref="CommandAttribute"/> class
		/// </summary>
		/// <param name="verbs">The versm that can be used to invoke the command</param>
		public CommandAttribute(params string[] verbs)
		{
			this.Verbs = verbs ?? Array.Empty<string>();
		}

		/// <summary>
		/// Gets the list of verbs that can be used to invoke the command
		/// </summary>
		public string[] Verbs { get; }

		/// <summary>
		/// Gets or sets the help text for the command
		/// </summary>
		public string? HelpText { get; set; }

		/// <summary>
		/// Indicates if the command must be hidden from the help list
		/// </summary>
		public bool Hidden { get; set; }
    }
}
