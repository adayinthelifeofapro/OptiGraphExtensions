using System.Text.Json.Serialization;

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
        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? CreatedAt { get; set; }
        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? UpdatedAt { get; set; }
    }
}