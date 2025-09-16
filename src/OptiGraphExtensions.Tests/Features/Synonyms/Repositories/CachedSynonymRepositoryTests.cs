using Moq;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Caching;
using OptiGraphExtensions.Features.Synonyms.Repositories;

namespace OptiGraphExtensions.Tests.Features.Synonyms.Repositories;

[TestFixture]
public class CachedSynonymRepositoryTests
{
    private Mock<ISynonymRepository> _mockRepository;
    private Mock<ICacheService> _mockCacheService;
    private CachedSynonymRepository _cachedRepository;
    private List<Synonym> _testSynonyms;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ISynonymRepository>();
        _mockCacheService = new Mock<ICacheService>();
        _cachedRepository = new CachedSynonymRepository(_mockRepository.Object, _mockCacheService.Object);

        _testSynonyms = new List<Synonym>
        {
            new() { Id = Guid.NewGuid(), SynonymItem = "car", CreatedAt = DateTime.UtcNow, CreatedBy = "user1" },
            new() { Id = Guid.NewGuid(), SynonymItem = "vehicle", CreatedAt = DateTime.UtcNow, CreatedBy = "user2" },
            new() { Id = Guid.NewGuid(), SynonymItem = "automobile", CreatedAt = DateTime.UtcNow, CreatedBy = "user1" }
        };
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new CachedSynonymRepository(_mockRepository.Object, _mockCacheService.Object));
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public async Task GetAllAsync_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var expectedCacheKey = CacheKeyBuilder.BuildEntityListKey<Synonym>();
        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<Synonym>>(expectedCacheKey))
                        .ReturnsAsync(_testSynonyms);

        // Act
        var result = await _cachedRepository.GetAllAsync();

        // Assert
        Assert.That(result, Is.EqualTo(_testSynonyms));
        _mockCacheService.Verify(x => x.GetAsync<IEnumerable<Synonym>>(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.GetAllAsync(), Times.Never);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task GetAllAsync_WhenCacheMiss_FetchesFromRepositoryAndCaches()
    {
        // Arrange
        var expectedCacheKey = CacheKeyBuilder.BuildEntityListKey<Synonym>();
        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<Synonym>>(expectedCacheKey))
                        .ReturnsAsync((IEnumerable<Synonym>?)null);
        _mockRepository.Setup(x => x.GetAllAsync())
                      .ReturnsAsync(_testSynonyms);

        // Act
        var result = await _cachedRepository.GetAllAsync();

        // Assert
        Assert.That(result, Is.EqualTo(_testSynonyms));
        _mockCacheService.Verify(x => x.GetAsync<IEnumerable<Synonym>>(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, It.IsAny<IEnumerable<Synonym>>(), _cacheExpiration), Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public async Task GetByIdAsync_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var testSynonym = _testSynonyms[0];
        var expectedCacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(testSynonym.Id);
        _mockCacheService.Setup(x => x.GetAsync<Synonym>(expectedCacheKey))
                        .ReturnsAsync(testSynonym);

        // Act
        var result = await _cachedRepository.GetByIdAsync(testSynonym.Id);

        // Assert
        Assert.That(result, Is.EqualTo(testSynonym));
        _mockCacheService.Verify(x => x.GetAsync<Synonym>(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_WhenCacheMissAndEntityExists_FetchesFromRepositoryAndCaches()
    {
        // Arrange
        var testSynonym = _testSynonyms[0];
        var expectedCacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(testSynonym.Id);
        _mockCacheService.Setup(x => x.GetAsync<Synonym>(expectedCacheKey))
                        .ReturnsAsync((Synonym?)null);
        _mockRepository.Setup(x => x.GetByIdAsync(testSynonym.Id))
                      .ReturnsAsync(testSynonym);

        // Act
        var result = await _cachedRepository.GetByIdAsync(testSynonym.Id);

        // Assert
        Assert.That(result, Is.EqualTo(testSynonym));
        _mockCacheService.Verify(x => x.GetAsync<Synonym>(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.GetByIdAsync(testSynonym.Id), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, testSynonym, _cacheExpiration), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_WhenCacheMissAndEntityNotExists_DoesNotCache()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var expectedCacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(testId);
        _mockCacheService.Setup(x => x.GetAsync<Synonym>(expectedCacheKey))
                        .ReturnsAsync((Synonym?)null);
        _mockRepository.Setup(x => x.GetByIdAsync(testId))
                      .ReturnsAsync((Synonym?)null);

        // Act
        var result = await _cachedRepository.GetByIdAsync(testId);

        // Assert
        Assert.That(result, Is.Null);
        _mockCacheService.Verify(x => x.GetAsync<Synonym>(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.GetByIdAsync(testId), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    #endregion

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_WithValidSynonym_CreatesAndInvalidatesCacheAndCachesNewEntity()
    {
        // Arrange
        var newSynonym = new Synonym
        {
            Id = Guid.NewGuid(),
            SynonymItem = "new synonym",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "testuser"
        };
        var expectedCacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(newSynonym.Id);
        var expectedPattern = CacheKeyBuilder.BuildEntityPattern<Synonym>();

        _mockRepository.Setup(x => x.CreateAsync(newSynonym))
                      .ReturnsAsync(newSynonym);

        // Act
        var result = await _cachedRepository.CreateAsync(newSynonym);

        // Assert
        Assert.That(result, Is.EqualTo(newSynonym));
        _mockRepository.Verify(x => x.CreateAsync(newSynonym), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(expectedPattern), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, newSynonym, _cacheExpiration), Times.Once);
    }


    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_WithValidSynonym_UpdatesAndInvalidatesCacheAndCachesUpdatedEntity()
    {
        // Arrange
        var synonymToUpdate = _testSynonyms[0];
        synonymToUpdate.SynonymItem = "updated synonym";
        var expectedCacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(synonymToUpdate.Id);
        var expectedPattern = CacheKeyBuilder.BuildEntityPattern<Synonym>();

        _mockRepository.Setup(x => x.UpdateAsync(synonymToUpdate))
                      .ReturnsAsync(synonymToUpdate);

        // Act
        var result = await _cachedRepository.UpdateAsync(synonymToUpdate);

        // Assert
        Assert.That(result, Is.EqualTo(synonymToUpdate));
        _mockRepository.Verify(x => x.UpdateAsync(synonymToUpdate), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(expectedPattern), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, synonymToUpdate, _cacheExpiration), Times.Once);
    }


    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_WhenDeleteSucceeds_InvalidatesCacheAndRemovesSpecificEntity()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var expectedCacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(testId);
        var expectedPattern = CacheKeyBuilder.BuildEntityPattern<Synonym>();

        _mockRepository.Setup(x => x.DeleteAsync(testId))
                      .ReturnsAsync(true);

        // Act
        var result = await _cachedRepository.DeleteAsync(testId);

        // Assert
        Assert.That(result, Is.True);
        _mockRepository.Verify(x => x.DeleteAsync(testId), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(expectedPattern), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(expectedCacheKey), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenDeleteFails_DoesNotInvalidateCache()
    {
        // Arrange
        var testId = Guid.NewGuid();

        _mockRepository.Setup(x => x.DeleteAsync(testId))
                      .ReturnsAsync(false);

        // Act
        var result = await _cachedRepository.DeleteAsync(testId);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x => x.DeleteAsync(testId), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_WithEmptyGuid_CallsRepositoryAndReturnsResult()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        _mockRepository.Setup(x => x.DeleteAsync(emptyGuid))
                      .ReturnsAsync(false);

        // Act
        var result = await _cachedRepository.DeleteAsync(emptyGuid);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x => x.DeleteAsync(emptyGuid), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ExistsAsync Tests

    [Test]
    public async Task ExistsAsync_WhenCacheHit_ReturnsCachedValue()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var expectedCacheKey = CacheKeyBuilder.BuildExistsKey<Synonym>(testId);

        _mockCacheService.Setup(x => x.Exists(expectedCacheKey))
                        .Returns(true);
        _mockCacheService.Setup(x => x.GetAsync<bool>(expectedCacheKey))
                        .ReturnsAsync(true);

        // Act
        var result = await _cachedRepository.ExistsAsync(testId);

        // Assert
        Assert.That(result, Is.True);
        _mockCacheService.Verify(x => x.Exists(expectedCacheKey), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<bool>(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.ExistsAsync(It.IsAny<Guid>()), Times.Never);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task ExistsAsync_WhenCacheMiss_FetchesFromRepositoryAndCaches()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var expectedCacheKey = CacheKeyBuilder.BuildExistsKey<Synonym>(testId);

        _mockCacheService.Setup(x => x.Exists(expectedCacheKey))
                        .Returns(false);
        _mockRepository.Setup(x => x.ExistsAsync(testId))
                      .ReturnsAsync(true);

        // Act
        var result = await _cachedRepository.ExistsAsync(testId);

        // Assert
        Assert.That(result, Is.True);
        _mockCacheService.Verify(x => x.Exists(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.ExistsAsync(testId), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, true, _cacheExpiration), Times.Once);
    }

    [Test]
    public async Task ExistsAsync_WhenCacheMissAndEntityNotExists_FetchesFromRepositoryAndCachesFalse()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var expectedCacheKey = CacheKeyBuilder.BuildExistsKey<Synonym>(testId);

        _mockCacheService.Setup(x => x.Exists(expectedCacheKey))
                        .Returns(false);
        _mockRepository.Setup(x => x.ExistsAsync(testId))
                      .ReturnsAsync(false);

        // Act
        var result = await _cachedRepository.ExistsAsync(testId);

        // Assert
        Assert.That(result, Is.False);
        _mockCacheService.Verify(x => x.Exists(expectedCacheKey), Times.Once);
        _mockRepository.Verify(x => x.ExistsAsync(testId), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, false, _cacheExpiration), Times.Once);
    }

    [Test]
    public async Task ExistsAsync_WithEmptyGuid_CallsRepositoryAndReturnsResult()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        var expectedCacheKey = CacheKeyBuilder.BuildExistsKey<Synonym>(emptyGuid);

        _mockCacheService.Setup(x => x.Exists(expectedCacheKey))
                        .Returns(false);
        _mockRepository.Setup(x => x.ExistsAsync(emptyGuid))
                      .ReturnsAsync(false);

        // Act
        var result = await _cachedRepository.ExistsAsync(emptyGuid);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(x => x.ExistsAsync(emptyGuid), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(expectedCacheKey, false, _cacheExpiration), Times.Once);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Test]
    public void CreateAsync_RepositoryThrowsException_DoesNotCacheAndRethrows()
    {
        // Arrange
        var newSynonym = new Synonym
        {
            Id = Guid.NewGuid(),
            SynonymItem = "test",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user"
        };

        _mockRepository.Setup(x => x.CreateAsync(newSynonym))
                      .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _cachedRepository.CreateAsync(newSynonym));

        Assert.That(exception!.Message, Is.EqualTo("Database error"));
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public void UpdateAsync_RepositoryThrowsException_DoesNotCacheAndRethrows()
    {
        // Arrange
        var synonymToUpdate = _testSynonyms[0];

        _mockRepository.Setup(x => x.UpdateAsync(synonymToUpdate))
                      .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _cachedRepository.UpdateAsync(synonymToUpdate));

        Assert.That(exception!.Message, Is.EqualTo("Database error"));
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public void GetAllAsync_CacheServiceThrowsException_FallsBackToRepository()
    {
        // Arrange
        var expectedCacheKey = CacheKeyBuilder.BuildEntityListKey<Synonym>();
        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<Synonym>>(expectedCacheKey))
                        .ThrowsAsync(new InvalidOperationException("Cache error"));
        _mockRepository.Setup(x => x.GetAllAsync())
                      .ReturnsAsync(_testSynonyms);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _cachedRepository.GetAllAsync());

        Assert.That(exception!.Message, Is.EqualTo("Cache error"));
    }

    #endregion
}