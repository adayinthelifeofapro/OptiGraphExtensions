using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.RequestLogs.Models;
using OptiGraphExtensions.Features.RequestLogs.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Tests.Features.RequestLogs.Services;

[TestFixture]
public class RequestLogServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private Mock<IOptiGraphConfigurationService> _mockConfigurationService;
    private Mock<IGraphConfigurationValidator> _mockGraphConfigurationValidator;
    private RequestLogService _service;

    private const string TestGatewayUrl = "https://cg.optimizely.com";
    private const string TestAppKey = "test-app-key";
    private const string TestSecret = "test-secret";

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfigurationService = new Mock<IOptiGraphConfigurationService>();
        _mockGraphConfigurationValidator = new Mock<IGraphConfigurationValidator>();

        _service = new RequestLogService(
            _httpClient,
            _mockConfigurationService.Object,
            _mockGraphConfigurationValidator.Object
        );

        _mockConfigurationService.Setup(c => c.GetGatewayUrl()).Returns(TestGatewayUrl);
        _mockConfigurationService.Setup(c => c.GetAppKey()).Returns(TestAppKey);
        _mockConfigurationService.Setup(c => c.GetSecret()).Returns(TestSecret);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    #region GetRequestLogsAsync - Success Scenarios

    [Test]
    public async Task GetRequestLogsAsync_WithValidResponse_ReturnsLogs()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                CreatedAt = "2025-01-01T10:00:00Z",
                Method = "GET",
                Host = "example.com",
                Path = "/api/content",
                Success = true,
                Duration = 150
            },
            new()
            {
                Id = "log-2",
                CreatedAt = "2025-01-01T10:01:00Z",
                Method = "POST",
                Host = "example.com",
                Path = "/api/query",
                Success = false,
                Duration = 500
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, logs);

        // Act
        var result = await _service.GetRequestLogsAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        var resultList = result.ToList();
        Assert.That(resultList[0].Id, Is.EqualTo("log-1"));
        Assert.That(resultList[0].Method, Is.EqualTo("GET"));
        Assert.That(resultList[0].Success, Is.True);
        Assert.That(resultList[1].Id, Is.EqualTo("log-2"));
        Assert.That(resultList[1].Success, Is.False);
    }

    [Test]
    public async Task GetRequestLogsAsync_WithEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, new List<RequestLogModel>());

        // Act
        var result = await _service.GetRequestLogsAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetRequestLogsAsync_WithNullResponse_ReturnsEmptyList()
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
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
        var result = await _service.GetRequestLogsAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region GetRequestLogsAsync - Query Parameters

    [Test]
    public async Task GetRequestLogsAsync_WithNoParameters_UsesBaseUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        // Act
        await _service.GetRequestLogsAsync();

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Is.EqualTo($"{TestGatewayUrl}/api/logs/request"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithPageParameter_AddsPageToUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters { Page = 5 };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Does.Contain("page=5"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithRequestIdParameter_AddsRequestIdToUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters { RequestId = "abc-123" };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Does.Contain("requestId=abc-123"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithHostParameter_AddsHostToUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters { Host = "example.com" };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Does.Contain("host=example.com"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithPathParameter_AddsEncodedPathToUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters { Path = "/api/content?type=page" };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        // Verify path is URL encoded
        Assert.That(capturedRequest!.RequestUri!.ToString(), Does.Contain("path="));
        Assert.That(capturedRequest.RequestUri.ToString(), Does.Contain("%2Fapi%2Fcontent"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithSuccessTrueParameter_AddsSuccessToUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters { Success = true };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Does.Contain("success=true"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithSuccessFalseParameter_AddsSuccessFalseToUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters { Success = false };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Does.Contain("success=false"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithMultipleParameters_AddsAllToUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters
        {
            Page = 2,
            Host = "example.com",
            Success = true
        };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        var url = capturedRequest!.RequestUri!.ToString();
        Assert.That(url, Does.Contain("page=2"));
        Assert.That(url, Does.Contain("host=example.com"));
        Assert.That(url, Does.Contain("success=true"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithEmptyParameters_UsesBaseUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters(); // All null/empty

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Is.EqualTo($"{TestGatewayUrl}/api/logs/request"));
    }

    #endregion

    #region GetRequestLogsAsync - Authentication

    [Test]
    public async Task GetRequestLogsAsync_SetsCorrectAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        // Act
        await _service.GetRequestLogsAsync();

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Headers.Authorization, Is.Not.Null);
        Assert.That(capturedRequest.Headers.Authorization!.Scheme, Is.EqualTo("Basic"));
    }

    [Test]
    public async Task GetRequestLogsAsync_ValidatesConfiguration()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, new List<RequestLogModel>());

        // Act
        await _service.GetRequestLogsAsync();

        // Assert
        _mockGraphConfigurationValidator.Verify(
            v => v.ValidateConfiguration(TestGatewayUrl, TestAppKey, TestSecret),
            Times.Once);
    }

    #endregion

    #region GetRequestLogsAsync - Error Handling

    [Test]
    public void GetRequestLogsAsync_WithInvalidConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockGraphConfigurationValidator
            .Setup(v => v.ValidateConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Graph configuration not set"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetRequestLogsAsync());
    }

    [Test]
    public void GetRequestLogsAsync_WithApiError_ThrowsGraphSyncException()
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act & Assert
        var ex = Assert.ThrowsAsync<GraphSyncException>(() => _service.GetRequestLogsAsync());
        Assert.That(ex.Message, Does.Contain("Error fetching request logs"));
        Assert.That(ex.Message, Does.Contain("InternalServerError"));
    }

    [Test]
    public void GetRequestLogsAsync_WithUnauthorizedResponse_ThrowsGraphSyncException()
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act & Assert
        var ex = Assert.ThrowsAsync<GraphSyncException>(() => _service.GetRequestLogsAsync());
        Assert.That(ex.Message, Does.Contain("Unauthorized"));
    }

    [Test]
    public void GetRequestLogsAsync_WithForbiddenResponse_ThrowsGraphSyncException()
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("Forbidden")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act & Assert
        var ex = Assert.ThrowsAsync<GraphSyncException>(() => _service.GetRequestLogsAsync());
        Assert.That(ex.Message, Does.Contain("Forbidden"));
    }

    [Test]
    public void GetRequestLogsAsync_WithNotFoundResponse_ThrowsGraphSyncException()
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not Found")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponseMessage);

        // Act & Assert
        var ex = Assert.ThrowsAsync<GraphSyncException>(() => _service.GetRequestLogsAsync());
        Assert.That(ex.Message, Does.Contain("NotFound"));
    }

    #endregion

    #region GetRequestLogsAsync - URL Building Edge Cases

    [Test]
    public async Task GetRequestLogsAsync_WithTrailingSlashInGatewayUrl_BuildsCorrectUrl()
    {
        // Arrange
        _mockConfigurationService.Setup(c => c.GetGatewayUrl()).Returns("https://cg.optimizely.com/");

        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        // Act
        await _service.GetRequestLogsAsync();

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Is.EqualTo("https://cg.optimizely.com/api/logs/request"));
    }

    [Test]
    public async Task GetRequestLogsAsync_WithSpecialCharactersInRequestId_EncodesCorrectly()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponseWithCapture(new List<RequestLogModel>(), req => capturedRequest = req);

        var parameters = new RequestLogQueryParameters { RequestId = "req+id&value" };

        // Act
        await _service.GetRequestLogsAsync(parameters);

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        // Verify special characters are URL encoded
        Assert.That(capturedRequest!.RequestUri!.ToString(), Does.Contain("requestId="));
        Assert.That(capturedRequest.RequestUri.ToString(), Does.Not.Contain("&value"));
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T response)
    {
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var httpResponseMessage = new HttpResponseMessage(statusCode)
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
    }

    private void SetupHttpResponseWithCapture<T>(T response, Action<HttpRequestMessage> captureAction)
    {
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => captureAction(req))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse)
            });
    }

    #endregion
}
