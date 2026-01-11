using System.Text.Json;
using System.Text.RegularExpressions;
using OptiGraphExtensions.Features.QueryLibrary.Models;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;

namespace OptiGraphExtensions.Features.QueryLibrary.Services
{
    /// <summary>
    /// Service for handling raw GraphQL queries.
    /// </summary>
    public class RawQueryService : IRawQueryService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        // Regex patterns for query analysis
        private static readonly Regex CursorVariablePattern = new(
            @"\$cursor\s*:\s*String",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LimitVariablePattern = new(
            @"\$limit\s*:\s*Int",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CursorFieldPattern = new(
            @"\bcursor\b(?!\s*:)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TotalFieldPattern = new(
            @"\btotal\b(?!\s*:)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex QueryOperationPattern = new(
            @"^\s*(query|mutation|subscription)\s+(\w+)?\s*(\([^)]*\))?\s*{",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public RawQueryService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public QueryValidationResult ValidateQuery(string rawQuery)
        {
            var result = new QueryValidationResult
            {
                IsValid = true,
                Messages = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(rawQuery))
            {
                result.IsValid = false;
                result.Messages.Add("Query cannot be empty");
                return result;
            }

            // Basic syntax check - ensure it looks like a GraphQL query
            if (!rawQuery.Contains("{") || !rawQuery.Contains("}"))
            {
                result.IsValid = false;
                result.Messages.Add("Query must contain braces { }");
                return result;
            }

            // Check for balanced braces
            var openBraces = rawQuery.Count(c => c == '{');
            var closeBraces = rawQuery.Count(c => c == '}');
            if (openBraces != closeBraces)
            {
                result.IsValid = false;
                result.Messages.Add("Unbalanced braces in query");
                return result;
            }

            // Check for pagination support
            result.SupportsPagination = SupportsPagination(rawQuery);

            if (!result.SupportsPagination)
            {
                result.Messages.Add(
                    "Warning: Query may not support pagination. " +
                    "For full export, include $limit and $cursor variables, " +
                    "and request 'cursor' and 'total' fields in response.");
            }

            return result;
        }

        public Dictionary<string, object>? ParseVariables(string? variablesJson)
        {
            if (string.IsNullOrWhiteSpace(variablesJson))
            {
                return null;
            }

            try
            {
                var variables = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    variablesJson, _jsonOptions);

                if (variables == null)
                {
                    return null;
                }

                // Convert JsonElement values to appropriate types
                var result = new Dictionary<string, object>();
                foreach (var (key, value) in variables)
                {
                    result[key] = ConvertJsonElement(value);
                }

                return result;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public bool SupportsPagination(string rawQuery)
        {
            // Check if query has $limit and $cursor variables
            var hasLimitVar = LimitVariablePattern.IsMatch(rawQuery);
            var hasCursorVar = CursorVariablePattern.IsMatch(rawQuery);

            // Check if query requests cursor and total in response
            var hasCursorField = CursorFieldPattern.IsMatch(rawQuery);
            var hasTotalField = TotalFieldPattern.IsMatch(rawQuery);

            return hasLimitVar && hasCursorVar && hasCursorField && hasTotalField;
        }

        public (string query, Dictionary<string, object> variables) InjectPaginationSupport(
            string rawQuery,
            Dictionary<string, object>? existingVariables,
            int pageSize,
            string? cursor)
        {
            var variables = existingVariables ?? new Dictionary<string, object>();
            var modifiedQuery = rawQuery;

            // Add limit to variables
            variables["limit"] = pageSize;

            // Add cursor to variables if provided
            if (!string.IsNullOrEmpty(cursor))
            {
                variables["cursor"] = cursor;
            }

            // If query already supports pagination, just return with updated variables
            if (SupportsPagination(rawQuery))
            {
                return (modifiedQuery, variables);
            }

            // If query has a named operation with parameters, try to inject $limit and $cursor
            var match = QueryOperationPattern.Match(rawQuery);
            if (match.Success)
            {
                var existingParams = match.Groups[3].Value;
                var newParams = BuildPaginationParams(existingParams);

                // Replace the operation signature
                var operationType = match.Groups[1].Value;
                var operationName = match.Groups[2].Success ? match.Groups[2].Value : "Query";

                var oldSignature = match.Value;
                var newSignature = $"{operationType} {operationName}{newParams} {{";

                modifiedQuery = rawQuery.Replace(oldSignature, newSignature);
            }

            return (modifiedQuery, variables);
        }

        private string BuildPaginationParams(string existingParams)
        {
            var hasExistingParams = !string.IsNullOrEmpty(existingParams) &&
                                    existingParams != "()" &&
                                    existingParams.Length > 2;

            var paramList = new List<string>();

            if (hasExistingParams)
            {
                // Extract existing params without parentheses
                var inner = existingParams.Trim('(', ')');
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    paramList.Add(inner);
                }
            }

            // Add limit if not present
            if (!LimitVariablePattern.IsMatch(existingParams ?? ""))
            {
                paramList.Add("$limit: Int!");
            }

            // Add cursor if not present
            if (!CursorVariablePattern.IsMatch(existingParams ?? ""))
            {
                paramList.Add("$cursor: String");
            }

            return $"({string.Join(", ", paramList)})";
        }

        private object ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(e => ConvertJsonElement(e))
                    .ToList(),
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                _ => element.GetRawText()
            };
        }
    }
}
