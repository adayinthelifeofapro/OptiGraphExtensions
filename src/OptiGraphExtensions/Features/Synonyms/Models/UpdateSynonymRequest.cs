using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class UpdateSynonymRequest
    {
        [Required(ErrorMessage = "Synonym text is required")]
        [StringLength(255, ErrorMessage = "Synonym must be less than 255 characters")]
        public string Synonym { get; set; } = string.Empty;

        [Required(ErrorMessage = "Language is required")]
        public string Language { get; set; } = string.Empty;
    }
}