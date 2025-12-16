namespace OptiGraphExtensions.Features.Common.Services
{
    public interface ILanguageService
    {
        IEnumerable<LanguageInfo> GetEnabledLanguages();
    }

    public class LanguageInfo
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
