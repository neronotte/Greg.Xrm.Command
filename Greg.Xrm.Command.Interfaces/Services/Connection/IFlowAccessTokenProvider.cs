namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>Acquires a Power Automate management API token using the active authentication profile.</summary>
	public interface IFlowAccessTokenProvider
	{
		Task<string> GetFlowAccessTokenAsync(CancellationToken cancellationToken);
	}
}
