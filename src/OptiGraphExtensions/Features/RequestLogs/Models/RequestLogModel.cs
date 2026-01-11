using System.Text.Json.Serialization;

namespace OptiGraphExtensions.Features.RequestLogs.Models;

public class RequestLogModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    public DateTime? CreatedAtParsed => DateTime.TryParse(CreatedAt, out var dt) ? dt : null;

    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("operationType")]
    public string? OperationType { get; set; }

    [JsonPropertyName("operationName")]
    public string? OperationName { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
