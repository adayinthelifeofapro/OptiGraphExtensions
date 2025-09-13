namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class GraphPinnedResultResponse
    {
        public string? Phrases { get; set; }
        public string? TargetKey { get; set; }
        public string? Language { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public string? Id { get; set; }
        public string? CollectionId { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
    }
}