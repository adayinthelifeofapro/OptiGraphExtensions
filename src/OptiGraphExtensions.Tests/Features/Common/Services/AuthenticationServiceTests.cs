using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using System.Security.Principal;
using OptiGraphExtensions.Features.Common.Services;

namespace OptiGraphExtensions.Tests.Features.Common.Services;

[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private AuthenticationService _authenticationService;

    [SetUp]
    public void Setup()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _authenticationService = new AuthenticationService(_mockHttpContextAccessor.Object);
    }

    [Test]
    public void IsUserAuthenticated_WithAuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        var mockIdentity = new Mock<IIdentity>();
        mockIdentity.Setup(i => i.IsAuthenticated).Returns(true);
        
        var mockPrincipal = new Mock<ClaimsPrincipal>();
        mockPrincipal.Setup(p => p.Identity).Returns(mockIdentity.Object);
        
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(mockPrincipal.Object);
        
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        // Act
        var result = _authenticationService.IsUserAuthenticated();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsUserAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var mockIdentity = new Mock<IIdentity>();
        mockIdentity.Setup(i => i.IsAuthenticated).Returns(false);
        
        var mockPrincipal = new Mock<ClaimsPrincipal>();
        mockPrincipal.Setup(p => p.Identity).Returns(mockIdentity.Object);
        
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(mockPrincipal.Object);
        
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        // Act
        var result = _authenticationService.IsUserAuthenticated();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsUserAuthenticated_WithNullHttpContext_ReturnsFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null!);

        // Act
        var result = _authenticationService.IsUserAuthenticated();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsUserAuthenticated_WithNullUser_ReturnsFalse()
    {
        // Arrange
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns((ClaimsPrincipal)null!);
        
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        // Act
        var result = _authenticationService.IsUserAuthenticated();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsUserAuthenticated_WithNullIdentity_ReturnsFalse()
    {
        // Arrange
        var mockPrincipal = new Mock<ClaimsPrincipal>();
        mockPrincipal.Setup(p => p.Identity).Returns((IIdentity)null!);
        
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(mockPrincipal.Object);
        
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        // Act
        var result = _authenticationService.IsUserAuthenticated();

        // Assert
        Assert.That(result, Is.False);
    }
}