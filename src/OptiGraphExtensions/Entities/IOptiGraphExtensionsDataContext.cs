using Microsoft.EntityFrameworkCore;

namespace OptiGraphExtensions.Entities
{
    public interface IOptiGraphExtensionsDataContext
    {
        DbSet<Synonyms> Synonyms { get; set; }
    }
}
