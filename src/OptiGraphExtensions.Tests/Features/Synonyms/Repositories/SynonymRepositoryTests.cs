using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Repositories;

namespace OptiGraphExtensions.Tests.Features.Synonyms.Repositories;

[TestFixture]
public class SynonymRepositoryTests
{
    private OptiGraphExtensionsDataContext _dataContext;
    private SynonymRepository _repository;
    private List<Synonym> _testSynonyms;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<OptiGraphExtensionsDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<OptiGraphExtensionsDataContext>>();
        _dataContext = new OptiGraphExtensionsDataContext(options, mockLogger.Object);
        _repository = new SynonymRepository(_dataContext);

        _testSynonyms = new List<Synonym>
        {
            new() { Id = Guid.NewGuid(), SynonymItem = "car", CreatedAt = DateTime.UtcNow, CreatedBy = "user1" },
            new() { Id = Guid.NewGuid(), SynonymItem = "vehicle", CreatedAt = DateTime.UtcNow, CreatedBy = "user2" },
            new() { Id = Guid.NewGuid(), SynonymItem = "automobile", CreatedAt = DateTime.UtcNow, CreatedBy = "user1" }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _dataContext.Dispose();
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllSynonyms()
    {
        // Arrange
        await _dataContext.Synonyms.AddRangeAsync(_testSynonyms);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(3));
        var resultList = result.ToList();
        Assert.That(resultList.Select(s => s.SynonymItem), Contains.Item("car"));
        Assert.That(resultList.Select(s => s.SynonymItem), Contains.Item("vehicle"));
        Assert.That(resultList.Select(s => s.SynonymItem), Contains.Item("automobile"));
    }

    [Test]
    public async Task GetByIdAsync_WithExistingId_ReturnsSynonym()
    {
        // Arrange
        var testSynonym = _testSynonyms[0];
        await _dataContext.Synonyms.AddAsync(testSynonym);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(testSynonym.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(testSynonym.Id));
        Assert.That(result.SynonymItem, Is.EqualTo(testSynonym.SynonymItem));
        Assert.That(result.CreatedBy, Is.EqualTo(testSynonym.CreatedBy));
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAsync_WithValidSynonym_AddsSynonymAndReturnsSynonym()
    {
        // Arrange
        var newSynonym = new Synonym
        {
            Id = Guid.NewGuid(),
            SynonymItem = "new synonym",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "testuser"
        };

        // Act
        var result = await _repository.CreateAsync(newSynonym);

        // Assert
        Assert.That(result, Is.EqualTo(newSynonym));

        var createdSynonym = await _dataContext.Synonyms.FindAsync(newSynonym.Id);
        Assert.That(createdSynonym, Is.Not.Null);
        Assert.That(createdSynonym!.SynonymItem, Is.EqualTo("new synonym"));
        Assert.That(createdSynonym.CreatedBy, Is.EqualTo("testuser"));
    }

    [Test]
    public async Task UpdateAsync_WithValidSynonym_UpdatesSynonymAndReturnsSynonym()
    {
        // Arrange
        var testSynonym = _testSynonyms[0];
        await _dataContext.Synonyms.AddAsync(testSynonym);
        await _dataContext.SaveChangesAsync();

        testSynonym.SynonymItem = "updated synonym";

        // Act
        var result = await _repository.UpdateAsync(testSynonym);

        // Assert
        Assert.That(result, Is.EqualTo(testSynonym));
        Assert.That(result.SynonymItem, Is.EqualTo("updated synonym"));

        var updatedSynonym = await _dataContext.Synonyms.FindAsync(testSynonym.Id);
        Assert.That(updatedSynonym!.SynonymItem, Is.EqualTo("updated synonym"));
    }

    [Test]
    public async Task DeleteAsync_WithExistingId_RemovesSynonymAndReturnsTrue()
    {
        // Arrange
        var testSynonym = _testSynonyms[0];
        await _dataContext.Synonyms.AddAsync(testSynonym);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(testSynonym.Id);

        // Assert
        Assert.That(result, Is.True);

        var deletedSynonym = await _dataContext.Synonyms.FindAsync(testSynonym.Id);
        Assert.That(deletedSynonym, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ExistsAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var testSynonym = _testSynonyms[0];
        await _dataContext.Synonyms.AddAsync(testSynonym);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(testSynonym.Id);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(nonExistingId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Constructor_WithNullDataContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SynonymRepository(null!));
    }

    [Test]
    public void CreateAsync_WithNullSynonym_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _repository.CreateAsync(null!));
    }

    [Test]
    public void UpdateAsync_WithNullSynonym_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _repository.UpdateAsync(null!));
    }

    [Test]
    public async Task DeleteAsync_WithEmptyGuid_ReturnsFalse()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var result = await _repository.DeleteAsync(emptyGuid);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ExistsAsync_WithEmptyGuid_ReturnsFalse()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var result = await _repository.ExistsAsync(emptyGuid);

        // Assert
        Assert.That(result, Is.False);
    }
}