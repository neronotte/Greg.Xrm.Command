using System.Text.RegularExpressions;

namespace Greg.Xrm.Command.Services.Pluralization
{
    public class PluralizationStrategy1040 : IPluralizationStrategy
    {
        public Task<string> GetPluralForAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return Task.FromResult(word);

			var plural = PluralizaNoun(word);

			return Task.FromResult(plural);
		}


		public static string PluralizaNoun(string noun)
		{
			if (string.IsNullOrWhiteSpace(noun))
				return noun;

			// Handle multiple words (compound nouns or noun phrases)
			var words = noun.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (words.Length > 1)
			{
				return PluralizePhrases(words);
			}

			return PluralizeSingleWord(noun.Trim());
		}

		private static string PluralizePhrases(string[] words)
		{
			// For compound nouns, typically only the last word is pluralized
			// unless it's an adjective-noun combination where both change
			var result = new List<string>();

			for (int i = 0; i < words.Length; i++)
			{
				if (i == words.Length - 1) // Last word (usually the main noun)
				{
					result.Add(PluralizeSingleWord(words[i]));
				}
				else
				{
					// Keep other words unchanged for now (could be enhanced for adjective agreement)
					result.Add(words[i]);
				}
			}

			return string.Join(" ", result);
		}


		private static string PluralizeSingleWord(string word)
		{
			var lowerWord = word.ToLower();
			
			// Check for irregular nouns first
			if (IrregularNouns.TryGetValue(lowerWord, out string? value))
				return PreserveCase(word, value);

			// Handle invariant nouns (words that don't change in plural)
			if (IsInvariantNoun(lowerWord))
				return word;

			// Apply regular pluralization rules
			var pluralizedLower = ApplyRegularRules(lowerWord);
			return PreserveCase(word, pluralizedLower);
		}

		private static string PreserveCase(string original, string pluralized)
		{
			if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(pluralized))
				return pluralized;

			// If original is all uppercase, return pluralized in uppercase
			if (original.All(c => !char.IsLetter(c) || char.IsUpper(c)))
				return pluralized.ToUpper();

			// If original starts with uppercase, capitalize the first letter of pluralized
			if (char.IsUpper(original[0]))
			{
				if (pluralized.Length == 1)
					return pluralized.ToUpper();
				
				return char.ToUpper(pluralized[0]) + pluralized[1..];
			}

			// Otherwise, return pluralized as is (lowercase)
			return pluralized;
		}

		private static bool IsInvariantNoun(string word)
		{
			// Words ending in accented vowels
			if (Regex.IsMatch(word, @"[àèéìíîòóù]$"))
				return true;

			// Generic rule: Words ending in consonants (including foreign words)
			if (word.Length > 0 && IsConsonant(word[^1]))
				return true;

			// Monosyllabic words
			if (word.Length <= 2)
				return true;

			// Words ending in -i (already plural or invariant)
			if (word.EndsWith('i') && word.Length > 2)
			{
				// But some -i words do change (like "sci" -> "sci", already handled in irregular)
				var penultimate = word[^2];
				if (IsVowel(penultimate))
					return true;
			}

			return false;
		}


		private static string ApplyRegularRules(string word)
		{
			// Masculine nouns ending in -o -> -i
			if (word.EndsWith('o'))
				return string.Concat(word.AsSpan(0, word.Length - 1), "i");

			// Feminine nouns ending in -a -> -e
			if (word.EndsWith('a'))
				return string.Concat(word.AsSpan(0, word.Length - 1), "e");

			// Nouns ending in -e -> -i (both masculine and feminine)
			if (word.EndsWith('e'))
				return string.Concat(word.AsSpan(0, word.Length - 1), "i");

			// Words ending in -co/-go
			if (word.EndsWith("co") || word.EndsWith("go"))
			{
				// If preceded by a vowel, usually -> -chi/-ghi
				if (word.Length > 2 && IsVowel(word[^3]))
				{
					if (word.EndsWith("co"))
						return string.Concat(word.AsSpan(0, word.Length - 2), "chi");
					else
						return string.Concat(word.AsSpan(0, word.Length - 2), "ghi");
				}
				// If preceded by a consonant, usually -> -ci/-gi
				else
				{
					if (word.EndsWith("co"))
						return string.Concat(word.AsSpan(0, word.Length - 2), "ci");
					else
						return string.Concat(word.AsSpan(0, word.Length - 2), "gi");
				}
			}

			// Words ending in -ca/-ga -> -che/-ghe
			if (word.EndsWith("ca"))
				return string.Concat(word.AsSpan(0, word.Length - 2), "che");
			if (word.EndsWith("ga"))
				return string.Concat(word.AsSpan(0, word.Length - 2), "ghe");

			// Words ending in -cia/-gia
			if (word.EndsWith("cia") || word.EndsWith("gia"))
			{
				// If preceded by a vowel, drop the i: -cia -> -ce, -gia -> -ge
				if (word.Length > 3 && IsVowel(word[^4]))
				{
					if (word.EndsWith("cia"))
						return string.Concat(word.AsSpan(0, word.Length - 3), "ce");
					else
						return string.Concat(word.AsSpan(0, word.Length - 3), "ge");
				}
				// If preceded by a consonant, keep the i: -cia -> -cie, -gia -> -gie
				else
				{
					if (word.EndsWith("cia"))
						return string.Concat(word.AsSpan(0, word.Length - 3), "cie");
					else
						return string.Concat(word.AsSpan(0, word.Length - 3), "gie");
				}
			}

			// Words ending in -io
			if (word.EndsWith("io"))
			{
				// If i is stressed, -> -ii
				// If i is unstressed, -> -i
				// For simplicity, we'll assume unstressed (most common case)
				return string.Concat(word.AsSpan(0, word.Length - 2), "i");
			}

			// Default case: if no rule applies, return unchanged
			return word;
		}

		private static bool IsVowel(char c)
		{
			return "aeiouàèéìíîòóù".Contains(char.ToLower(c));
		}

		private static bool IsConsonant(char c)
		{
			return char.IsLetter(c) && !IsVowel(c);
		}


		private static readonly Dictionary<string, string> IrregularNouns = new(StringComparer.OrdinalIgnoreCase)
	{
        // Truly irregular plurals (don't follow standard rules)
        {"uomo", "uomini"},
		{"dio", "dei"},
		{"bue", "buoi"},
		{"uovo", "uova"},
		{"braccio", "braccia"},
		{"ginocchio", "ginocchia"},
		{"labbro", "labbra"},
		{"osso", "ossa"},
		{"paio", "paia"},
		{"centinaio", "centinaia"},
		{"migliaio", "migliaia"},
		{"riso", "risa"}, // laughter
        {"eco", "echi"},
		
		// Invariant words (don't change)
		{"re", "re"},
        {"sci", "sci"},
        {"tè", "tè"},
        {"caffè", "caffè"},
        {"città", "città"},
        {"università", "università"},
        {"auto", "auto"},
        {"foto", "foto"},
        {"radio", "radio"},
        {"cinema", "cinema"},
        {"euro", "euro"},
        
        // Greek-origin words ending in -ma → -mi (irregular pattern)
        {"programma", "programmi"},
		{"problema", "problemi"},
		{"sistema", "sistemi"},
		{"clima", "climi"},
		{"tema", "temi"},
		{"panorama", "panorami"},
        {"schema", "schemi"},
		{"dogma", "dogmi"},
		{"diploma", "diplomi"},
		{"trauma", "traumi"},
		{"dilemma", "dilemmi"},
		{"emblema", "emblemi"},
		{"teorema", "teoremi"},
		{"poema", "poemi"},
		{"dramma", "drammi"},
        
        // Words ending in -i (invariant - already plural or don't change)
        {"analisi", "analisi"},
        {"crisi", "crisi"},
        {"tesi", "tesi"},
        {"sintesi", "sintesi"},
        {"ipotesi", "ipotesi"},
        {"diagnosi", "diagnosi"},
        {"prognosi", "prognosi"},
        {"metropoli", "metropoli"}
	};
	}
}