using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.RequestLogs.Models;
using OptiGraphExtensions.Features.RequestLogs.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.RequestLogs;

public class RequestLogManagementComponentBase : ManagementComponentBase<RequestLogModel, RequestLogModel>
{
    /// <summary>
    /// Threshold for warning about large result sets that may cause performance issues with client-side filtering.
    /// </summary>
    protected const int LargeResultSetThreshold = 500;

    [Inject]
    protected IRequestLogService RequestLogService { get; set; } = null!;

    [Inject]
    protected IRequestLogExportService ExportService { get; set; } = null!;

    [Inject]
    protected IPaginationService<RequestLogModel> PaginationService { get; set; } = null!;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = null!;

    protected PaginationResult<RequestLogModel>? PaginationResult { get; set; }
    protected IList<RequestLogModel> AllLogs { get; set; } = new List<RequestLogModel>();
    protected IList<RequestLogModel> FilteredLogs { get; set; } = new List<RequestLogModel>();

    /// <summary>
    /// Warning message displayed when the result set is large and may cause performance issues.
    /// </summary>
    protected string? LargeResultSetWarning { get; set; }

    // API Query Parameters (server-side filtering)
    protected string ApiRequestId { get; set; } = string.Empty;
    protected string ApiHost { get; set; } = string.Empty;
    protected string ApiPath { get; set; } = string.Empty;
    protected string ApiSuccess { get; set; } = string.Empty;
    protected int? ApiPage { get; set; }

    // Client-side filter options (applied after fetching)
    protected DateTime? StartDate { get; set; }
    protected DateTime? EndDate { get; set; }
    protected string SelectedMethod { get; set; } = string.Empty;
    protected string SelectedStatus { get; set; } = string.Empty;
    protected string SearchText { get; set; } = string.Empty;

    // Detail view
    protected RequestLogModel? SelectedLog { get; set; }
    protected bool ShowDetails { get; set; }

    // Pagination
    protected int CurrentPage { get; set; } = 1;
    protected int PageSize { get; set; } = 25;
    protected int TotalPages => PaginationResult?.TotalPages ?? 0;
    protected int TotalItems => PaginationResult?.TotalItems ?? 0;
    protected IList<RequestLogModel> Logs => PaginationResult?.Items ?? Array.Empty<RequestLogModel>();

    protected static IEnumerable<string> AvailableHttpMethods => new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD" };

    protected override async Task LoadDataAsync()
    {
        await LoadLogs();
    }

    protected async Task LoadLogs()
    {
        await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
        {
            var queryParameters = BuildQueryParameters();
            AllLogs = (await RequestLogService.GetRequestLogsAsync(queryParameters)).ToList();

            // Check for large result sets and warn about potential performance impact
            if (AllLogs.Count >= LargeResultSetThreshold)
            {
                LargeResultSetWarning = $"Large result set ({AllLogs.Count} items). Consider using API filters (Request ID, Host, Path, Success) to reduce the data set for better performance.";
            }
            else
            {
                LargeResultSetWarning = null;
            }

            ApplyFilters();
        }, "loading request logs");
    }

    private RequestLogQueryParameters? BuildQueryParameters()
    {
        var parameters = new RequestLogQueryParameters();
        var hasParameters = false;

        if (ApiPage.HasValue && ApiPage.Value > 0)
        {
            parameters.Page = ApiPage.Value;
            hasParameters = true;
        }

        if (!string.IsNullOrWhiteSpace(ApiRequestId))
        {
            parameters.RequestId = ApiRequestId.Trim();
            hasParameters = true;
        }

        if (!string.IsNullOrWhiteSpace(ApiHost))
        {
            parameters.Host = ApiHost.Trim();
            hasParameters = true;
        }

        if (!string.IsNullOrWhiteSpace(ApiPath))
        {
            parameters.Path = ApiPath.Trim();
            hasParameters = true;
        }

        if (!string.IsNullOrEmpty(ApiSuccess))
        {
            parameters.Success = ApiSuccess == "true";
            hasParameters = true;
        }

        return hasParameters ? parameters : null;
    }

    protected void ApplyFilters()
    {
        IEnumerable<RequestLogModel> filtered = AllLogs;

        // Date range filter (filter out items without parsed dates first)
        if (StartDate.HasValue || EndDate.HasValue)
        {
            filtered = filtered.Where(l => l.CreatedAtParsed.HasValue);
        }

        if (StartDate.HasValue)
        {
            filtered = filtered.Where(l => l.CreatedAtParsed!.Value >= StartDate.Value.Date);
        }

        if (EndDate.HasValue)
        {
            // Use start of next day for inclusive end date comparison
            var endOfDay = EndDate.Value.Date.AddDays(1);
            filtered = filtered.Where(l => l.CreatedAtParsed!.Value < endOfDay);
        }

        // Method filter
        if (!string.IsNullOrEmpty(SelectedMethod))
        {
            filtered = filtered.Where(l => string.Equals(l.Method, SelectedMethod, StringComparison.OrdinalIgnoreCase));
        }

        // Status filter
        if (!string.IsNullOrEmpty(SelectedStatus))
        {
            var isSuccess = SelectedStatus == "success";
            filtered = filtered.Where(l => l.Success == isSuccess);
        }

        // Search text filter
        if (!string.IsNullOrEmpty(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(l =>
                (l.Path?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (l.OperationName?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (l.Message?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (l.User?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (l.Host?.ToLowerInvariant().Contains(searchLower) ?? false)
            );
        }

        FilteredLogs = filtered.OrderByDescending(l => l.CreatedAtParsed).ToList();
        CurrentPage = 1;
        UpdatePaginatedLogs();
    }

    protected void OnStartDateChanged(ChangeEventArgs e)
    {
        StartDate = DateTime.TryParse(e.Value?.ToString(), out var date) ? date : null;
        ApplyFilters();
    }

    protected void OnEndDateChanged(ChangeEventArgs e)
    {
        EndDate = DateTime.TryParse(e.Value?.ToString(), out var date) ? date : null;
        ApplyFilters();
    }

    protected void OnMethodFilterChanged(ChangeEventArgs e)
    {
        SelectedMethod = e.Value?.ToString() ?? string.Empty;
        ApplyFilters();
    }

    protected void OnStatusFilterChanged(ChangeEventArgs e)
    {
        SelectedStatus = e.Value?.ToString() ?? string.Empty;
        ApplyFilters();
    }

    protected void OnSearchTextChanged(ChangeEventArgs e)
    {
        SearchText = e.Value?.ToString() ?? string.Empty;
        ApplyFilters();
    }

    protected async Task ClearAllFilters()
    {
        // Clear API parameters
        ApiRequestId = string.Empty;
        ApiHost = string.Empty;
        ApiPath = string.Empty;
        ApiSuccess = string.Empty;
        ApiPage = null;

        // Clear client-side filters
        StartDate = null;
        EndDate = null;
        SelectedMethod = string.Empty;
        SelectedStatus = string.Empty;
        SearchText = string.Empty;

        // Reload data without API filters
        await LoadLogs();
    }

    protected void ClearClientFilters()
    {
        StartDate = null;
        EndDate = null;
        SelectedMethod = string.Empty;
        SelectedStatus = string.Empty;
        SearchText = string.Empty;
        ApplyFilters();
    }

    protected bool HasActiveApiFilters =>
        !string.IsNullOrEmpty(ApiRequestId) ||
        !string.IsNullOrEmpty(ApiHost) ||
        !string.IsNullOrEmpty(ApiPath) ||
        !string.IsNullOrEmpty(ApiSuccess) ||
        ApiPage.HasValue;

    protected bool HasActiveClientFilters =>
        StartDate.HasValue ||
        EndDate.HasValue ||
        !string.IsNullOrEmpty(SelectedMethod) ||
        !string.IsNullOrEmpty(SelectedStatus) ||
        !string.IsNullOrEmpty(SearchText);

    protected bool HasActiveFilters => HasActiveApiFilters || HasActiveClientFilters;

    protected void ShowLogDetails(RequestLogModel log)
    {
        SelectedLog = log;
        ShowDetails = true;
        StateHasChanged();
    }

    protected void CloseDetails()
    {
        ShowDetails = false;
        SelectedLog = null;
        StateHasChanged();
    }

    protected async Task ExportToCsv()
    {
        await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
        {
            var csvContent = ExportService.ExportToCsv(FilteredLogs);
            var fileName = $"request-logs-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            await DownloadFile(fileName, csvContent, "text/csv");
        }, "exporting logs to CSV");
    }

    protected async Task ExportToJson()
    {
        await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
        {
            var jsonContent = ExportService.ExportToJson(FilteredLogs);
            var fileName = $"request-logs-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            await DownloadFile(fileName, jsonContent, "application/json");
        }, "exporting logs to JSON");
    }

    private async Task DownloadFile(string fileName, string content, string contentType)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var base64 = Convert.ToBase64String(bytes);
        await JSRuntime.InvokeVoidAsync("downloadFile", fileName, base64, contentType);
    }

    protected void UpdatePaginatedLogs()
    {
        PaginationResult = PaginationService.GetPage(FilteredLogs, CurrentPage, PageSize);
        StateHasChanged();
    }

    protected void GoToPage(int page)
    {
        NavigateToPage(page, CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedLogsAsync);
    }

    protected void GoToPreviousPage()
    {
        NavigateToPreviousPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedLogsAsync);
    }

    protected void GoToNextPage()
    {
        NavigateToNextPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedLogsAsync);
    }

    private Task UpdatePaginatedLogsAsync()
    {
        UpdatePaginatedLogs();
        return Task.CompletedTask;
    }

    protected static string GetStatusBadgeClass(bool success)
    {
        return success ? "epi-status-badge--success" : "epi-status-badge--error";
    }

    protected static string GetStatusText(bool success)
    {
        return success ? "Success" : "Failed";
    }

    protected static string FormatDateTime(string? dateTimeStr)
    {
        if (DateTime.TryParse(dateTimeStr, out var dateTime))
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return dateTimeStr ?? string.Empty;
    }

    protected static string FormatDuration(int durationMs)
    {
        if (durationMs < 1000)
        {
            return $"{durationMs}ms";
        }
        return $"{durationMs / 1000.0:F2}s";
    }
}
