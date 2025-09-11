using OptiGraphExtensions.Features.Common.Services;

namespace OptiGraphExtensions.Tests.Features.Common.Services;

[TestFixture]
public class GraphConfigurationValidatorTests
{
    private GraphConfigurationValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new GraphConfigurationValidator();
    }

    [Test]
    public void ValidateConfiguration_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        const string gatewayUrl = "https://example.com";
        const string hmacKey = "testKey";
        const string hmacSecret = "testSecret";

        // Act & Assert
        Assert.DoesNotThrow(() => _validator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret));
    }

    [Test]
    public void ValidateConfiguration_WithEmptyGatewayUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        const string gatewayUrl = "";
        const string hmacKey = "testKey";
        const string hmacSecret = "testSecret";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            _validator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret));
        Assert.That(ex.Message, Is.EqualTo("Optimizely Graph Gateway URL not configured"));
    }

    [Test]
    public void ValidateConfiguration_WithNullGatewayUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        string gatewayUrl = null!;
        const string hmacKey = "testKey";
        const string hmacSecret = "testSecret";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            _validator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret));
        Assert.That(ex.Message, Is.EqualTo("Optimizely Graph Gateway URL not configured"));
    }

    [Test]
    public void ValidateConfiguration_WithEmptyHmacKey_ThrowsInvalidOperationException()
    {
        // Arrange
        const string gatewayUrl = "https://example.com";
        const string hmacKey = "";
        const string hmacSecret = "testSecret";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            _validator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret));
        Assert.That(ex.Message, Is.EqualTo("Optimizely Graph HMAC credentials not configured"));
    }

    [Test]
    public void ValidateConfiguration_WithEmptyHmacSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        const string gatewayUrl = "https://example.com";
        const string hmacKey = "testKey";
        const string hmacSecret = "";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            _validator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret));
        Assert.That(ex.Message, Is.EqualTo("Optimizely Graph HMAC credentials not configured"));
    }

    [Test]
    public void ValidateConfiguration_WithNullHmacCredentials_ThrowsInvalidOperationException()
    {
        // Arrange
        const string gatewayUrl = "https://example.com";
        string hmacKey = null!;
        string hmacSecret = null!;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            _validator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret));
        Assert.That(ex.Message, Is.EqualTo("Optimizely Graph HMAC credentials not configured"));
    }
}