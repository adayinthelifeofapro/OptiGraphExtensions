using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.StopWords.Models;
using OptiGraphExtensions.Features.StopWords.Services.Abstractions;

namespace OptiGraphExtensions.Features.StopWords
{
    [ApiController]
    [Route("api/optimizely-graphextensions/stopwords")]
    [Authorize(Policy = OptiGraphExtensionsConstants.AuthorizationPolicy)]
    public class StopWordsApiController : ControllerBase
    {
        private readonly IStopWordService _stopWordService;

        public StopWordsApiController(IStopWordService stopWordService)
        {
            _stopWordService = stopWordService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StopWord>>> GetStopWords([FromQuery] string? language = null)
        {
            IEnumerable<StopWord> stopWords;

            if (!string.IsNullOrWhiteSpace(language))
            {
                stopWords = await _stopWordService.GetStopWordsByLanguageAsync(language);
            }
            else
            {
                stopWords = await _stopWordService.GetAllStopWordsAsync();
            }

            return Ok(stopWords);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StopWord>> GetStopWord(Guid id)
        {
            var stopWord = await _stopWordService.GetStopWordByIdAsync(id);

            if (stopWord == null)
            {
                return NotFound();
            }

            return stopWord;
        }

        [HttpPost]
        public async Task<ActionResult<StopWord>> CreateStopWord(CreateStopWordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Word))
            {
                return BadRequest("Stop word is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest("Language is required.");
            }

            var stopWord = await _stopWordService.CreateStopWordAsync(request.Word, request.Language, User.Identity?.Name);
            return CreatedAtAction(nameof(GetStopWord), new { id = stopWord.Id }, stopWord);
        }

        [HttpPut("{id}")]
        [Route("{id}")]
        public async Task<IActionResult> UpdateStopWord(Guid id, UpdateStopWordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Word))
            {
                return BadRequest("Stop word is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest("Language is required.");
            }

            var updatedStopWord = await _stopWordService.UpdateStopWordAsync(id, request.Word, request.Language);
            if (updatedStopWord == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Route("{id}")]
        public async Task<IActionResult> DeleteStopWord(Guid id)
        {
            var deleted = await _stopWordService.DeleteStopWordAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
