using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Synonyms
{
    [ApiController]
    [Route("api/optimizely-graphextensions/synonyms")]
    public class SynonymsApiController : ControllerBase
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public SynonymsApiController(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Synonym>>> GetSynonyms()
        {
            return await _dataContext.Synonyms.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Synonym>> GetSynonym(Guid id)
        {
            var synonym = await _dataContext.Synonyms.FindAsync(id);

            if (synonym == null)
            {
                return NotFound();
            }

            return synonym;
        }

        [HttpPost]
        public async Task<ActionResult<Synonym>> CreateSynonym(CreateSynonymRequest request)
        {
            var synonym = new Synonym
            {
                Id = Guid.NewGuid(),
                SynonymItem = request.Synonym,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            _dataContext.Synonyms.Add(synonym);
            await _dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSynonym), new { id = synonym.Id }, synonym);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSynonym(Guid id, UpdateSynonymRequest request)
        {
            var synonym = await _dataContext.Synonyms.FindAsync(id);
            if (synonym == null)
            {
                return NotFound();
            }

            synonym.SynonymItem = request.Synonym;

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SynonymExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSynonym(Guid id)
        {
            var synonym = await _dataContext.Synonyms.FindAsync(id);
            if (synonym == null)
            {
                return NotFound();
            }

            _dataContext.Synonyms.Remove(synonym);
            await _dataContext.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> SynonymExists(Guid id)
        {
            return await _dataContext.Synonyms.AnyAsync(e => e.Id == id);
        }
    }

    public class CreateSynonymRequest
    {
        public string? Synonym { get; set; }
    }

    public class UpdateSynonymRequest
    {
        public string? Synonym { get; set; }
    }
}