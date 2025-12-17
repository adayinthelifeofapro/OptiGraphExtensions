using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.ContentSearch.Models;
using OptiGraphExtensions.Features.ContentSearch.Services.Abstractions;

namespace OptiGraphExtensions.Features.ContentSearch;

/// <summary>
/// API controller for content search functionality via Optimizely Graph
/// </summary>
[ApiController]
[Route("api/optimizely-graphextensions/content-search")]
[Authorize(Policy = OptiGraphExtensionsConstants.AuthorizationPolicy)]
public class ContentSearchApiController : ControllerBase
{
    private readonly IContentSearchService _contentSearchService;

    public ContentSearchApiController(IContentSearchService contentSearchService)
    {
        _contentSearchService = contentSearchService;
    }

    /// <summary>
    /// Search for content items in Optimizely Graph
    /// </summary>
    /// <param name="q">Search query text (minimum 2 characters)</param>
    /// <param name="contentType">Optional content type filter</param>
    /// <param name="language">Optional language filter (e.g., "en", "sv")</param>
    /// <param name="limit">Maximum results to return (default: 10, max: 10)</param>
    /// <returns>List of matching content items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContentSearchResult>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<ContentSearchResult>>> Search(
        [FromQuery(Name = "q")] string? q,
        [FromQuery] string? contentType = null,
        [FromQuery] string? language = null,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest("Search query must be at least 2 characters");
        }

        try
        {
            var results = await _contentSearchService.SearchContentAsync(
                q,
                contentType,
                language,
                Math.Min(limit, 10));

            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Get available content types for filtering
    /// </summary>
    /// <returns>List of content type names</returns>
    [HttpGet("content-types")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<string>>> GetContentTypes()
    {
        try
        {
            var contentTypes = await _contentSearchService.GetAvailableContentTypesAsync();
            return Ok(contentTypes);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
