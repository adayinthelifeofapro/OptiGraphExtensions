using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class UpdateGraphCollectionIdRequest
    {
        [Required(ErrorMessage = "Graph Collection ID is required")]
        [StringLength(255, ErrorMessage = "Graph Collection ID must be less than 255 characters")]
        public string GraphCollectionId { get; set; } = string.Empty;
    }
}