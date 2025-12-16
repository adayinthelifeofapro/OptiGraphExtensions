using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.StopWords.Models
{
    public class StopWordModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Stop word is required")]
        [StringLength(100, ErrorMessage = "Stop word must be less than 100 characters")]
        public string? Word { get; set; }

        [Required(ErrorMessage = "Language is required")]
        public string? Language { get; set; }
    }
}
