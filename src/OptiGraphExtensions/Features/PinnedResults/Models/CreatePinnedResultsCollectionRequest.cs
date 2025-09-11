using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class CreatePinnedResultsCollectionRequest
    {
        [Required(ErrorMessage = "Collection title is required")]
        [StringLength(255, ErrorMessage = "Title must be less than 255 characters")]
        public string Title { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
    }
}