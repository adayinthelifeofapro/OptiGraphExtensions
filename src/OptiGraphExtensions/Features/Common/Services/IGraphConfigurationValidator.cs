namespace OptiGraphExtensions.Features.Common.Services;

public interface IGraphConfigurationValidator
{
    void ValidateConfiguration(string gatewayUrl, string hmacKey, string hmacSecret);
}