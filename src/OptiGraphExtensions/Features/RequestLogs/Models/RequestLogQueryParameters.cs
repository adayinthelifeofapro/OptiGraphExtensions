namespace OptiGraphExtensions.Features.RequestLogs.Models;

/// <summary>
/// Query parameters for the Optimizely Graph Request Logs API.
/// These are passed as URL query parameters for server-side filtering.
/// </summary>
public class RequestLogQueryParameters
{
    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// Filter by specific request ID.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Filter by host name.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Filter by request path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Filter by success status (true/false).
    /// </summary>
    public bool? Success { get; set; }

    public bool HasParameters =>
        Page.HasValue ||
        !string.IsNullOrEmpty(RequestId) ||
        !string.IsNullOrEmpty(Host) ||
        !string.IsNullOrEmpty(Path) ||
        Success.HasValue;
}
