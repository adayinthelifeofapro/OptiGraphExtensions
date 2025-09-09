using Microsoft.AspNetCore.Mvc;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Services;

namespace OptiGraphExtensions.Features.PinnedResults
{
    [ApiController]
    [Route("api/optimizely-graphextensions/pinned-results-collections")]
    public class PinnedResultsCollectionsApiController : ControllerBase
    {
        private readonly IPinnedResultsCollectionService _collectionService;

        public PinnedResultsCollectionsApiController(IPinnedResultsCollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PinnedResultsCollection>>> GetCollections()
        {
            var collections = await _collectionService.GetAllCollectionsAsync();
            return Ok(collections);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PinnedResultsCollection>> GetCollection(Guid id)
        {
            var collection = await _collectionService.GetCollectionByIdAsync(id);

            if (collection == null)
            {
                return NotFound();
            }

            return collection;
        }

        [HttpPost]
        public async Task<ActionResult<PinnedResultsCollection>> CreateCollection(CreatePinnedResultsCollectionRequest request)
        {
            var collection = await _collectionService.CreateCollectionAsync(
                request.Title,
                request.IsActive,
                User.Identity?.Name);

            return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> UpdateCollection(Guid id, UpdatePinnedResultsCollectionRequest request)
        {
            var updatedCollection = await _collectionService.UpdateCollectionAsync(
                id,
                request.Title,
                request.IsActive);

            if (updatedCollection == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteCollection(Guid id)
        {
            var deleted = await _collectionService.DeleteCollectionAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPatch]
        [Route("{id}/graph-id")]
        public async Task<IActionResult> UpdateGraphCollectionId(Guid id, UpdateGraphCollectionIdRequest request)
        {
            var updatedCollection = await _collectionService.UpdateGraphCollectionIdAsync(id, request.GraphCollectionId);
            if (updatedCollection == null)
            {
                return NotFound();
            }

            return NoContent();
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