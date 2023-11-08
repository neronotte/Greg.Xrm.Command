namespace Greg.Xrm.Command.Services.Pluralization
{
    public class PluralizationFactory : IPluralizationFactory
    {
        private readonly Dictionary<int, IPluralizationStrategy> cache = new Dictionary<int, IPluralizationStrategy>();


        public IPluralizationStrategy CreateFor(int languageCode)
        {
            if (cache.TryGetValue(languageCode, out var strategy))
                return strategy;

            strategy = CreateStrategy(languageCode);
            cache.Add(languageCode, strategy);
            return strategy;
        }

        private static IPluralizationStrategy CreateStrategy(int languageCode)
        {
            if (languageCode == 1040) return new PluralizationStrategy1040();
            if (languageCode == 1033) return new PluralizationStrategy1033();

            return new PluralizationStrategyIdentity();
        }
    }
}
