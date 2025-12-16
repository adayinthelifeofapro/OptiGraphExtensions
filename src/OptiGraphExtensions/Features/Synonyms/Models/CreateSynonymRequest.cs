using System.ComponentModel.DataAnnotations;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class CreateSynonymRequest
    {
        [Required(ErrorMessage = "Synonym text is required")]
        [StringLength(255, ErrorMessage = "Synonym must be less than 255 characters")]
        public string Synonym { get; set; } = string.Empty;

        [Required(ErrorMessage = "Language is required")]
        public string Language { get; set; } = string.Empty;

        public SynonymSlot Slot { get; set; } = SynonymSlot.ONE;
    }
}