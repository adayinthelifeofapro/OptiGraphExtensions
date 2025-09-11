namespace OptiGraphExtensions.Features.Common.Services;

public class GraphConfigurationValidator : IGraphConfigurationValidator
{
    public void ValidateConfiguration(string gatewayUrl, string hmacKey, string hmacSecret)
    {
        if (string.IsNullOrEmpty(gatewayUrl))
            throw new InvalidOperationException("Optimizely Graph Gateway URL not configured");
        if (string.IsNullOrEmpty(hmacKey) || string.IsNullOrEmpty(hmacSecret))
            throw new InvalidOperationException("Optimizely Graph HMAC credentials not configured");
    }
}