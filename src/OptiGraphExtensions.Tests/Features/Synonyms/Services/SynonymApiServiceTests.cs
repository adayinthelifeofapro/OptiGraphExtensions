using Moq;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Tests.Features.Synonyms.Services;

[TestFixture]
public class SynonymApiServiceTests
{
    private Mock<ISynonymCrudService> _mockSynonymCrudService;
    private Mock<ISynonymGraphSyncService> _mockGraphSyncService;
    private SynonymApiService _service;

    [SetUp]
    public void Setup()
    {
        _mockSynonymCrudService = new Mock<ISynonymCrudService>();
        _mockGraphSyncService = new Mock<ISynonymGraphSyncService>();
        _service = new SynonymApiService(_mockSynonymCrudService.Object, _mockGraphSyncService.Object);
    }

    [Test]
    public async Task GetSynonymsAsync_DelegatesToCrudService()
    {
        // Arrange
        var expectedSynonyms = new List<Synonym>
        {
            new() { Id = Guid.NewGuid(), SynonymItem = "test" }
        };
        _mockSynonymCrudService.Setup(s => s.GetSynonymsAsync()).ReturnsAsync(expectedSynonyms);

        // Act
        var result = await _service.GetSynonymsAsync();

        // Assert
        Assert.That(result, Is.EqualTo(expectedSynonyms));
        _mockSynonymCrudService.Verify(s => s.GetSynonymsAsync(), Times.Once);
    }

    [Test]
    public async Task CreateSynonymAsync_DelegatesToCrudService()
    {
        // Arrange
        var request = new CreateSynonymRequest { Synonym = "test" };
        _mockSynonymCrudService.Setup(s => s.CreateSynonymAsync(request)).ReturnsAsync(true);

        // Act
        var result = await _service.CreateSynonymAsync(request);

        // Assert
        Assert.That(result, Is.True);
        _mockSynonymCrudService.Verify(s => s.CreateSynonymAsync(request), Times.Once);
    }

    [Test]
    public async Task UpdateSynonymAsync_DelegatesToCrudService()
    {
        // Arrange
        var synonymId = Guid.NewGuid();
        var request = new UpdateSynonymRequest { Synonym = "updated test" };
        _mockSynonymCrudService.Setup(s => s.UpdateSynonymAsync(synonymId, request)).ReturnsAsync(true);

        // Act
        var result = await _service.UpdateSynonymAsync(synonymId, request);

        // Assert
        Assert.That(result, Is.True);
        _mockSynonymCrudService.Verify(s => s.UpdateSynonymAsync(synonymId, request), Times.Once);
    }

    [Test]
    public async Task DeleteSynonymAsync_DelegatesToCrudService()
    {
        // Arrange
        var synonymId = Guid.NewGuid();
        _mockSynonymCrudService.Setup(s => s.DeleteSynonymAsync(synonymId)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteSynonymAsync(synonymId);

        // Assert
        Assert.That(result, Is.True);
        _mockSynonymCrudService.Verify(s => s.DeleteSynonymAsync(synonymId), Times.Once);
    }

    [Test]
    public async Task SyncSynonymsToOptimizelyGraphAsync_DelegatesToGraphSyncService()
    {
        // Arrange
        _mockGraphSyncService.Setup(s => s.SyncSynonymsToOptimizelyGraphAsync()).ReturnsAsync(true);

        // Act
        var result = await _service.SyncSynonymsToOptimizelyGraphAsync();

        // Assert
        Assert.That(result, Is.True);
        _mockGraphSyncService.Verify(s => s.SyncSynonymsToOptimizelyGraphAsync(), Times.Once);
    }
}