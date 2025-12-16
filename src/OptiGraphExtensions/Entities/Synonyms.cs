using System.ComponentModel.DataAnnotations.Schema;

namespace OptiGraphExtensions.Entities
{
    [Table("tbl_OptiGraphExtensions_Synonyms")]
    public class Synonym
    {
        public Guid Id { get; set; }

        public string? SynonymItem { get; set; }

        public string? Language { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? CreatedBy { get; set; }
    }
}
