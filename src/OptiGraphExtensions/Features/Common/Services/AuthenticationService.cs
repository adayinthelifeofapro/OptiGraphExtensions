using Microsoft.AspNetCore.Http;

namespace OptiGraphExtensions.Features.Common.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsUserAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }
}