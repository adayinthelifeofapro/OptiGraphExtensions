using Microsoft.AspNetCore.Components;

using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Webhooks.Models;
using OptiGraphExtensions.Features.Webhooks.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Webhooks
{
    public class WebhookManagementComponentBase : ManagementComponentBase<WebhookModel, WebhookModel>
    {
        [Inject]
        protected IWebhookService WebhookService { get; set; } = null!;

        [Inject]
        protected IWebhookValidationService ValidationService { get; set; } = null!;

        [Inject]
        protected IPaginationService<WebhookModel> PaginationService { get; set; } = null!;

        protected PaginationResult<WebhookModel>? PaginationResult { get; set; }
        protected IList<WebhookModel> AllWebhooks { get; set; } = new List<WebhookModel>();
        protected IList<WebhookModel> FilteredWebhooks { get; set; } = new List<WebhookModel>();
        protected CreateWebhookRequest NewWebhook { get; set; } = new();
        protected UpdateWebhookRequest EditingWebhook { get; set; } = new();
        protected bool IsCreating { get; set; }
        protected bool IsEditing { get; set; }
        protected bool ShowTopicHelp { get; set; }
        protected bool ShowFilterHelp { get; set; }
        protected bool ShowEditTopicHelp { get; set; }
        protected bool ShowEditFilterHelp { get; set; }

        protected string SelectedStatusFilter { get; set; } = string.Empty;

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<WebhookModel> Webhooks => PaginationResult?.Items?.ToList() ?? new List<WebhookModel>();

        protected static IEnumerable<string> AvailableHttpMethods => new[] { "POST", "GET", "PUT", "PATCH", "DELETE" };

        protected static IEnumerable<string> AvailableTopics => new[]
        {
            "doc.created",
            "doc.updated",
            "doc.deleted",
            "doc.*",
            "bulk.created",
            "bulk.updated",
            "bulk.deleted",
            "bulk.completed",
            "bulk.*",
            "*.created",
            "*.updated",
            "*.deleted",
            "*.*"
        };

        protected override async Task LoadDataAsync()
        {
            await LoadWebhooks();
        }

        protected async Task LoadWebhooks()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                AllWebhooks = (await WebhookService.GetAllWebhooksAsync()).ToList();
                ApplyFilters();
            }, "loading webhooks");
        }

        protected void ApplyFilters()
        {
            IEnumerable<WebhookModel> filtered = AllWebhooks;

            if (!string.IsNullOrEmpty(SelectedStatusFilter))
            {
                var isDisabled = SelectedStatusFilter == "disabled";
                filtered = filtered.Where(w => w.Disabled == isDisabled);
            }

            FilteredWebhooks = filtered.ToList();
            CurrentPage = 1;
            UpdatePaginatedWebhooks();
        }

        protected void OnStatusFilterChanged(ChangeEventArgs e)
        {
            SelectedStatusFilter = e.Value?.ToString() ?? string.Empty;
            ApplyFilters();
        }

        protected bool HasActiveFilters => !string.IsNullOrEmpty(SelectedStatusFilter);

        protected void ShowCreateForm()
        {
            IsCreating = true;
            NewWebhook = new CreateWebhookRequest
            {
                Method = "POST",
                Disabled = false,
                Topics = new List<string>(),
                Filters = new List<WebhookFilter>()
            };
            ClearMessages();
        }

        protected void CancelCreate()
        {
            IsCreating = false;
            NewWebhook = new CreateWebhookRequest();
            ClearMessages();
        }

        protected async Task CreateWebhook()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                var validationResult = ValidationService.ValidateCreateRequest(NewWebhook);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException(validationResult.ErrorMessage);
                }

                await WebhookService.CreateWebhookAsync(NewWebhook);
                NewWebhook = new CreateWebhookRequest();
                IsCreating = false;
                SetSuccessMessage("Webhook created successfully.");
                await LoadWebhooks();
            }, "creating webhook", showLoading: false);
        }

        protected async Task DeleteWebhook(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SetErrorMessage("Webhook ID is required for deletion.");
                return;
            }

            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await WebhookService.DeleteWebhookAsync(id);
                SetSuccessMessage("Webhook deleted successfully.");
                await LoadWebhooks();
            }, "deleting webhook", showLoading: false);
        }

        protected void StartEdit(WebhookModel webhook)
        {
            IsEditing = true;
            IsCreating = false;

            // Convert *. to empty list for UI display (means "all events")
            var topics = webhook.Topics.ToList();
            if (topics.Count == 1 && topics[0] == "*.*")
            {
                topics = new List<string>();
            }

            EditingWebhook = new UpdateWebhookRequest
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Method = webhook.Method,
                Disabled = webhook.Disabled,
                Topics = topics,
                Filters = webhook.Filters.Select(f => new WebhookFilter
                {
                    Field = f.Field,
                    Operator = f.Operator,
                    Value = f.Value
                }).ToList()
            };
            ShowEditTopicHelp = false;
            ShowEditFilterHelp = false;
            ClearMessages();
        }

        protected void CancelEdit()
        {
            IsEditing = false;
            EditingWebhook = new UpdateWebhookRequest();
            ShowEditTopicHelp = false;
            ShowEditFilterHelp = false;
            ClearMessages();
        }

        protected async Task UpdateWebhook()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                var validationResult = ValidationService.ValidateUpdateRequest(EditingWebhook);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException(validationResult.ErrorMessage);
                }

                await WebhookService.UpdateWebhookAsync(EditingWebhook);
                EditingWebhook = new UpdateWebhookRequest();
                IsEditing = false;
                SetSuccessMessage("Webhook updated successfully.");
                await LoadWebhooks();
            }, "updating webhook", showLoading: false);
        }

        protected void AddEditTopic(string topic)
        {
            if (!string.IsNullOrEmpty(topic) && !EditingWebhook.Topics.Contains(topic))
            {
                EditingWebhook.Topics.Add(topic);
                StateHasChanged();
            }
        }

        protected void RemoveEditTopic(string topic)
        {
            EditingWebhook.Topics.Remove(topic);
            StateHasChanged();
        }

        protected void AddEditFilter()
        {
            EditingWebhook.Filters.Add(new WebhookFilter
            {
                Field = "Status",
                Operator = "eq",
                Value = "Published"
            });
            StateHasChanged();
        }

        protected void RemoveEditFilter(WebhookFilter filter)
        {
            EditingWebhook.Filters.Remove(filter);
            StateHasChanged();
        }

        protected void ToggleEditTopicHelp()
        {
            ShowEditTopicHelp = !ShowEditTopicHelp;
            StateHasChanged();
        }

        protected void ToggleEditFilterHelp()
        {
            ShowEditFilterHelp = !ShowEditFilterHelp;
            StateHasChanged();
        }

        protected async Task<bool> ConfirmDelete()
        {
            return await Task.FromResult(true);
        }

        protected void UpdatePaginatedWebhooks()
        {
            PaginationResult = PaginationService.GetPage(FilteredWebhooks, CurrentPage, PageSize);
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            NavigateToPage(page, CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedWebhooksAsync);
        }

        protected void GoToPreviousPage()
        {
            NavigateToPreviousPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedWebhooksAsync);
        }

        protected void GoToNextPage()
        {
            NavigateToNextPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedWebhooksAsync);
        }

        private Task UpdatePaginatedWebhooksAsync()
        {
            UpdatePaginatedWebhooks();
            return Task.CompletedTask;
        }

        protected void AddTopic(string topic)
        {
            if (!string.IsNullOrEmpty(topic) && !NewWebhook.Topics.Contains(topic))
            {
                NewWebhook.Topics.Add(topic);
                StateHasChanged();
            }
        }

        protected void RemoveTopic(string topic)
        {
            NewWebhook.Topics.Remove(topic);
            StateHasChanged();
        }

        protected void AddFilter()
        {
            NewWebhook.Filters.Add(new WebhookFilter
            {
                Field = "status",
                Operator = "eq",
                Value = "Published"
            });
            StateHasChanged();
        }

        protected void RemoveFilter(WebhookFilter filter)
        {
            NewWebhook.Filters.Remove(filter);
            StateHasChanged();
        }

        protected static string GetStatusBadgeClass(bool disabled)
        {
            return disabled ? "epi-status-badge--disabled" : "epi-status-badge--active";
        }

        protected static string GetStatusText(bool disabled)
        {
            return disabled ? "Disabled" : "Active";
        }

        protected static string FormatTopics(List<string> topics)
        {
            if (topics == null || !topics.Any())
            {
                return "All events";
            }
            // Display *. as "All events" since it matches everything
            if (topics.Count == 1 && topics[0] == "*.*")
            {
                return "All events";
            }
            return string.Join(", ", topics);
        }

        protected static string FormatFilters(List<WebhookFilter> filters)
        {
            if (filters == null || !filters.Any())
            {
                return "No filters";
            }
            return string.Join("; ", filters.Select(f => $"{f.Field} {f.Operator} {f.Value}"));
        }

        protected void ToggleTopicHelp()
        {
            ShowTopicHelp = !ShowTopicHelp;
            StateHasChanged();
        }

        protected void ToggleFilterHelp()
        {
            ShowFilterHelp = !ShowFilterHelp;
            StateHasChanged();
        }

        protected static string GetTopicDescription(string topic)
        {
            return topic switch
            {
                "doc.created" => "Triggered when a single content item is created or indexed for the first time",
                "doc.updated" => "Triggered when a single content item is modified and re-indexed",
                "doc.deleted" => "Triggered when a single content item is removed from the index",
                "doc.*" => "Matches all single document events (created, updated, deleted)",
                "bulk.created" => "Triggered when multiple content items are created in a batch operation",
                "bulk.updated" => "Triggered when multiple content items are updated in a batch operation",
                "bulk.deleted" => "Triggered when multiple content items are deleted in a batch operation",
                "bulk.completed" => "Triggered when a bulk synchronization operation completes",
                "bulk.*" => "Matches all bulk/batch operation events",
                "*.created" => "Matches all creation events (both single and bulk)",
                "*.updated" => "Matches all update events (both single and bulk)",
                "*.deleted" => "Matches all deletion events (both single and bulk)",
                "*.*" => "Matches all events - equivalent to subscribing to everything",
                _ => $"Custom topic pattern: {topic}"
            };
        }
    }
}
