using Microsoft.EntityFrameworkCore;

namespace OptiGraphExtensions.Entities
{
    public interface IOptiGraphExtensionsDataContext
    {
        DbSet<Synonym> Synonyms { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
