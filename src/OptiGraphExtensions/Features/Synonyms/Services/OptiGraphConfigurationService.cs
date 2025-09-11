using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class OptiGraphConfigurationService : IOptiGraphConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OptiGraphConfigurationService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetGatewayUrl()
        {
            return _configuration["Optimizely:ContentGraph:GatewayAddress"] ?? string.Empty;
        }

        public string GetAppKey()
        {
            return _configuration["Optimizely:ContentGraph:AppKey"] ?? string.Empty;
        }

        public string GetSecret()
        {
            return _configuration["Optimizely:ContentGraph:Secret"] ?? string.Empty;
        }

        public string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return string.Empty;

            return $"{request.Scheme}://{request.Host}";
        }
    }
}