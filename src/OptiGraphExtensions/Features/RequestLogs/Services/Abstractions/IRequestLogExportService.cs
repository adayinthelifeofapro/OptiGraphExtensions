using OptiGraphExtensions.Features.RequestLogs.Models;

namespace OptiGraphExtensions.Features.RequestLogs.Services.Abstractions;

public interface IRequestLogExportService
{
    string ExportToCsv(IEnumerable<RequestLogModel> logs);
    string ExportToJson(IEnumerable<RequestLogModel> logs);
}
