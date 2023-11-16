namespace Greg.Xrm.Command.Services.ComponentResolvers
{
    public interface IComponentResolver
    {
        Task<Dictionary<Guid, string>> GetNamesAsync(IReadOnlyList<Guid> componentIdSet);
    }
}
