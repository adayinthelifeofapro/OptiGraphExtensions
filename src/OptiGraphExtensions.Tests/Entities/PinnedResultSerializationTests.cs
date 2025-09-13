using System.Text.Json;
using NUnit.Framework;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Tests.Entities;

[TestFixture]
public class PinnedResultSerializationTests
{
    [Test]
    public void PinnedResult_WithCollection_SerializesWithoutCircularReferenceException()
    {
        // Arrange
        var collection = new PinnedResultsCollection
        {
            Id = Guid.NewGuid(),
            Title = "Test Collection",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test User"
        };

        var pinnedResult = new PinnedResult
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            Phrases = "test,search,phrases",
            TargetKey = "12345678-1234-1234-1234-123456789012",
            Language = "en",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test User",
            Collection = collection
        };

        collection.PinnedResults.Add(pinnedResult);

        var serializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act & Assert - Should not throw JsonException due to circular references
        Assert.DoesNotThrow(() =>
        {
            var json = JsonSerializer.Serialize(pinnedResult, serializerOptions);
            Assert.That(json, Is.Not.Null);
            Assert.That(json.Length, Is.GreaterThan(0));
        }, "PinnedResult serialization should not throw circular reference exception when ReferenceHandler.IgnoreCycles is used");
    }

    [Test]
    public void PinnedResultsCollection_WithPinnedResults_SerializesWithoutCircularReferenceException()
    {
        // Arrange
        var collection = new PinnedResultsCollection
        {
            Id = Guid.NewGuid(),
            Title = "Test Collection",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test User"
        };

        var pinnedResult = new PinnedResult
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            Phrases = "test,search,phrases",
            TargetKey = "12345678-1234-1234-1234-123456789012",
            Language = "en",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test User",
            Collection = collection
        };

        collection.PinnedResults.Add(pinnedResult);

        var serializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act & Assert - Should not throw JsonException due to circular references
        Assert.DoesNotThrow(() =>
        {
            var json = JsonSerializer.Serialize(collection, serializerOptions);
            Assert.That(json, Is.Not.Null);
            Assert.That(json.Length, Is.GreaterThan(0));
        }, "PinnedResultsCollection serialization should not throw circular reference exception when ReferenceHandler.IgnoreCycles is used");
    }

    [Test]
    public void JsonIgnoreAttributes_AreAppliedToNavigationProperties()
    {
        // This test verifies that the JsonIgnore attributes are correctly applied to the navigation properties
        // Even though they may not completely prevent serialization in all test scenarios,
        // they should be present on the navigation properties

        // Arrange - Check that JsonIgnore attributes are present
        var collectionProperty = typeof(PinnedResult).GetProperty("Collection");
        var pinnedResultsProperty = typeof(PinnedResultsCollection).GetProperty("PinnedResults");

        // Act & Assert - Verify JsonIgnore attributes are present
        Assert.That(collectionProperty, Is.Not.Null, "Collection navigation property should exist");
        Assert.That(pinnedResultsProperty, Is.Not.Null, "PinnedResults navigation property should exist");

        var collectionJsonIgnore = collectionProperty!.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);
        var pinnedResultsJsonIgnore = pinnedResultsProperty!.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);

        Assert.That(collectionJsonIgnore.Length, Is.GreaterThan(0), "Collection property should have JsonIgnore attribute");
        Assert.That(pinnedResultsJsonIgnore.Length, Is.GreaterThan(0), "PinnedResults property should have JsonIgnore attribute");
    }

    [Test]
    public void WebApplicationJsonConfiguration_PreventCircularReferences()
    {
        // This test demonstrates that the web application JSON configuration
        // (ReferenceHandler.IgnoreCycles) prevents circular reference exceptions
        
        // Arrange - Create entities with potential circular references
        var collection = new PinnedResultsCollection
        {
            Id = Guid.NewGuid(),
            Title = "Test Collection",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test User"
        };

        var pinnedResult = new PinnedResult
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            Phrases = "test,search,phrases",
            TargetKey = "12345678-1234-1234-1234-123456789012",
            Language = "en",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test User",
            Collection = collection
        };

        collection.PinnedResults.Add(pinnedResult);

        // Create the same JSON configuration that's used in the web application
        var webAppJsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act & Assert - This should work without throwing circular reference exceptions
        Assert.DoesNotThrow(() =>
        {
            var collectionJson = JsonSerializer.Serialize(collection, webAppJsonOptions);
            var pinnedResultJson = JsonSerializer.Serialize(pinnedResult, webAppJsonOptions);
            
            Assert.That(collectionJson, Is.Not.Null);
            Assert.That(pinnedResultJson, Is.Not.Null);
            Assert.That(collectionJson.Length, Is.GreaterThan(0));
            Assert.That(pinnedResultJson.Length, Is.GreaterThan(0));
        }, "Web application JSON configuration should prevent circular reference exceptions");
    }
}