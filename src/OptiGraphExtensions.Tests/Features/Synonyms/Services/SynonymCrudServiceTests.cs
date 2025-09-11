using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Validation;
using OptiGraphExtensions.Features.Common.Exceptions;
using ValidationResult = OptiGraphExtensions.Features.Common.Validation.ValidationResult;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Tests.Features.Synonyms.Services;

[TestFixture]
public class SynonymCrudServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private Mock<IAuthenticationService> _mockAuthenticationService;
    private Mock<IOptiGraphConfigurationService> _mockConfigurationService;
    private Mock<IValidationService<CreateSynonymRequest>> _mockCreateValidationService;
    private Mock<IValidationService<UpdateSynonymRequest>> _mockUpdateValidationService;
    private SynonymCrudService _service;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockAuthenticationService = new Mock<IAuthenticationService>();
        _mockConfigurationService = new Mock<IOptiGraphConfigurationService>();
        _mockCreateValidationService = new Mock<IValidationService<CreateSynonymRequest>>();
        _mockUpdateValidationService = new Mock<IValidationService<UpdateSynonymRequest>>();

        _service = new SynonymCrudService(
            _httpClient,
            _mockAuthenticationService.Object,
            _mockConfigurationService.Object,
            _mockCreateValidationService.Object,
            _mockUpdateValidationService.Object
        );

        _mockConfigurationService.Setup(c => c.GetBaseUrl()).Returns("https://test.com");
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task GetSynonymsAsync_WithAuthenticatedUser_ReturnsOrderedSynonyms()
    {
        // Arrange
        _mockAuthenticationService.Setup(a => a.IsUserAuthenticated()).Returns(true);

        var synonyms = new List<Synonym>
        {
            new() { Id = Guid.NewGuid(), SynonymItem = "zebra" },
            new() { Id = Guid.NewGuid(), SynonymItem = "apple" },
            new() { Id = Guid.NewGuid(), SynonymItem = "banana" }
        };

        var jsonResponse = JsonSerializer.Serialize(synonyms);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.GetSynonymsAsync();

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result[0].SynonymItem, Is.EqualTo("apple")); // Should be ordered
        Assert.That(result[1].SynonymItem, Is.EqualTo("banana"));
        Assert.That(result[2].SynonymItem, Is.EqualTo("zebra"));
    }

    [Test]
    public void GetSynonymsAsync_WithUnauthenticatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockAuthenticationService.Setup(a => a.IsUserAuthenticated()).Returns(false);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetSynonymsAsync());
        Assert.That(ex.Message, Is.EqualTo("User is not authenticated"));
    }

    [Test]
    public async Task CreateSynonymAsync_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new CreateSynonymRequest { Synonym = "test synonym" };
        _mockCreateValidationService
            .Setup(v => v.Validate(request))
            .Returns(ValidationResult.Success());

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.CreateSynonymAsync(request);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CreateSynonymAsync_WithInvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateSynonymRequest { Synonym = "" };
        _mockCreateValidationService
            .Setup(v => v.Validate(request))
            .Returns(ValidationResult.Failure("Synonym is required"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() => _service.CreateSynonymAsync(request));
        Assert.That(ex.ValidationErrors.Count, Is.EqualTo(1));
        Assert.That(ex.ValidationErrors[0], Is.EqualTo("Synonym is required"));
    }

    [Test]
    public void CreateSynonymAsync_WithUnauthorizedResponse_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new CreateSynonymRequest { Synonym = "test synonym" };
        _mockCreateValidationService
            .Setup(v => v.Validate(request))
            .Returns(ValidationResult.Success());

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateSynonymAsync(request));
        Assert.That(ex.Message, Is.EqualTo("You are not authorized to create synonyms"));
    }

    [Test]
    public async Task UpdateSynonymAsync_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var synonymId = Guid.NewGuid();
        var request = new UpdateSynonymRequest { Synonym = "updated synonym" };
        _mockUpdateValidationService
            .Setup(v => v.Validate(request))
            .Returns(ValidationResult.Success());

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.UpdateSynonymAsync(synonymId, request);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void UpdateSynonymAsync_WithNotFoundResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var synonymId = Guid.NewGuid();
        var request = new UpdateSynonymRequest { Synonym = "updated synonym" };
        _mockUpdateValidationService
            .Setup(v => v.Validate(request))
            .Returns(ValidationResult.Success());

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateSynonymAsync(synonymId, request));
        Assert.That(ex.Message, Is.EqualTo("Synonym not found"));
    }

    [Test]
    public async Task DeleteSynonymAsync_WithExistingSynonym_ReturnsTrue()
    {
        // Arrange
        var synonymId = Guid.NewGuid();
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.DeleteSynonymAsync(synonymId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void DeleteSynonymAsync_WithNotFoundResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var synonymId = Guid.NewGuid();
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteSynonymAsync(synonymId));
        Assert.That(ex.Message, Is.EqualTo("Synonym not found"));
    }
}