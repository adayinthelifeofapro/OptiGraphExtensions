namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions
{
    public interface IOptiGraphConfigurationService
    {
        string GetGatewayUrl();
        string GetAppKey();
        string GetSecret();
        string GetBaseUrl();
    }
}