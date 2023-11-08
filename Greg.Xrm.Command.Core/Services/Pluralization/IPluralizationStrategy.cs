using System.Globalization;

namespace Greg.Xrm.Command.Services.Pluralization
{
    public interface IPluralizationStrategy
    {
        Task<string> GetPluralForAsync(string word);
    }
}