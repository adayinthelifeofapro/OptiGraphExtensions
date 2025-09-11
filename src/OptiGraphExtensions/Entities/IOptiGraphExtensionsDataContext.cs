﻿using Microsoft.EntityFrameworkCore;

namespace OptiGraphExtensions.Entities
{
    public interface IOptiGraphExtensionsDataContext
    {
        DbSet<Synonym> Synonyms { get; set; }
        
        DbSet<PinnedResultsCollection> PinnedResultsCollections { get; set; }
        
        DbSet<PinnedResult> PinnedResults { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
