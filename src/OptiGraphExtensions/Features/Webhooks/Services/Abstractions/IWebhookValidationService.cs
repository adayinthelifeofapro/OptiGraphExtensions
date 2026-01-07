using OptiGraphExtensions.Features.Webhooks.Models;

namespace OptiGraphExtensions.Features.Webhooks.Services.Abstractions
{
    public interface IWebhookValidationService
    {
        WebhookValidationResult ValidateWebhook(WebhookModel model);
        WebhookValidationResult ValidateCreateRequest(CreateWebhookRequest request);
        WebhookValidationResult ValidateUpdateRequest(UpdateWebhookRequest request);
    }

    public class WebhookValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
