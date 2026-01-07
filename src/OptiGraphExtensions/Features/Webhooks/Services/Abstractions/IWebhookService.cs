using OptiGraphExtensions.Features.Webhooks.Models;

namespace OptiGraphExtensions.Features.Webhooks.Services.Abstractions
{
    public interface IWebhookService
    {
        Task<IEnumerable<WebhookModel>> GetAllWebhooksAsync();
        Task<WebhookModel> CreateWebhookAsync(CreateWebhookRequest request);
        Task<WebhookModel> UpdateWebhookAsync(UpdateWebhookRequest request);
        Task<bool> DeleteWebhookAsync(string id);
    }
}
