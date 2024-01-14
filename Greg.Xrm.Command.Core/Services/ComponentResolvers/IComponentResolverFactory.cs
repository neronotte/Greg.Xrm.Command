namespace Greg.Xrm.Command.Services.ComponentResolvers
{
	public interface IComponentResolverFactory
	{
		IComponentResolver? GetComponentResolverFor(ComponentType componentType);
	}
}