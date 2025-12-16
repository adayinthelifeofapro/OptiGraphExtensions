using EPiServer.DataAbstraction;

namespace OptiGraphExtensions.Features.Common.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public LanguageService(ILanguageBranchRepository languageBranchRepository)
        {
            _languageBranchRepository = languageBranchRepository;
        }

        public IEnumerable<LanguageInfo> GetEnabledLanguages()
        {
            var languageBranches = _languageBranchRepository.ListEnabled();

            return languageBranches
                .OrderBy(lb => lb.SortIndex)
                .Select(lb => new LanguageInfo
                {
                    LanguageCode = lb.LanguageID,
                    DisplayName = lb.Name
                })
                .ToList();
        }
    }
}
