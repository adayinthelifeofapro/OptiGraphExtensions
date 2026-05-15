namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class SynonymCsvParseRow
    {
        public int LineNumber { get; set; }
        public string? Synonym { get; set; }
        public string? Language { get; set; }
        public string? Slot { get; set; }
        public string? ParseError { get; set; }
    }
}
