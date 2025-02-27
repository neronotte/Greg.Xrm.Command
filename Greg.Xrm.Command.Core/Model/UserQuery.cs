using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	public class UserQuery : TableView
	{
		public UserQuery(Entity entity) : base(entity)
		{
		}

		public UserQuery() : base("userquery") { }

		public class Repository : TableView.Repository<UserQuery>, IUserQueryRepository
		{
			public Repository() : base("userquery", e => new UserQuery(e))
			{
			}
		}
	}
}
