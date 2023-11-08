using Pluralize.NET;

namespace Greg.Xrm.Command.Services.Pluralization
{
    public class PluralizationStrategy1033 : IPluralizationStrategy
    {
        private readonly IPluralize service;

        public PluralizationStrategy1033()
        {
            service = new Pluralizer();
        }


        public Task<string> GetPluralForAsync(string word)
        {
            return Task.FromResult(service.Pluralize(word));
        }
    }
}