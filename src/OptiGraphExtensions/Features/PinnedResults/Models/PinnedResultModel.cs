using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class PinnedResultModel
    {
        public Guid Id { get; set; }

        public Guid CollectionId { get; set; }

        [Required(ErrorMessage = "Phrases are required")]
        [StringLength(1000, ErrorMessage = "Phrases must be less than 1000 characters")]
        public string? Phrases { get; set; }

        [Required(ErrorMessage = "Target Key is required")]
        [StringLength(100, ErrorMessage = "Target Key must be less than 100 characters")]
        public string? TargetKey { get; set; }

        [StringLength(500, ErrorMessage = "Target Name must be less than 500 characters")]
        public string? TargetName { get; set; }

        [Required(ErrorMessage = "Language is required")]
        [StringLength(10, ErrorMessage = "Language must be less than 10 characters")]
        public string? Language { get; set; }

        public int Priority { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}