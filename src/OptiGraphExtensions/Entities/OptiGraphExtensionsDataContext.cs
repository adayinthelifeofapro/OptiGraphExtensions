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
        }

        public DbSet<Synonym> Synonyms { get; set; }

        public DbSet<PinnedResultsCollection> PinnedResultsCollections { get; set; }

        public DbSet<PinnedResult> PinnedResults { get; set; }

        public DbSet<SavedQuery> SavedQueries { get; set; }

        public DbSet<ImportConfiguration> ImportConfigurations { get; set; }

        public DbSet<ImportExecutionHistory> ImportExecutionHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // Synonym indexes (from AddPerformanceIndexes migration)
            // ============================================
            modelBuilder.Entity<Synonym>()
                .HasIndex(s => s.Language)
                .HasDatabaseName("IX_Synonyms_Language");

            modelBuilder.Entity<Synonym>()
                .HasIndex(s => s.Slot)
                .HasDatabaseName("IX_Synonyms_Slot");

            modelBuilder.Entity<Synonym>()
                .HasIndex(s => new { s.Language, s.Slot })
                .HasDatabaseName("IX_Synonyms_Language_Slot");

            // ============================================
            // PinnedResult indexes (from AddPerformanceIndexes migration)
            // ============================================
            modelBuilder.Entity<PinnedResult>()
                .HasIndex(p => p.Language)
                .HasDatabaseName("IX_PinnedResults_Language");

            modelBuilder.Entity<PinnedResult>()
                .HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_PinnedResults_IsActive");

            // ============================================
            // PinnedResultsCollection indexes (from AddPerformanceIndexes migration)
            // ============================================
            modelBuilder.Entity<PinnedResultsCollection>()
                .HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_PinnedResultsCollections_IsActive");

            // ============================================
            // SavedQuery indexes (from AddPerformanceIndexes migration)
            // ============================================
            modelBuilder.Entity<SavedQuery>()
                .HasIndex(s => s.Name)
                .IsUnique()
                .HasDatabaseName("IX_SavedQueries_Name");

            modelBuilder.Entity<SavedQuery>()
                .HasIndex(s => s.IsActive)
                .HasDatabaseName("IX_SavedQueries_IsActive");

            // ============================================
            // ImportConfiguration indexes
            // ============================================
            modelBuilder.Entity<ImportConfiguration>()
                .HasIndex(c => c.TargetSourceId)
                .HasDatabaseName("IX_tbl_OptiGraphExtensions_ImportConfigurations_TargetSourceId");

            modelBuilder.Entity<ImportConfiguration>()
                .HasIndex(c => c.NextScheduledRunAt);

            modelBuilder.Entity<ImportConfiguration>()
                .HasIndex(c => c.NextRetryAt);

            // ============================================
            // ImportExecutionHistory configuration
            // ============================================
            modelBuilder.Entity<ImportExecutionHistory>()
                .HasOne(h => h.ImportConfiguration)
                .WithMany(c => c.ExecutionHistory)
                .HasForeignKey(h => h.ImportConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ImportExecutionHistory>()
                .HasIndex(h => h.ImportConfigurationId);
        }
    }
}
