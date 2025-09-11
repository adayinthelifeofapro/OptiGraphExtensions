using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class PinnedResultsCollectionModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Collection title is required")]
        [StringLength(255, ErrorMessage = "Collection title must be less than 255 characters")]
        public string? Title { get; set; }

        public bool IsActive { get; set; } = true;
    }
}