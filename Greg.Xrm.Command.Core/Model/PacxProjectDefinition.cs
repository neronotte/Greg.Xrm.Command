namespace Greg.Xrm.Command.Model
{
	/// <summary>
	/// Represents a pacx project 
	/// </summary>
    public class PacxProjectDefinition
	{
		public string Version { get; set; } = "1.0";
		public bool IsSuspended { get; set; } = false;

		public string AuthProfileName { get; set; } = string.Empty;

		public string SolutionName { get; set; } = string.Empty;
	}
}
