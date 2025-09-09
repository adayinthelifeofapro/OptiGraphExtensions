namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class GraphCollectionResponse
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public bool IsActive { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
    }
}