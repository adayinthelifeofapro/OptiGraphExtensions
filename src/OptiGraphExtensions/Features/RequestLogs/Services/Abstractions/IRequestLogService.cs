using OptiGraphExtensions.Features.RequestLogs.Models;

namespace OptiGraphExtensions.Features.RequestLogs.Services.Abstractions;

public interface IRequestLogService
{
    Task<IEnumerable<RequestLogModel>> GetRequestLogsAsync(RequestLogQueryParameters? queryParameters = null);
}
