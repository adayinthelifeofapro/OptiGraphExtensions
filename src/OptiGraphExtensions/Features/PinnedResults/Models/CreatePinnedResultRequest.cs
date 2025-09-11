namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class CreatePinnedResultRequest
    {
        public Guid CollectionId { get; set; }
        public string? Phrases { get; set; }
        public string? TargetKey { get; set; }
        public string? Language { get; set; }
        public int Priority { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}