using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Provides tokens for different resources using the current connection settings.
	/// </summary>
	public interface ITokenProvider
	{
		/// <summary>
		/// Gets an access token for the specified resource.
		/// </summary>
		/// <param name="resource">The resource for which to get a token (e.g. "https://api.bap.microsoft.com/").</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>The access token, or null if it could not be retrieved.</returns>
		Task<string?> GetTokenAsync(string resource, CancellationToken cancellationToken = default);
	}
}
