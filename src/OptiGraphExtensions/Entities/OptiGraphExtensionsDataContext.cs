using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OptiGraphExtensions.Entities
{
    public class OptiGraphExtensionsDataContext : DbContext, IOptiGraphExtensionsDataContext
    {
        private readonly ILogger<OptiGraphExtensionsDataContext> _logger;

        public OptiGraphExtensionsDataContext(
            DbContextOptions<OptiGraphExtensionsDataContext> options,
            ILogger<OptiGraphExtensionsDataContext> logger)
            : base(options)
        {
            _logger = logger;
            Debug.WriteLine($"OptiGraphExtensionsDataContext created: {DateTime.UtcNow}");
        }

        public DbSet<Synonym> Synonyms { get; set; }
        
        public DbSet<PinnedResultsCollection> PinnedResultsCollections { get; set; }
        
        public DbSet<PinnedResult> PinnedResults { get; set; }
    }
}
