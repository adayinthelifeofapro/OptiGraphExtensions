using System.Text;
using System.Text.RegularExpressions;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.QueryLibrary.Models;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;

namespace OptiGraphExtensions.Features.QueryLibrary.Services
{
    /// <summary>
    /// Service for building GraphQL queries from visual query configurations.
    /// </summary>
    public class QueryBuilderService : IQueryBuilderService
    {
        private static readonly Dictionary<string, string> OperatorMapping = new()
        {
            ["eq"] = "eq",
            ["neq"] = "neq",
            ["contains"] = "contains",
            ["startsWith"] = "startsWith",
            ["gt"] = "gt",
            ["lt"] = "lt",
            ["gte"] = "gte",
            ["lte"] = "lte",
            ["exists"] = "exists"
        };

        // Default subfields for known complex field names in Optimizely Graph
        // These fields require subfield selection and cannot be queried as scalar values
        private static readonly Dictionary<string, string> DefaultSubfieldsForFieldNames = new(StringComparer.OrdinalIgnoreCase)
        {
            // Content references
            ["ContentLink"] = "GuidValue Url",
            ["ParentLink"] = "GuidValue Url",

            // Language fields
            ["Language"] = "Name",
            ["MasterLanguage"] = "Name",
            ["ExistingLanguages"] = "Name",

            // Content area fields - use pattern matching for any *ContentArea field
            ["MainContentArea"] = "DisplayOption ContentLink { GuidValue Url }",
            ["RelatedContentArea"] = "DisplayOption ContentLink { GuidValue Url }",

            // Media/Image fields
            ["Thumbnail"] = "Url",
            ["PageImage"] = "Url",
            ["TeaserImage"] = "Url",

            // Link fields
            ["Links"] = "Href Text Title Target",

            // Metadata
            ["Ancestors"] = "GuidValue",
            ["Categories"] = "GuidValue",

            // Site
            ["SiteId"] = "GuidValue"
        };

        // Patterns for field names that are typically complex types
        private static readonly string[] ComplexFieldPatterns = new[]
        {
            "ContentArea$",     // Any field ending with ContentArea
            "^Image$",          // Image field
            "Image$",           // Any field ending with Image
            "Link$",            // Any field ending with Link (but not ContentLink which is handled above)
            "Reference$",       // Any field ending with Reference
            "Block$"            // Any field ending with Block
        };

        // Default subfields for pattern matches
        private const string DefaultContentAreaSubfields = "DisplayOption ContentLink { GuidValue Url }";
        private const string DefaultLinkSubfields = "GuidValue Url";
        private const string DefaultImageSubfields = "Url";

        public string BuildGraphQLQuery(QueryExecutionRequest request)
        {
            if (request.QueryType == QueryType.Raw && !string.IsNullOrEmpty(request.RawGraphQuery))
            {
                return request.RawGraphQuery;
            }

            if (string.IsNullOrEmpty(request.ContentType))
            {
                throw new ArgumentException("ContentType is required for visual queries");
            }

            var fieldSelection = BuildFieldSelection(request.SelectedFields);
            var whereClause = BuildWhereClause(request.Filters);
            var localeParam = !string.IsNullOrEmpty(request.Language)
                ? ", $locale: [Locales]"
                : "";
            var localeFilter = !string.IsNullOrEmpty(request.Language)
                ? "locale: $locale,"
                : "";
            var orderByClause = BuildOrderByClause(request.SortField, request.SortDescending);

            var query = $@"
query ExportQuery($limit: Int!, $cursor: String{localeParam}) {{
    {request.ContentType}(
        {whereClause}
        limit: $limit,
        cursor: $cursor,
        {localeFilter}
        {orderByClause}
    ) {{
        items {{
            {fieldSelection}
        }}
        cursor
        total
    }}
}}";

            return query;
        }

        public Dictionary<string, object> BuildVariables(QueryExecutionRequest request)
        {
            var variables = new Dictionary<string, object>
            {
                ["limit"] = request.PageSize > 0 ? request.PageSize : 100
            };

            if (!string.IsNullOrEmpty(request.Cursor))
            {
                variables["cursor"] = request.Cursor;
            }

            if (!string.IsNullOrEmpty(request.Language))
            {
                variables["locale"] = new[] { request.Language.ToLowerInvariant() };
            }

            return variables;
        }

        public string BuildFieldSelection(IEnumerable<string> fieldPaths)
        {
            if (fieldPaths == null || !fieldPaths.Any())
            {
                // Default fields if none selected
                return @"Name
                ContentLink { GuidValue }
                ContentType
                Language { Name }";
            }

            // Group nested fields by their parent path
            var fieldTree = BuildFieldTree(fieldPaths);
            return RenderFieldTree(fieldTree, 0);
        }

        public string BuildWhereClause(IEnumerable<QueryFilter> filters)
        {
            if (filters == null || !filters.Any())
            {
                return "";
            }

            var conditions = new List<string>();

            foreach (var filter in filters.Where(f =>
                !string.IsNullOrEmpty(f.Field) &&
                !string.IsNullOrEmpty(f.Value)))
            {
                var graphOperator = OperatorMapping.GetValueOrDefault(filter.Operator, "eq");
                var value = FormatFilterValue(filter.Value, graphOperator);
                conditions.Add($"{filter.Field}: {{ {graphOperator}: {value} }}");
            }

            if (!conditions.Any())
            {
                return "";
            }

            return $"where: {{ {string.Join(", ", conditions)} }},";
        }

        private string BuildOrderByClause(string? sortField, bool sortDescending)
        {
            if (string.IsNullOrEmpty(sortField))
            {
                return "";
            }

            var direction = sortDescending ? "DESC" : "ASC";
            return $"orderBy: {{ {sortField}: {direction} }}";
        }

        private Dictionary<string, object> BuildFieldTree(IEnumerable<string> fieldPaths)
        {
            var tree = new Dictionary<string, object>();

            foreach (var path in fieldPaths)
            {
                var parts = path.Split('.');
                var current = tree;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (i == parts.Length - 1)
                    {
                        // Leaf node
                        if (!current.ContainsKey(part))
                        {
                            current[part] = new Dictionary<string, object>();
                        }
                    }
                    else
                    {
                        // Branch node
                        if (!current.ContainsKey(part))
                        {
                            current[part] = new Dictionary<string, object>();
                        }
                        current = (Dictionary<string, object>)current[part];
                    }
                }
            }

            return tree;
        }

        private string RenderFieldTree(Dictionary<string, object> tree, int indent)
        {
            var sb = new StringBuilder();
            var indentStr = new string(' ', indent * 4);

            foreach (var (fieldName, children) in tree)
            {
                var childDict = (Dictionary<string, object>)children;

                if (childDict.Count == 0)
                {
                    // Leaf field - check if it needs default subfields
                    var subfields = GetDefaultSubfieldsForField(fieldName);
                    if (!string.IsNullOrEmpty(subfields))
                    {
                        // Complex field - add default subfields
                        sb.AppendLine($"{indentStr}{fieldName} {{ {subfields} }}");
                    }
                    else
                    {
                        // Simple scalar field
                        sb.AppendLine($"{indentStr}{fieldName}");
                    }
                }
                else
                {
                    // Nested field with explicit children
                    sb.AppendLine($"{indentStr}{fieldName} {{");
                    sb.Append(RenderFieldTree(childDict, indent + 1));
                    sb.AppendLine($"{indentStr}}}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the default subfields for a complex field name, if any.
        /// Returns null for scalar fields that don't need subfields.
        /// </summary>
        private string? GetDefaultSubfieldsForField(string fieldName)
        {
            // First check exact matches
            if (DefaultSubfieldsForFieldNames.TryGetValue(fieldName, out var subfields))
            {
                return subfields;
            }

            // Then check patterns
            if (Regex.IsMatch(fieldName, "ContentArea$", RegexOptions.IgnoreCase))
            {
                return DefaultContentAreaSubfields;
            }

            if (Regex.IsMatch(fieldName, "Image$", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(fieldName, "^Image$", RegexOptions.IgnoreCase))
            {
                return DefaultImageSubfields;
            }

            if (Regex.IsMatch(fieldName, "Link$", RegexOptions.IgnoreCase) &&
                !fieldName.Equals("ContentLink", StringComparison.OrdinalIgnoreCase) &&
                !fieldName.Equals("ParentLink", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultLinkSubfields;
            }

            if (Regex.IsMatch(fieldName, "Reference$", RegexOptions.IgnoreCase))
            {
                return DefaultLinkSubfields;
            }

            // No default subfields - treat as scalar
            return null;
        }

        private string FormatFilterValue(string value, string graphOperator)
        {
            // For boolean operators
            if (graphOperator == "exists")
            {
                return value.ToLowerInvariant() == "true" ? "true" : "false";
            }

            // Try to parse as number
            if (int.TryParse(value, out _) || double.TryParse(value, out _))
            {
                return value;
            }

            // Try to parse as boolean
            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue ? "true" : "false";
            }

            // String value - needs quotes
            var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }
    }
}
