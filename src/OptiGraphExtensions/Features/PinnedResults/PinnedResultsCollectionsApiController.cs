using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults
{
    [ApiController]
    [Route("api/optimizely-graphextensions/pinned-results-collections")]
    public class PinnedResultsCollectionsApiController : ControllerBase
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public PinnedResultsCollectionsApiController(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PinnedResultsCollection>>> GetCollections()
        {
            return await _dataContext.PinnedResultsCollections.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PinnedResultsCollection>> GetCollection(Guid id)
        {
            var collection = await _dataContext.PinnedResultsCollections.FindAsync(id);

            if (collection == null)
            {
                return NotFound();
            }

            return collection;
        }

        [HttpPost]
        public async Task<ActionResult<PinnedResultsCollection>> CreateCollection(CreatePinnedResultsCollectionRequest request)
        {
            var collection = new PinnedResultsCollection
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            _dataContext.PinnedResultsCollections.Add(collection);
            await _dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> UpdateCollection(Guid id, UpdatePinnedResultsCollectionRequest request)
        {
            var collection = await _dataContext.PinnedResultsCollections.FindAsync(id);
            if (collection == null)
            {
                return NotFound();
            }

            collection.Title = request.Title;
            collection.IsActive = request.IsActive;

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CollectionExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteCollection(Guid id)
        {
            var collection = await _dataContext.PinnedResultsCollections.FindAsync(id);
            if (collection == null)
            {
                return NotFound();
            }

            // Also delete all related pinned results
            var relatedPinnedResults = await _dataContext.PinnedResults
                .Where(pr => pr.CollectionId == id)
                .ToListAsync();
            
            _dataContext.PinnedResults.RemoveRange(relatedPinnedResults);
            _dataContext.PinnedResultsCollections.Remove(collection);
            await _dataContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch]
        [Route("{id}/graph-id")]
        public async Task<IActionResult> UpdateGraphCollectionId(Guid id, UpdateGraphCollectionIdRequest request)
        {
            var collection = await _dataContext.PinnedResultsCollections.FindAsync(id);
            if (collection == null)
            {
                return NotFound();
            }

            collection.GraphCollectionId = request.GraphCollectionId;

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CollectionExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        private async Task<bool> CollectionExists(Guid id)
        {
            return await _dataContext.PinnedResultsCollections.AnyAsync(e => e.Id == id);
        }
    }

    public class CreatePinnedResultsCollectionRequest
    {
        public string? Title { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePinnedResultsCollectionRequest
    {
        public string? Title { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateGraphCollectionIdRequest
    {
        public string? GraphCollectionId { get; set; }
    }
}