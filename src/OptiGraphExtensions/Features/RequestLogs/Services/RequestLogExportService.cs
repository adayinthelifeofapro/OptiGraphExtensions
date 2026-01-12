using System.Text;
using System.Text.Json;

using OptiGraphExtensions.Features.RequestLogs.Models;
using OptiGraphExtensions.Features.RequestLogs.Services.Abstractions;

namespace OptiGraphExtensions.Features.RequestLogs.Services;

public class RequestLogExportService : IRequestLogExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ExportToCsv(IEnumerable<RequestLogModel> logs)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Id,CreatedAt,InstanceId,Status,Method,Host,Path,OperationType,OperationName,Message,Duration,User,Success");

        // Data rows
        foreach (var log in logs)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvField(log.Id),
                EscapeCsvField(log.CreatedAt),
                EscapeCsvField(log.InstanceId),
                EscapeCsvField(log.Status),
                EscapeCsvField(log.Method),
                EscapeCsvField(log.Host),
                EscapeCsvField(log.Path),
                EscapeCsvField(log.OperationType),
                EscapeCsvField(log.OperationName),
                EscapeCsvField(log.Message),
                log.Duration,
                EscapeCsvField(log.User),
                log.Success
            ));
        }

        return sb.ToString();
    }

    public string ExportToJson(IEnumerable<RequestLogModel> logs)
    {
        return JsonSerializer.Serialize(logs, JsonOptions);
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // If the field contains a comma, quote, or newline, wrap it in quotes and escape any quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
