using Microsoft.AspNetCore.Mvc;

using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults
{
    [ApiController]
    [Route("api/optimizely-graphextensions/pinned-results")]
    public class PinnedResultsApiController : ControllerBase
    {
        private readonly IPinnedResultService _pinnedResultService;

        public PinnedResultsApiController(IPinnedResultService pinnedResultService)
        {
            _pinnedResultService = pinnedResultService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PinnedResult>>> GetPinnedResults([FromQuery] Guid? collectionId = null)
        {
            var pinnedResults = await _pinnedResultService.GetAllPinnedResultsAsync(collectionId);
            return Ok(pinnedResults);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PinnedResult>> GetPinnedResult(Guid id)
        {
            var pinnedResult = await _pinnedResultService.GetPinnedResultByIdAsync(id);

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
            var collectionExists = await _pinnedResultService.CollectionExistsAsync(request.CollectionId);
            
            if (!collectionExists)
            {
                return BadRequest("The specified collection does not exist.");
            }

            var pinnedResult = await _pinnedResultService.CreatePinnedResultAsync(
                request.CollectionId,
                request.Phrases,
                request.TargetKey,
                request.Language,
                request.Priority,
                request.IsActive,
                User.Identity?.Name);

            return CreatedAtAction(nameof(GetPinnedResult), new { id = pinnedResult.Id }, pinnedResult);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> UpdatePinnedResult(Guid id, UpdatePinnedResultRequest request)
        {
            var updatedPinnedResult = await _pinnedResultService.UpdatePinnedResultAsync(
                id, 
                request.Phrases, 
                request.TargetKey, 
                request.Language, 
                request.Priority, 
                request.IsActive);

            if (updatedPinnedResult == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeletePinnedResult(Guid id)
        {
            var deleted = await _pinnedResultService.DeletePinnedResultAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }

}