namespace Greg.Xrm.Command.Parsing.Attributes
{
	/// <summary>
	/// Hides a specific command when launching in interactive experience mode.
	/// </summary>
	/// <param name="reason">The reason why the command is hidden in the interactive experience.</param>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class HideInInteractiveExperienceAttribute(string reason) : Attribute
	{
		/// <summary>
		/// Gets the reason why the current command is hidden in the interactive experience.
		/// </summary>
		public string Reason { get; } = reason;
	}
}
