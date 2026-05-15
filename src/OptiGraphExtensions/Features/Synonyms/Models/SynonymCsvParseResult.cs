namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class SynonymCsvParseResult
    {
        public IReadOnlyList<SynonymCsvParseRow> Rows { get; set; } = Array.Empty<SynonymCsvParseRow>();
        public IReadOnlyList<string> GlobalErrors { get; set; } = Array.Empty<string>();
        public bool HeaderDetected { get; set; }
    }
}
