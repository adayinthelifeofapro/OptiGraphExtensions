using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.Webhooks.Models
{
    public class WebhookModel
    {
        public string? Id { get; set; }

        public bool Disabled { get; set; }

        [Required(ErrorMessage = "Webhook URL is required")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(2048, ErrorMessage = "URL must be less than 2048 characters")]
        public string? Url { get; set; }

        [Required(ErrorMessage = "HTTP Method is required")]
        public string Method { get; set; } = "POST";

        public List<string> Topics { get; set; } = new();

        public List<WebhookFilter> Filters { get; set; } = new();
    }

    public class WebhookFilter
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = "eq";
        public string Value { get; set; } = string.Empty;
    }

    public class WebhookRequest
    {
        public string? Url { get; set; }
        public string? Method { get; set; }
    }

    public class WebhookResponse
    {
        public string? Id { get; set; }
        public bool Disabled { get; set; }
        public WebhookRequest? Request { get; set; }
        public List<string>? Topic { get; set; }
        public List<Dictionary<string, Dictionary<string, string>>>? Filters { get; set; }
    }

    public class CreateWebhookRequest
    {
        [Required(ErrorMessage = "Webhook URL is required")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? Url { get; set; }

        [Required(ErrorMessage = "HTTP Method is required")]
        public string Method { get; set; } = "POST";

        public bool Disabled { get; set; }

        public List<string> Topics { get; set; } = new();

        public List<WebhookFilter> Filters { get; set; } = new();
    }

    public class UpdateWebhookRequest
    {
        [Required(ErrorMessage = "Webhook ID is required")]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Webhook URL is required")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? Url { get; set; }

        [Required(ErrorMessage = "HTTP Method is required")]
        public string Method { get; set; } = "POST";

        public bool Disabled { get; set; }

        public List<string> Topics { get; set; } = new();

        public List<WebhookFilter> Filters { get; set; } = new();
    }
}
