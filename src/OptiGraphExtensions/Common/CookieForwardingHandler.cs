using Microsoft.AspNetCore.Http;

namespace OptiGraphExtensions.Common;

/// <summary>
/// DelegatingHandler that forwards authentication cookies from the current HttpContext
/// to outgoing HTTP requests. This is necessary when Blazor Server components make
/// HTTP calls to local API endpoints that require authentication.
/// </summary>
public class CookieForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Forward all cookies from the current request to the outgoing request
            var cookieHeader = httpContext.Request.Headers["Cookie"].ToString();
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            }

            // Also forward the anti-forgery token if present (for write operations)
            var antiForgeryToken = httpContext.Request.Headers["RequestVerificationToken"].ToString();
            if (!string.IsNullOrEmpty(antiForgeryToken))
            {
                request.Headers.TryAddWithoutValidation("RequestVerificationToken", antiForgeryToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
