namespace Greg.Xrm.Command.Services.Pluralization
{
    public class PluralizationStrategy1040 : IPluralizationStrategy
    {
        public Task<string> GetPluralForAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return Task.FromResult(word);

			if (word.EndsWith('a')) return Task.FromResult(string.Concat(word.AsSpan(0, word.Length - 1), "e"));
			if (word.EndsWith('e')) return Task.FromResult(string.Concat(word.AsSpan(0, word.Length - 1), "i"));
			if (word.EndsWith('i')) return Task.FromResult(word);
			if (word.EndsWith('o')) return Task.FromResult(string.Concat(word.AsSpan(0, word.Length - 1), "i"));
			if (word.EndsWith('e')) return Task.FromResult(string.Concat(word.AsSpan(0, word.Length - 1), "i"));

			return Task.FromResult(word);
		}
    }
}