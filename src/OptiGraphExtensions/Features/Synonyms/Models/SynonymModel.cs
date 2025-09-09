using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class SynonymModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Synonym is required")]
        [StringLength(255, ErrorMessage = "Synonym must be less than 255 characters")]
        public string? Synonym { get; set; }
    }
}