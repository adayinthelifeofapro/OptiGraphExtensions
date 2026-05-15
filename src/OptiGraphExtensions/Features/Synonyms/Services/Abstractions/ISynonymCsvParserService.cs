using OptiGraphExtensions.Features.Synonyms.Models;

namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions
{
    public interface ISynonymCsvParserService
    {
        SynonymCsvParseResult Parse(string csvContent);
    }
}
