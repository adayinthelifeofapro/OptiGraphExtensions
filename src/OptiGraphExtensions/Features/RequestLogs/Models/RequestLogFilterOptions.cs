namespace OptiGraphExtensions.Features.RequestLogs.Models;

public class RequestLogFilterOptions
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Method { get; set; }
    public bool? Success { get; set; }
    public string? SearchText { get; set; }

    public bool HasFilters =>
        StartDate.HasValue ||
        EndDate.HasValue ||
        !string.IsNullOrEmpty(Method) ||
        Success.HasValue ||
        !string.IsNullOrEmpty(SearchText);
}
