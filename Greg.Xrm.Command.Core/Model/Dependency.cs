using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	internal class Dependency : EntityWrapper
    {
        public Dependency(Entity entity) : base(entity)
        {
        }

#pragma warning disable IDE1006 // Naming Styles
		public Guid requiredcomponentbasesolutionid => Get<Guid>();
        public Guid requiredcomponentobjectid => Get<Guid>();

		public OptionSetValue dependencytype => Get<OptionSetValue>();
		public Guid requiredcomponentparentid => Get<Guid>();
        public OptionSetValue requiredcomponenttype => Get<OptionSetValue>();

        public EntityReference requiredcomponentnodeid => Get<EntityReference>();
        public OptionSetValue dependentcomponenttype => Get<OptionSetValue>();
        public Guid dependentcomponentparentid => Get<Guid>();
        public Guid dependentcomponentbasesolutionid => Get<Guid>();
        public EntityReference dependentcomponentnodeid => Get<EntityReference>();
        public Guid dependentcomponentobjectid => Get<Guid>();
#pragma warning restore IDE1006 // Naming Styles


		public string? DependencyTypeFormatted => GetFormatted(nameof(dependencytype));
        public string? RequiredComponentTypeFormatted => GetFormatted(nameof(requiredcomponenttype));
        public string? DependentComponentTypeFormatted => GetFormatted(nameof(dependentcomponenttype));
    }
}
