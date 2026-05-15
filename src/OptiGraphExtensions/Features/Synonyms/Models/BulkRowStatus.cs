namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public enum BulkRowStatus
    {
        Valid = 0,
        DuplicateInFile = 1,
        DuplicateInDatabase = 2,
        Invalid = 3
    }
}
