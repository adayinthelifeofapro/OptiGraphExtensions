using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms
{
    [ApiController]
    [Route("api/optimizely-graphextensions/synonyms")]
    [Authorize(Policy = OptiGraphExtensionsConstants.AuthorizationPolicy)]
    public class SynonymsApiController : ControllerBase
    {
        private readonly ISynonymService _synonymService;

        public SynonymsApiController(ISynonymService synonymService)
        {
            _synonymService = synonymService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Synonym>>> GetSynonyms([FromQuery] string? language = null)
        {
            IEnumerable<Synonym> synonyms;

            if (!string.IsNullOrWhiteSpace(language))
            {
                synonyms = await _synonymService.GetSynonymsByLanguageAsync(language);
            }
            else
            {
                synonyms = await _synonymService.GetAllSynonymsAsync();
            }

            return Ok(synonyms);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Synonym>> GetSynonym(Guid id)
        {
            var synonym = await _synonymService.GetSynonymByIdAsync(id);

            if (synonym == null)
            {
                return NotFound();
            }

            return synonym;
        }

        [HttpPost]
        public async Task<ActionResult<Synonym>> CreateSynonym(CreateSynonymRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Synonym))
            {
                return BadRequest("Synonym text is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest("Language is required.");
            }

            var synonym = await _synonymService.CreateSynonymAsync(request.Synonym, request.Language, User.Identity?.Name);
            return CreatedAtAction(nameof(GetSynonym), new { id = synonym.Id }, synonym);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSynonym(Guid id, UpdateSynonymRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Synonym))
            {
                return BadRequest("Synonym text is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest("Language is required.");
            }

            var updatedSynonym = await _synonymService.UpdateSynonymAsync(id, request.Synonym, request.Language);
            if (updatedSynonym == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSynonym(Guid id)
        {
            var deleted = await _synonymService.DeleteSynonymAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}