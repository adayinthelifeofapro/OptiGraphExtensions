using System.Text.Json;
using OptiGraphExtensions.Features.RequestLogs.Models;
using OptiGraphExtensions.Features.RequestLogs.Services;

namespace OptiGraphExtensions.Tests.Features.RequestLogs.Services;

[TestFixture]
public class RequestLogExportServiceTests
{
    private RequestLogExportService _service;

    [SetUp]
    public void Setup()
    {
        _service = new RequestLogExportService();
    }

    #region ExportToCsv - Basic Functionality

    [Test]
    public void ExportToCsv_WithEmptyList_ReturnsHeaderOnly()
    {
        // Arrange
        var logs = new List<RequestLogModel>();

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(1));
        Assert.That(lines[0], Is.EqualTo("Id,CreatedAt,InstanceId,Status,Method,Host,Path,OperationType,OperationName,Message,Duration,User,Success"));
    }

    [Test]
    public void ExportToCsv_WithSingleLog_ReturnsHeaderAndDataRow()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                CreatedAt = "2025-01-01T10:00:00Z",
                InstanceId = "instance-1",
                Status = "200",
                Method = "GET",
                Host = "example.com",
                Path = "/api/content",
                OperationType = "query",
                OperationName = "GetContent",
                Message = "Success",
                Duration = 150,
                User = "admin",
                Success = true
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(2));
        Assert.That(lines[1], Does.Contain("log-1"));
        Assert.That(lines[1], Does.Contain("GET"));
        Assert.That(lines[1], Does.Contain("example.com"));
        Assert.That(lines[1], Does.Contain("True"));
    }

    [Test]
    public void ExportToCsv_WithMultipleLogs_ReturnsAllRows()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new() { Id = "log-1", Method = "GET", Success = true, Duration = 100 },
            new() { Id = "log-2", Method = "POST", Success = false, Duration = 200 },
            new() { Id = "log-3", Method = "PUT", Success = true, Duration = 300 }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(4)); // 1 header + 3 data rows
    }

    #endregion

    #region ExportToCsv - CSV Escaping

    [Test]
    public void ExportToCsv_WithCommaInField_EscapesWithQuotes()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Message = "Error, please try again",
                Duration = 100,
                Success = false
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        Assert.That(result, Does.Contain("\"Error, please try again\""));
    }

    [Test]
    public void ExportToCsv_WithQuotesInField_EscapesWithDoubleQuotes()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Message = "Error: \"Invalid input\"",
                Duration = 100,
                Success = false
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        // Double quotes should be escaped as ""
        Assert.That(result, Does.Contain("\"Error: \"\"Invalid input\"\"\""));
    }

    [Test]
    public void ExportToCsv_WithNewlineInField_EscapesWithQuotes()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Message = "Line 1\nLine 2",
                Duration = 100,
                Success = false
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        Assert.That(result, Does.Contain("\"Line 1\nLine 2\""));
    }

    [Test]
    public void ExportToCsv_WithCarriageReturnInField_EscapesWithQuotes()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Message = "Line 1\rLine 2",
                Duration = 100,
                Success = false
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        Assert.That(result, Does.Contain("\"Line 1\rLine 2\""));
    }

    [Test]
    public void ExportToCsv_WithCommaAndQuotesInField_EscapesBothCorrectly()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Message = "Error: \"Invalid, malformed input\"",
                Duration = 100,
                Success = false
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        // Should be wrapped in quotes with internal quotes escaped
        Assert.That(result, Does.Contain("\"Error: \"\"Invalid, malformed input\"\"\""));
    }

    #endregion

    #region ExportToCsv - Null Handling

    [Test]
    public void ExportToCsv_WithNullFields_HandlesGracefully()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = null,
                CreatedAt = null,
                InstanceId = null,
                Status = null,
                Method = null,
                Host = null,
                Path = null,
                OperationType = null,
                OperationName = null,
                Message = null,
                Duration = 0,
                User = null,
                Success = false
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        Assert.That(result, Is.Not.Null);
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(2));
        // Should have empty fields for null values
        Assert.That(lines[1], Does.Contain(",,"));
    }

    [Test]
    public void ExportToCsv_WithEmptyStringFields_HandlesGracefully()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "",
                Message = "",
                Duration = 0,
                Success = true
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        Assert.That(result, Is.Not.Null);
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(2));
    }

    #endregion

    #region ExportToCsv - Field Order

    [Test]
    public void ExportToCsv_HasCorrectHeaderOrder()
    {
        // Arrange
        var logs = new List<RequestLogModel>();

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        var expectedHeader = "Id,CreatedAt,InstanceId,Status,Method,Host,Path,OperationType,OperationName,Message,Duration,User,Success";
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines[0], Is.EqualTo(expectedHeader));
    }

    [Test]
    public void ExportToCsv_HasCorrectFieldOrder()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "ID123",
                CreatedAt = "2025-01-01",
                InstanceId = "INST456",
                Status = "200",
                Method = "POST",
                Host = "myhost.com",
                Path = "/mypath",
                OperationType = "mutation",
                OperationName = "CreateItem",
                Message = "Created successfully",
                Duration = 500,
                User = "testuser",
                Success = true
            }
        };

        // Act
        var result = _service.ExportToCsv(logs);

        // Assert
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var dataLine = lines[1];
        var fields = ParseCsvLine(dataLine);

        Assert.That(fields[0], Is.EqualTo("ID123"));
        Assert.That(fields[1], Is.EqualTo("2025-01-01"));
        Assert.That(fields[2], Is.EqualTo("INST456"));
        Assert.That(fields[3], Is.EqualTo("200"));
        Assert.That(fields[4], Is.EqualTo("POST"));
        Assert.That(fields[5], Is.EqualTo("myhost.com"));
        Assert.That(fields[6], Is.EqualTo("/mypath"));
        Assert.That(fields[7], Is.EqualTo("mutation"));
        Assert.That(fields[8], Is.EqualTo("CreateItem"));
        Assert.That(fields[9], Is.EqualTo("Created successfully"));
        Assert.That(fields[10], Is.EqualTo("500"));
        Assert.That(fields[11], Is.EqualTo("testuser"));
        Assert.That(fields[12], Is.EqualTo("True"));
    }

    #endregion

    #region ExportToJson - Basic Functionality

    [Test]
    public void ExportToJson_WithEmptyList_ReturnsEmptyJsonArray()
    {
        // Arrange
        var logs = new List<RequestLogModel>();

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        Assert.That(result.Trim(), Is.EqualTo("[]"));
    }

    [Test]
    public void ExportToJson_WithSingleLog_ReturnsValidJson()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Method = "GET",
                Host = "example.com",
                Success = true,
                Duration = 150
            }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
    }

    [Test]
    public void ExportToJson_WithMultipleLogs_ReturnsValidJsonArray()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new() { Id = "log-1", Duration = 100 },
            new() { Id = "log-2", Duration = 200 },
            new() { Id = "log-3", Duration = 300 }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(3));
    }

    #endregion

    #region ExportToJson - Formatting

    [Test]
    public void ExportToJson_IsIndentedForReadability()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new() { Id = "log-1", Duration = 100 }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        // Indented JSON should contain newlines
        Assert.That(result, Does.Contain(Environment.NewLine).Or.Contain("\n"));
    }

    [Test]
    public void ExportToJson_UsesCamelCasePropertyNames()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                CreatedAt = "2025-01-01",
                InstanceId = "instance-1",
                OperationType = "query",
                OperationName = "GetContent",
                Duration = 100
            }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        Assert.That(result, Does.Contain("\"id\""));
        Assert.That(result, Does.Contain("\"createdAt\""));
        Assert.That(result, Does.Contain("\"instanceId\""));
        Assert.That(result, Does.Contain("\"operationType\""));
        Assert.That(result, Does.Contain("\"operationName\""));
        Assert.That(result, Does.Contain("\"duration\""));

        // Should not contain PascalCase
        Assert.That(result, Does.Not.Contain("\"Id\""));
        Assert.That(result, Does.Not.Contain("\"CreatedAt\""));
        Assert.That(result, Does.Not.Contain("\"InstanceId\""));
    }

    #endregion

    #region ExportToJson - Special Characters

    [Test]
    public void ExportToJson_WithSpecialCharactersInMessage_EscapesCorrectly()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Message = "Error with \"quotes\" and\nnewlines",
                Duration = 100
            }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        // Should be valid JSON - parsing will throw if escaping is wrong
        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
    }

    [Test]
    public void ExportToJson_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "log-1",
                Message = "Unicode: \u00e9\u00e8\u00ea \u4e2d\u6587",
                Duration = 100
            }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
        var doc = JsonDocument.Parse(result);
        var message = doc.RootElement[0].GetProperty("message").GetString();
        Assert.That(message, Does.Contain("\u00e9"));
    }

    #endregion

    #region ExportToJson - Null Handling

    [Test]
    public void ExportToJson_WithNullFields_HandlesGracefully()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = null,
                Message = null,
                Duration = 0,
                Success = false
            }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
    }

    #endregion

    #region ExportToJson - Data Integrity

    [Test]
    public void ExportToJson_PreservesAllFields()
    {
        // Arrange
        var logs = new List<RequestLogModel>
        {
            new()
            {
                Id = "test-id",
                CreatedAt = "2025-01-15T10:30:00Z",
                InstanceId = "inst-123",
                Status = "200",
                Method = "POST",
                Host = "api.example.com",
                Path = "/v1/content",
                OperationType = "mutation",
                OperationName = "CreateContent",
                Message = "Content created successfully",
                Duration = 250,
                User = "admin@example.com",
                Success = true
            }
        };

        // Act
        var result = _service.ExportToJson(logs);

        // Assert
        var doc = JsonDocument.Parse(result);
        var item = doc.RootElement[0];

        Assert.That(item.GetProperty("id").GetString(), Is.EqualTo("test-id"));
        Assert.That(item.GetProperty("createdAt").GetString(), Is.EqualTo("2025-01-15T10:30:00Z"));
        Assert.That(item.GetProperty("instanceId").GetString(), Is.EqualTo("inst-123"));
        Assert.That(item.GetProperty("status").GetString(), Is.EqualTo("200"));
        Assert.That(item.GetProperty("method").GetString(), Is.EqualTo("POST"));
        Assert.That(item.GetProperty("host").GetString(), Is.EqualTo("api.example.com"));
        Assert.That(item.GetProperty("path").GetString(), Is.EqualTo("/v1/content"));
        Assert.That(item.GetProperty("operationType").GetString(), Is.EqualTo("mutation"));
        Assert.That(item.GetProperty("operationName").GetString(), Is.EqualTo("CreateContent"));
        Assert.That(item.GetProperty("message").GetString(), Is.EqualTo("Content created successfully"));
        Assert.That(item.GetProperty("duration").GetInt32(), Is.EqualTo(250));
        Assert.That(item.GetProperty("user").GetString(), Is.EqualTo("admin@example.com"));
        Assert.That(item.GetProperty("success").GetBoolean(), Is.True);
    }

    #endregion

    #region Helper Methods

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < line.Length)
        {
            if (inQuotes)
            {
                if (line[i] == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    currentField.Append(line[i]);
                    i++;
                }
            }
            else
            {
                if (line[i] == '"')
                {
                    inQuotes = true;
                    i++;
                }
                else if (line[i] == ',')
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                    i++;
                }
                else
                {
                    currentField.Append(line[i]);
                    i++;
                }
            }
        }

        fields.Add(currentField.ToString());
        return fields;
    }

    #endregion
}
