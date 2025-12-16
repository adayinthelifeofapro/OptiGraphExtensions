using System.ComponentModel.DataAnnotations.Schema;

namespace OptiGraphExtensions.Entities
{
    [Table("tbl_OptiGraphExtensions_StopWords")]
    public class StopWord
    {
        public Guid Id { get; set; }

        public string? Word { get; set; }

        public string? Language { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? CreatedBy { get; set; }
    }
}
