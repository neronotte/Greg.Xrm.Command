using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	public class SavedQuery : TableView
	{
		public SavedQuery(Entity entity) : base(entity)
		{
		}


		public class Repository : Repository<SavedQuery>, ISavedQueryRepository
		{
			public Repository() : base("savedquery", e => new SavedQuery(e))
			{
			}
		}
	}
}
