using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults
{
    [ApiController]
    [Route("api/optimizely-graphextensions/pinned-results")]
    public class PinnedResultsApiController : ControllerBase
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public PinnedResultsApiController(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PinnedResult>>> GetPinnedResults([FromQuery] Guid? collectionId = null)
        {
            var query = _dataContext.PinnedResults.AsQueryable();
            
            if (collectionId.HasValue)
            {
                query = query.Where(pr => pr.CollectionId == collectionId.Value);
            }
            
            return await query.OrderBy(pr => pr.Priority).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PinnedResult>> GetPinnedResult(Guid id)
        {
            var pinnedResult = await _dataContext.PinnedResults.FindAsync(id);

            if (pinnedResult == null)
            {
                return NotFound();
            }

            return pinnedResult;
        }

        [HttpPost]
        public async Task<ActionResult<PinnedResult>> CreatePinnedResult(CreatePinnedResultRequest request)
        {
            // Validate that the collection exists
            var collectionExists = await _dataContext.PinnedResultsCollections
                .AnyAsync(c => c.Id == request.CollectionId);
            
            if (!collectionExists)
            {
                return BadRequest("The specified collection does not exist.");
            }

            var pinnedResult = new PinnedResult
            {
                Id = Guid.NewGuid(),
                CollectionId = request.CollectionId,
                Phrases = request.Phrases,
                TargetKey = request.TargetKey,
                Language = request.Language,
                Priority = request.Priority,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            _dataContext.PinnedResults.Add(pinnedResult);
            await _dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPinnedResult), new { id = pinnedResult.Id }, pinnedResult);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> UpdatePinnedResult(Guid id, UpdatePinnedResultRequest request)
        {
            var pinnedResult = await _dataContext.PinnedResults.FindAsync(id);
            if (pinnedResult == null)
            {
                return NotFound();
            }

            pinnedResult.Phrases = request.Phrases;
            pinnedResult.TargetKey = request.TargetKey;
            pinnedResult.Language = request.Language;
            pinnedResult.Priority = request.Priority;
            pinnedResult.IsActive = request.IsActive;

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PinnedResultExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeletePinnedResult(Guid id)
        {
            var pinnedResult = await _dataContext.PinnedResults.FindAsync(id);
            if (pinnedResult == null)
            {
                return NotFound();
            }

            _dataContext.PinnedResults.Remove(pinnedResult);
            await _dataContext.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> PinnedResultExists(Guid id)
        {
            return await _dataContext.PinnedResults.AnyAsync(e => e.Id == id);
        }
    }

    public class CreatePinnedResultRequest
    {
        public Guid CollectionId { get; set; }
        public string? Phrases { get; set; }
        public string? TargetKey { get; set; }
        public string? Language { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePinnedResultRequest
    {
        public string? Phrases { get; set; }
        public string? TargetKey { get; set; }
        public string? Language { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
    }
}