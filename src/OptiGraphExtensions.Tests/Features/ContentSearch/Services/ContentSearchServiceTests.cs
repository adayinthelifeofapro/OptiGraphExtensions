using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.ContentSearch.Models;
using OptiGraphExtensions.Features.ContentSearch.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Tests.Features.ContentSearch.Services;

[TestFixture]
public class ContentSearchServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private Mock<IOptiGraphConfigurationService> _mockConfigurationService;
    private Mock<IGraphConfigurationValidator> _mockGraphConfigurationValidator;
    private ContentSearchService _service;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfigurationService = new Mock<IOptiGraphConfigurationService>();
        _mockGraphConfigurationValidator = new Mock<IGraphConfigurationValidator>();

        _service = new ContentSearchService(
            _httpClient,
            _mockConfigurationService.Object,
            _mockGraphConfigurationValidator.Object
        );

        _mockConfigurationService.Setup(c => c.GetGatewayUrl()).Returns("https://cg.optimizely.com");
        _mockConfigurationService.Setup(c => c.GetAppKey()).Returns("test-app-key");
        _mockConfigurationService.Setup(c => c.GetSecret()).Returns("test-secret");
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task SearchContentAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = new ContentSearchResultSet
                {
                    Items = new List<ContentSearchItem>
                    {
                        new()
                        {
                            ContentLink = new ContentLinkInfo { GuidValue = "test-guid-1" },
                            Name = "Test Page 1",
                            RelativePath = "/test-page-1",
                            ContentType = new List<string> { "StandardPage" }
                        },
                        new()
                        {
                            ContentLink = new ContentLinkInfo { GuidValue = "test-guid-2" },
                            Name = "Test Page 2",
                            RelativePath = "/test-page-2",
                            ContentType = new List<string> { "ArticlePage" }
                        }
                    },
                    Total = 2
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, graphResponse);

        // Act
        var result = await _service.SearchContentAsync("test", null, null, 10);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].GuidValue, Is.EqualTo("test-guid-1"));
        Assert.That(result[0].Name, Is.EqualTo("Test Page 1"));
        Assert.That(result[0].Url, Is.EqualTo("/test-page-1"));
        Assert.That(result[0].ContentType, Is.EqualTo("StandardPage"));
    }

    [Test]
    public async Task SearchContentAsync_WithShortQuery_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchContentAsync("a");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchContentAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchContentAsync("");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchContentAsync_WithNullQuery_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchContentAsync(null!);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchContentAsync_WithWhitespaceQuery_ReturnsEmptyList()
    {
        // Act
        var result = await _service.SearchContentAsync("   ");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SearchContentAsync_WithInvalidConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockGraphConfigurationValidator
            .Setup(v => v.ValidateConfiguration(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Graph configuration not set"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SearchContentAsync("test"));
    }

    [Test]
    public void SearchContentAsync_WithGraphApiError_ThrowsInvalidOperationException()
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
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SearchContentAsync("test"));
        Assert.That(ex.Message, Does.Contain("Graph search failed"));
    }

    [Test]
    public async Task SearchContentAsync_WithContentTypeFilter_IncludesFilterInQuery()
    {
        // Arrange
        string? capturedRequestContent = null;

        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = new ContentSearchResultSet
                {
                    Items = new List<ContentSearchItem>(),
                    Total = 0
                }
            }
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                capturedRequestContent = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(graphResponse))
            });

        // Act
        await _service.SearchContentAsync("test", "StandardPage");

        // Assert
        Assert.That(capturedRequestContent, Is.Not.Null);
        Assert.That(capturedRequestContent, Does.Contain("StandardPage"));
    }

    [Test]
    public async Task SearchContentAsync_WithLimitExceeding10_CapsAt10()
    {
        // Arrange
        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = new ContentSearchResultSet
                {
                    Items = new List<ContentSearchItem>(),
                    Total = 0
                }
            }
        };

        string? capturedRequestContent = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                capturedRequestContent = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(graphResponse))
            });

        // Act
        await _service.SearchContentAsync("test", null, null, 50);

        // Assert
        Assert.That(capturedRequestContent, Is.Not.Null);
        // The limit should be capped at 10
        Assert.That(capturedRequestContent, Does.Contain("\"limit\":10"));
    }

    [Test]
    public async Task SearchContentAsync_WithGraphQLErrors_ThrowsInvalidOperationException()
    {
        // Arrange
        var graphResponse = new ContentSearchGraphResponse
        {
            Errors = new List<GraphQLError>
            {
                new() { Message = "GraphQL error occurred" }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, graphResponse);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SearchContentAsync("test"));
        Assert.That(ex.Message, Does.Contain("GraphQL errors"));
    }

    [Test]
    public async Task SearchContentAsync_WithNullContentInResponse_ReturnsEmptyList()
    {
        // Arrange
        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = null
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, graphResponse);

        // Act
        var result = await _service.SearchContentAsync("test");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchContentAsync_WithNullItemsInResponse_ReturnsEmptyList()
    {
        // Arrange
        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = new ContentSearchResultSet
                {
                    Items = null,
                    Total = 0
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, graphResponse);

        // Act
        var result = await _service.SearchContentAsync("test");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchContentAsync_FiltersOutItemsWithNullGuidValue()
    {
        // Arrange
        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = new ContentSearchResultSet
                {
                    Items = new List<ContentSearchItem>
                    {
                        new()
                        {
                            ContentLink = new ContentLinkInfo { GuidValue = "valid-guid" },
                            Name = "Valid Page"
                        },
                        new()
                        {
                            ContentLink = new ContentLinkInfo { GuidValue = null },
                            Name = "Invalid Page"
                        },
                        new()
                        {
                            ContentLink = null,
                            Name = "No ContentLink Page"
                        }
                    },
                    Total = 3
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, graphResponse);

        // Act
        var result = await _service.SearchContentAsync("test");

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].GuidValue, Is.EqualTo("valid-guid"));
    }

    [Test]
    public async Task GetAvailableContentTypesAsync_ReturnsContentTypes()
    {
        // Arrange
        var facetsResponse = new ContentTypeFacetsResponse
        {
            Data = new ContentTypeFacetsData
            {
                Content = new ContentTypeFacetsContent
                {
                    Facets = new ContentTypeFacets
                    {
                        ContentType = new List<ContentTypeFacetItem>
                        {
                            new() { Name = "StandardPage", Count = 10 },
                            new() { Name = "ArticlePage", Count = 5 },
                            new() { Name = "ProductPage", Count = 3 }
                        }
                    }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, facetsResponse);

        // Act
        var result = await _service.GetAvailableContentTypesAsync();

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result, Does.Contain("ArticlePage"));
        Assert.That(result, Does.Contain("ProductPage"));
        Assert.That(result, Does.Contain("StandardPage"));
    }

    [Test]
    public async Task GetAvailableContentTypesAsync_WithApiError_ReturnsEmptyList()
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

        // Act
        var result = await _service.GetAvailableContentTypesAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchContentAsync_SetsCorrectAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;

        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = new ContentSearchResultSet { Items = new List<ContentSearchItem>(), Total = 0 }
            }
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(graphResponse))
            });

        // Act
        await _service.SearchContentAsync("test");

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Headers.Authorization, Is.Not.Null);
        Assert.That(capturedRequest.Headers.Authorization!.Scheme, Is.EqualTo("Basic"));
    }

    [Test]
    public async Task SearchContentAsync_UsesCorrectEndpoint()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;

        var graphResponse = new ContentSearchGraphResponse
        {
            Data = new ContentSearchData
            {
                Content = new ContentSearchResultSet { Items = new List<ContentSearchItem>(), Total = 0 }
            }
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(graphResponse))
            });

        // Act
        await _service.SearchContentAsync("test");

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.RequestUri!.ToString(), Is.EqualTo("https://cg.optimizely.com/content/v2"));
    }

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T response)
    {
        var jsonResponse = JsonSerializer.Serialize(response);
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
}
