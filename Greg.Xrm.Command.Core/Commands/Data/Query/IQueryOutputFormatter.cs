using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public interface IQueryOutputFormatter
	{
		public Task Print(IReadOnlyCollection<Entity> entities, bool autorun, CancellationToken cancellationToken);
	}
}
