namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class CreatePinnedResultsCollectionRequest
    {
        public string? Title { get; set; }
        public bool IsActive { get; set; } = true;
    }
}