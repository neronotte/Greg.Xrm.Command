namespace Greg.Xrm.Command.Services.Pluralization
{
    public class PluralizationStrategyIdentity : IPluralizationStrategy
    {
        public Task<string> GetPluralForAsync(string word)
        {
            return Task.FromResult(word);
        }
    }
}
