using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.PinnedResults.Models
{
    public class UpdatePinnedResultRequest
    {
        [Required(ErrorMessage = "Phrases are required")]
        [StringLength(500, ErrorMessage = "Phrases must be less than 500 characters")]
        public string Phrases { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Target Key is required")]
        [StringLength(255, ErrorMessage = "Target Key must be less than 255 characters")]
        public string TargetKey { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Language is required")]
        [StringLength(10, ErrorMessage = "Language must be less than 10 characters")]
        public string Language { get; set; } = string.Empty;
        
        [Range(1, 1000, ErrorMessage = "Priority must be between 1 and 1000")]
        public int Priority { get; set; }
        
        public bool IsActive { get; set; }
    }
}