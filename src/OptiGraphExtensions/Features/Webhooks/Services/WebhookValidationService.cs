using OptiGraphExtensions.Features.Webhooks.Models;
using OptiGraphExtensions.Features.Webhooks.Services.Abstractions;

namespace OptiGraphExtensions.Features.Webhooks.Services
{
    public class WebhookValidationService : IWebhookValidationService
    {
        private static readonly string[] ValidHttpMethods = { "GET", "POST", "PUT", "PATCH", "DELETE" };

        public WebhookValidationResult ValidateWebhook(WebhookModel model)
        {
            if (model == null)
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Webhook model is required"
                };
            }

            return ValidateCommonFields(model.Url, model.Method);
        }

        public WebhookValidationResult ValidateCreateRequest(CreateWebhookRequest request)
        {
            if (request == null)
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Create request is required"
                };
            }

            return ValidateCommonFields(request.Url, request.Method);
        }

        public WebhookValidationResult ValidateUpdateRequest(UpdateWebhookRequest request)
        {
            if (request == null)
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Update request is required"
                };
            }

            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Webhook ID is required"
                };
            }

            return ValidateCommonFields(request.Url, request.Method);
        }

        private static WebhookValidationResult ValidateCommonFields(string? url, string? method)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Webhook URL is required"
                };
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Please enter a valid HTTP or HTTPS URL"
                };
            }

            if (url.Length > 2048)
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "URL must be less than 2048 characters"
                };
            }

            if (string.IsNullOrWhiteSpace(method))
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "HTTP method is required"
                };
            }

            if (!ValidHttpMethods.Contains(method.ToUpperInvariant()))
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"HTTP method must be one of: {string.Join(", ", ValidHttpMethods)}"
                };
            }

            return new WebhookValidationResult { IsValid = true };
        }
    }
}
