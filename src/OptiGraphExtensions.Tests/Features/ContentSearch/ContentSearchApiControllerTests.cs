using Microsoft.AspNetCore.Mvc;
using Moq;
using OptiGraphExtensions.Features.ContentSearch;
using OptiGraphExtensions.Features.ContentSearch.Models;
using OptiGraphExtensions.Features.ContentSearch.Services.Abstractions;

namespace OptiGraphExtensions.Tests.Features.ContentSearch;

[TestFixture]
public class ContentSearchApiControllerTests
{
    private Mock<IContentSearchService> _mockContentSearchService;
    private ContentSearchApiController _controller;

    [SetUp]
    public void Setup()
    {
        _mockContentSearchService = new Mock<IContentSearchService>();
        _controller = new ContentSearchApiController(_mockContentSearchService.Object);
    }

    [Test]
    public async Task Search_WithValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        var results = new List<ContentSearchResult>
        {
            new() { GuidValue = "guid-1", Name = "Page 1", Url = "/page-1", ContentType = "StandardPage" },
            new() { GuidValue = "guid-2", Name = "Page 2", Url = "/page-2", ContentType = "ArticlePage" }
        };

        _mockContentSearchService
            .Setup(s => s.SearchContentAsync("test", null, null, 10))
            .ReturnsAsync(results);

        // Act
        var result = await _controller.Search("test");

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedResults = okResult.Value as IEnumerable<ContentSearchResult>;
        Assert.That(returnedResults, Is.Not.Null);
        Assert.That(returnedResults!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task Search_WithShortQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("a");

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
        Assert.That(badRequestResult.Value?.ToString(), Does.Contain("2 characters"));
    }

    [Test]
    public async Task Search_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search(null);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("");

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    [Test]
    public async Task Search_WithWhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search("   ");

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    [Test]
    public async Task Search_WithServiceException_Returns500()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.SearchContentAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _controller.Search("test");

        // Assert
        var statusResult = result.Result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
        Assert.That(statusResult.Value?.ToString(), Does.Contain("Service error"));
    }

    [Test]
    public async Task Search_WithContentTypeFilter_PassesFilterToService()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.SearchContentAsync("test", "StandardPage", null, 10))
            .ReturnsAsync(new List<ContentSearchResult>());

        // Act
        await _controller.Search("test", contentType: "StandardPage");

        // Assert
        _mockContentSearchService.Verify(
            s => s.SearchContentAsync("test", "StandardPage", null, 10),
            Times.Once);
    }

    [Test]
    public async Task Search_WithLanguageFilter_PassesFilterToService()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.SearchContentAsync("test", null, "en", 10))
            .ReturnsAsync(new List<ContentSearchResult>());

        // Act
        await _controller.Search("test", language: "en");

        // Assert
        _mockContentSearchService.Verify(
            s => s.SearchContentAsync("test", null, "en", 10),
            Times.Once);
    }

    [Test]
    public async Task Search_WithCustomLimit_PassesLimitToService()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.SearchContentAsync("test", null, null, 5))
            .ReturnsAsync(new List<ContentSearchResult>());

        // Act
        await _controller.Search("test", limit: 5);

        // Assert
        _mockContentSearchService.Verify(
            s => s.SearchContentAsync("test", null, null, 5),
            Times.Once);
    }

    [Test]
    public async Task Search_WithLimitOver10_CapsAt10()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.SearchContentAsync("test", null, null, 10))
            .ReturnsAsync(new List<ContentSearchResult>());

        // Act
        await _controller.Search("test", limit: 50);

        // Assert
        _mockContentSearchService.Verify(
            s => s.SearchContentAsync("test", null, null, 10),
            Times.Once);
    }

    [Test]
    public async Task GetContentTypes_ReturnsOkWithTypes()
    {
        // Arrange
        var contentTypes = new List<string> { "StandardPage", "ArticlePage", "ProductPage" };

        _mockContentSearchService
            .Setup(s => s.GetAvailableContentTypesAsync())
            .ReturnsAsync(contentTypes);

        // Act
        var result = await _controller.GetContentTypes();

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedTypes = okResult.Value as IEnumerable<string>;
        Assert.That(returnedTypes, Is.Not.Null);
        Assert.That(returnedTypes!.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task GetContentTypes_WithServiceException_Returns500()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.GetAvailableContentTypesAsync())
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _controller.GetContentTypes();

        // Assert
        var statusResult = result.Result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetContentTypes_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.GetAvailableContentTypesAsync())
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.GetContentTypes();

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var returnedTypes = okResult.Value as IEnumerable<string>;
        Assert.That(returnedTypes, Is.Not.Null);
        Assert.That(returnedTypes, Is.Empty);
    }

    [Test]
    public async Task Search_WithAllParameters_PassesAllToService()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.SearchContentAsync("search term", "ProductPage", "sv", 8))
            .ReturnsAsync(new List<ContentSearchResult>());

        // Act
        await _controller.Search("search term", contentType: "ProductPage", language: "sv", limit: 8);

        // Assert
        _mockContentSearchService.Verify(
            s => s.SearchContentAsync("search term", "ProductPage", "sv", 8),
            Times.Once);
    }

    [Test]
    public async Task Search_WithTwoCharacterQuery_ReturnsOk()
    {
        // Arrange
        _mockContentSearchService
            .Setup(s => s.SearchContentAsync("ab", null, null, 10))
            .ReturnsAsync(new List<ContentSearchResult>());

        // Act
        var result = await _controller.Search("ab");

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
    }
}
