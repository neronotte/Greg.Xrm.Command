using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	public class SavedQuery : TableView
	{
		public SavedQuery(Entity entity) : base(entity)
		{
		}

		public SavedQuery() : base("savedquery") { }

		public bool isquickfindquery
		{
			get => this.Get<bool>();
			set => this.SetValue(value);
		}


		public class Repository : Repository<SavedQuery>, ISavedQueryRepository
		{
			public Repository() : base("savedquery", e => new SavedQuery(e), nameof(SavedQuery.isquickfindquery))
			{
			}
		}

		public override async Task<bool> SaveOrUpdateAsync(IOrganizationServiceAsync2 service)
		{
			if (!IsChangeTrackingEnabled)
			{
				throw new InvalidOperationException("Change tracking not enabled, unable to save the record");
			}

			if (IsDeleted)
			{
				return false;
			}

			var me = (IEntityWrapperInternal)this;


			var target = me.GetTarget();
			var preImage = me.GetPreImage();

			if (IsNew) // isNew
			{
				target.Id = await service.CreateAsync(target);
				preImage.Id = target.Id;
			}
			else if (target.Attributes.Count > 0)
			{
				var postImage = me.GetPostImage();
				await service.UpdateAsync(postImage);
			}



			// merge
			foreach (var key in target.Attributes.Keys)
			{
				preImage[key] = target[key];
			}
			target.Attributes.Clear();
			return true;
		}
	}
}
