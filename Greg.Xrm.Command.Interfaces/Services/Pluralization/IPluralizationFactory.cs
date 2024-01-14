namespace Greg.Xrm.Command.Services.Pluralization
{
    public interface IPluralizationFactory
    {
        IPluralizationStrategy CreateFor(int languageCode);
    }
}
