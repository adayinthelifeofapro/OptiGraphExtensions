namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class UpdatePinnedResultRequest
    {
        public string? Phrases { get; set; }
        public string? TargetKey { get; set; }
        public string? Language { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
    }
}