using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.QueryLibrary.Models;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.QueryLibrary
{
    public class QueryLibraryManagementComponentBase : ManagementComponentBase<SavedQuery, SavedQueryModel>
    {
        [Inject]
        protected ISavedQueryService SavedQueryService { get; set; } = null!;

        [Inject]
        protected ISchemaDiscoveryService SchemaService { get; set; } = null!;

        [Inject]
        protected IQueryExecutionService ExecutionService { get; set; } = null!;

        [Inject]
        protected ICsvExportService CsvExportService { get; set; } = null!;

        [Inject]
        protected IRawQueryService RawQueryService { get; set; } = null!;

        [Inject]
        protected IQueryBuilderService QueryBuilderService { get; set; } = null!;

        [Inject]
        protected IPaginationService<SavedQueryModel> PaginationService { get; set; } = null!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = null!;

        // UI State
        protected string CurrentMode { get; set; } = "list"; // list, build, preview
        protected string QueryInputMode { get; set; } = "visual"; // visual, raw

        // Saved Queries
        protected IList<SavedQueryModel> AllQueries { get; set; } = new List<SavedQueryModel>();
        protected PaginationResult<SavedQueryModel>? PaginationResult { get; set; }
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<SavedQueryModel> Queries => PaginationResult?.Items?.ToList() ?? new List<SavedQueryModel>();

        // Query Builder State
        protected SavedQueryModel CurrentQuery { get; set; } = new();
        protected IList<string> AvailableContentTypes { get; set; } = new List<string>();
        protected IList<SchemaField> AvailableFields { get; set; } = new List<SchemaField>();
        protected bool IsLoadingContentTypes { get; set; }
        protected bool IsLoadingFields { get; set; }

        // Preview State
        protected QueryExecutionResult? PreviewResult { get; set; }
        protected bool IsExecuting { get; set; }
        protected bool IsExporting { get; set; }
        protected int ExportProgress { get; set; }

        // Validation
        protected QueryValidationResult? RawQueryValidation { get; set; }

        protected override async Task LoadDataAsync()
        {
            await LoadQueries();
            await LoadContentTypes();
        }

        protected async Task LoadQueries()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                AllQueries = (await SavedQueryService.GetAllQueriesAsync()).ToList();
                UpdatePaginatedQueries();
            }, "loading saved queries");
        }

        protected async Task LoadContentTypes()
        {
            IsLoadingContentTypes = true;
            StateHasChanged();

            try
            {
                AvailableContentTypes = await SchemaService.GetContentTypesAsync();
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Failed to load content types: {ex.Message}");
            }
            finally
            {
                IsLoadingContentTypes = false;
                StateHasChanged();
            }
        }

        protected async Task OnContentTypeChanged(ChangeEventArgs e)
        {
            CurrentQuery.ContentType = e.Value?.ToString();
            CurrentQuery.SelectedFields = new List<string>();

            if (!string.IsNullOrEmpty(CurrentQuery.ContentType))
            {
                await LoadFieldsForContentType(CurrentQuery.ContentType);
            }
            else
            {
                AvailableFields = new List<SchemaField>();
            }

            OnVisualSettingsChanged();
            StateHasChanged();
        }

        protected async Task LoadFieldsForContentType(string contentType)
        {
            IsLoadingFields = true;
            StateHasChanged();

            try
            {
                AvailableFields = await SchemaService.GetFieldsForContentTypeAsync(contentType);
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Failed to load fields: {ex.Message}");
            }
            finally
            {
                IsLoadingFields = false;
                StateHasChanged();
            }
        }

        protected void ToggleField(string fieldPath, bool isChecked)
        {
            if (isChecked)
            {
                if (!CurrentQuery.SelectedFields.Contains(fieldPath))
                {
                    CurrentQuery.SelectedFields.Add(fieldPath);
                }
            }
            else
            {
                CurrentQuery.SelectedFields.Remove(fieldPath);
            }
            OnVisualSettingsChanged();
            StateHasChanged();
        }

        protected bool IsFieldSelected(string fieldPath)
        {
            return CurrentQuery.SelectedFields.Contains(fieldPath);
        }

        protected void AddFilter()
        {
            CurrentQuery.Filters.Add(new QueryFilter
            {
                Field = "",
                Operator = "eq",
                Value = ""
            });
            OnVisualSettingsChanged();
            StateHasChanged();
        }

        protected void RemoveFilter(QueryFilter filter)
        {
            CurrentQuery.Filters.Remove(filter);
            OnVisualSettingsChanged();
            StateHasChanged();
        }

        protected void OnFilterChanged()
        {
            OnVisualSettingsChanged();
            StateHasChanged();
        }

        protected void OnSortChanged()
        {
            OnVisualSettingsChanged();
            StateHasChanged();
        }

        protected void OnLanguageChanged()
        {
            OnVisualSettingsChanged();
            StateHasChanged();
        }

        protected void ShowCreateForm()
        {
            CurrentMode = "build";
            QueryInputMode = "visual";
            CurrentQuery = new SavedQueryModel
            {
                QueryType = QueryType.Visual,
                SelectedFields = new List<string>(),
                Filters = new List<QueryFilter>(),
                PageSize = 100
            };
            PreviewResult = null;
            RawQueryValidation = null;
            ClearMessages();
        }

        protected void ShowRawQueryForm()
        {
            CurrentMode = "build";
            QueryInputMode = "raw";
            CurrentQuery = new SavedQueryModel
            {
                QueryType = QueryType.Raw,
                RawGraphQuery = GetDefaultRawQuery(),
                QueryVariablesJson = "{\n  \"limit\": 100\n}",
                PageSize = 100
            };
            PreviewResult = null;
            RawQueryValidation = null;
            ClearMessages();
        }

        protected async Task SwitchToVisualMode()
        {
            // Try to parse raw query into visual settings
            if (!string.IsNullOrEmpty(CurrentQuery.RawGraphQuery))
            {
                await ParseRawQueryToVisualAsync();
            }

            QueryInputMode = "visual";
            CurrentQuery.QueryType = QueryType.Visual;
            StateHasChanged();
        }

        protected void SwitchToRawMode()
        {
            // Generate raw query from visual settings
            SyncVisualToRaw();

            QueryInputMode = "raw";
            CurrentQuery.QueryType = QueryType.Raw;
            StateHasChanged();
        }

        /// <summary>
        /// Generates the raw GraphQL query from the current visual builder settings.
        /// </summary>
        protected void SyncVisualToRaw()
        {
            try
            {
                // Only generate if we have a content type selected
                if (string.IsNullOrEmpty(CurrentQuery.ContentType))
                {
                    if (string.IsNullOrEmpty(CurrentQuery.RawGraphQuery))
                    {
                        CurrentQuery.RawGraphQuery = GetDefaultRawQuery();
                        CurrentQuery.QueryVariablesJson = "{\n  \"limit\": 100\n}";
                    }
                    return;
                }

                // Create execution request from current visual settings
                var request = new QueryExecutionRequest
                {
                    QueryType = QueryType.Visual,
                    ContentType = CurrentQuery.ContentType,
                    SelectedFields = CurrentQuery.SelectedFields ?? new List<string>(),
                    Filters = CurrentQuery.Filters ?? new List<QueryFilter>(),
                    Language = CurrentQuery.Language,
                    SortField = CurrentQuery.SortField,
                    SortDescending = CurrentQuery.SortDescending,
                    PageSize = CurrentQuery.PageSize > 0 ? CurrentQuery.PageSize : 100
                };

                // Generate the GraphQL query
                CurrentQuery.RawGraphQuery = QueryBuilderService.BuildGraphQLQuery(request);

                // Generate variables
                var variables = QueryBuilderService.BuildVariables(request);
                CurrentQuery.QueryVariablesJson = System.Text.Json.JsonSerializer.Serialize(
                    variables,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception)
            {
                // If generation fails, use default query
                if (string.IsNullOrEmpty(CurrentQuery.RawGraphQuery))
                {
                    CurrentQuery.RawGraphQuery = GetDefaultRawQuery();
                    CurrentQuery.QueryVariablesJson = "{\n  \"limit\": 100\n}";
                }
            }
        }

        /// <summary>
        /// Parses a raw GraphQL query and updates the visual builder settings.
        /// </summary>
        protected async Task ParseRawQueryToVisualAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentQuery.RawGraphQuery))
            {
                return;
            }

            try
            {
                var query = CurrentQuery.RawGraphQuery;

                // Extract content type from query (e.g., "ArticlePage(" or "Content(")
                var contentTypeMatch = Regex.Match(query, @"^\s*(?:query\s+\w+\s*\([^)]*\)\s*\{\s*)?(\w+)\s*\(", RegexOptions.Multiline);
                if (contentTypeMatch.Success)
                {
                    var parsedContentType = contentTypeMatch.Groups[1].Value;

                    // Check if content type exists in available types
                    if (AvailableContentTypes.Contains(parsedContentType))
                    {
                        if (CurrentQuery.ContentType != parsedContentType)
                        {
                            CurrentQuery.ContentType = parsedContentType;
                            await LoadFieldsForContentType(parsedContentType);
                        }
                    }
                }

                // Extract fields from items { ... } block
                var itemsMatch = Regex.Match(query, @"items\s*\{([^}]+(?:\{[^}]*\}[^}]*)*)\}", RegexOptions.Singleline);
                if (itemsMatch.Success)
                {
                    var fieldsBlock = itemsMatch.Groups[1].Value;
                    var parsedFields = ParseFieldsFromBlock(fieldsBlock);

                    if (parsedFields.Any())
                    {
                        CurrentQuery.SelectedFields = parsedFields;
                    }
                }

                // Extract language from locale parameter
                var localeMatch = Regex.Match(query, @"locale:\s*\$locale");
                if (localeMatch.Success)
                {
                    // Try to get locale from variables
                    if (!string.IsNullOrEmpty(CurrentQuery.QueryVariablesJson))
                    {
                        try
                        {
                            var variables = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                                CurrentQuery.QueryVariablesJson,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (variables?.TryGetValue("locale", out var localeValue) == true)
                            {
                                if (localeValue is System.Text.Json.JsonElement jsonElement)
                                {
                                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array && jsonElement.GetArrayLength() > 0)
                                    {
                                        CurrentQuery.Language = jsonElement[0].GetString();
                                    }
                                    else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        CurrentQuery.Language = jsonElement.GetString();
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                // Extract orderBy
                var orderByMatch = Regex.Match(query, @"orderBy:\s*\{\s*(\w+):\s*(ASC|DESC)\s*\}", RegexOptions.IgnoreCase);
                if (orderByMatch.Success)
                {
                    CurrentQuery.SortField = orderByMatch.Groups[1].Value;
                    CurrentQuery.SortDescending = orderByMatch.Groups[2].Value.Equals("DESC", StringComparison.OrdinalIgnoreCase);
                }

                // Note: Parsing where clause filters is complex and may not cover all cases
                // We'll skip filter parsing for now as it requires a full GraphQL parser
            }
            catch (Exception)
            {
                // Parsing failed - keep existing visual settings
            }
        }

        /// <summary>
        /// Parses field names from a GraphQL fields block.
        /// </summary>
        private List<string> ParseFieldsFromBlock(string fieldsBlock, string prefix = "")
        {
            var fields = new List<string>();

            // Simple regex-based parsing of field names
            // This handles basic cases like "Name", "ContentLink { GuidValue }", etc.
            var lines = fieldsBlock.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed == "{" || trimmed == "}")
                {
                    continue;
                }

                // Check for nested field: FieldName { ... }
                var nestedMatch = Regex.Match(trimmed, @"^(\w+)\s*\{");
                if (nestedMatch.Success)
                {
                    var fieldName = nestedMatch.Groups[1].Value;
                    var fullPath = string.IsNullOrEmpty(prefix) ? fieldName : $"{prefix}.{fieldName}";

                    // For nested fields, we add the parent field (visual builder will add default subfields)
                    fields.Add(fullPath);
                }
                else
                {
                    // Simple field name
                    var fieldMatch = Regex.Match(trimmed, @"^(\w+)\s*$");
                    if (fieldMatch.Success)
                    {
                        var fieldName = fieldMatch.Groups[1].Value;
                        // Skip cursor and total as they're pagination fields
                        if (fieldName != "cursor" && fieldName != "total")
                        {
                            var fullPath = string.IsNullOrEmpty(prefix) ? fieldName : $"{prefix}.{fieldName}";
                            fields.Add(fullPath);
                        }
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// Called when visual builder settings change to update the raw query.
        /// </summary>
        protected void OnVisualSettingsChanged()
        {
            // Update raw query if we're in build mode
            if (CurrentMode == "build")
            {
                SyncVisualToRaw();
            }
        }

        protected void BackToList()
        {
            CurrentMode = "list";
            CurrentQuery = new SavedQueryModel();
            PreviewResult = null;
            RawQueryValidation = null;
            ClearMessages();
        }

        protected async Task EditQuery(SavedQueryModel query)
        {
            CurrentMode = "build";
            CurrentQuery = new SavedQueryModel
            {
                Id = query.Id,
                Name = query.Name,
                Description = query.Description,
                QueryType = query.QueryType,
                ContentType = query.ContentType,
                SelectedFields = query.SelectedFields?.ToList() ?? new List<string>(),
                Filters = query.Filters?.Select(f => new QueryFilter
                {
                    Field = f.Field,
                    Operator = f.Operator,
                    Value = f.Value
                }).ToList() ?? new List<QueryFilter>(),
                Language = query.Language,
                SortField = query.SortField,
                SortDescending = query.SortDescending,
                RawGraphQuery = query.RawGraphQuery,
                QueryVariablesJson = query.QueryVariablesJson,
                PageSize = query.PageSize,
                IsActive = query.IsActive
            };

            QueryInputMode = query.QueryType == QueryType.Raw ? "raw" : "visual";

            if (!string.IsNullOrEmpty(CurrentQuery.ContentType))
            {
                await LoadFieldsForContentType(CurrentQuery.ContentType);
            }

            PreviewResult = null;
            RawQueryValidation = null;
            ClearMessages();
        }

        protected async Task RunQuery(SavedQueryModel query)
        {
            await EditQuery(query);
            await ExecutePreview();
        }

        protected async Task DeleteQuery(Guid id)
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await SavedQueryService.DeleteQueryAsync(id);
                SetSuccessMessage("Query deleted successfully.");
                await LoadQueries();
            }, "deleting query", showLoading: false);
        }

        protected async Task SaveQuery()
        {
            if (string.IsNullOrWhiteSpace(CurrentQuery.Name))
            {
                SetErrorMessage("Query name is required.");
                return;
            }

            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                if (CurrentQuery.Id == Guid.Empty)
                {
                    await SavedQueryService.CreateQueryAsync(CurrentQuery);
                    SetSuccessMessage("Query saved successfully.");
                }
                else
                {
                    await SavedQueryService.UpdateQueryAsync(CurrentQuery.Id, CurrentQuery);
                    SetSuccessMessage("Query updated successfully.");
                }

                await LoadQueries();
            }, "saving query", showLoading: false);
        }

        protected async Task ExecutePreview()
        {
            IsExecuting = true;
            PreviewResult = null;
            ClearMessages();
            StateHasChanged();

            try
            {
                // Validate raw query if in raw mode
                if (CurrentQuery.QueryType == QueryType.Raw)
                {
                    if (string.IsNullOrWhiteSpace(CurrentQuery.RawGraphQuery))
                    {
                        SetErrorMessage("GraphQL query is required.");
                        return;
                    }

                    RawQueryValidation = RawQueryService.ValidateQuery(CurrentQuery.RawGraphQuery);
                    if (!RawQueryValidation.IsValid)
                    {
                        SetErrorMessage(string.Join("; ", RawQueryValidation.Messages));
                        return;
                    }
                }
                else
                {
                    // Validate visual query
                    if (string.IsNullOrEmpty(CurrentQuery.ContentType))
                    {
                        SetErrorMessage("Content type is required.");
                        return;
                    }
                }

                var request = SavedQueryService.ToExecutionRequest(CurrentQuery);
                PreviewResult = await ExecutionService.ExecuteQueryAsync(request);

                if (!PreviewResult.IsSuccess)
                {
                    SetErrorMessage(PreviewResult.ErrorMessage ?? "Query execution failed.");
                }
                else
                {
                    CurrentMode = "preview";
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Query execution failed: {ex.Message}");
            }
            finally
            {
                IsExecuting = false;
                StateHasChanged();
            }
        }

        protected async Task ExportToCsv()
        {
            IsExporting = true;
            ExportProgress = 0;
            ClearMessages();

            try
            {
                await InvokeAsync(StateHasChanged);

                // If we have preview results, export those first (single page)
                // For full export with pagination, we'll fetch all pages
                if (PreviewResult != null && PreviewResult.IsSuccess && PreviewResult.Rows.Any())
                {
                    // Quick export of current preview
                    await ExportPreviewDataAsync();
                }
                else
                {
                    // Full export with pagination
                    await ExportAllPagesAsync();
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Export failed: {ex.Message}");
            }
            finally
            {
                IsExporting = false;
                ExportProgress = 0;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task ExportPreviewDataAsync()
        {
            if (PreviewResult == null || !PreviewResult.Rows.Any())
            {
                SetErrorMessage("No preview data to export.");
                return;
            }

            ExportProgress = 50;
            await InvokeAsync(StateHasChanged);

            var csvContent = CsvExportService.GenerateCsvPreview(PreviewResult, int.MaxValue);
            var filename = CsvExportService.GenerateFilename(CurrentQuery.Name ?? "export");

            ExportProgress = 90;
            await InvokeAsync(StateHasChanged);

            // Trigger download via JavaScript
            await DownloadCsvAsync(filename, csvContent);

            var rowCount = PreviewResult.Rows.Count;
            var hasMore = PreviewResult.HasMore;
            SetSuccessMessage($"Exported {rowCount} rows{(hasMore ? " (first page only - use full export for all data)" : "")}.");
        }

        private async Task ExportAllPagesAsync()
        {
            var request = SavedQueryService.ToExecutionRequest(CurrentQuery);

            var allRows = new List<Dictionary<string, object?>>();
            var columns = new List<string>();
            string? cursor = null;
            var hasMore = true;
            var pageCount = 0;
            var totalCount = 0;

            while (hasMore)
            {
                pageCount++;
                request.Cursor = cursor;

                QueryExecutionResult result;
                try
                {
                    result = await ExecutionService.ExecuteQueryAsync(request);
                }
                catch (Exception ex)
                {
                    SetErrorMessage($"Query execution failed on page {pageCount}: {ex.Message}");
                    return;
                }

                if (!result.IsSuccess)
                {
                    SetErrorMessage(result.ErrorMessage ?? $"Export failed on page {pageCount}.");
                    return;
                }

                if (!columns.Any() && result.Columns.Any())
                {
                    columns = result.Columns;
                }

                if (totalCount == 0 && result.TotalCount > 0)
                {
                    totalCount = result.TotalCount;
                }

                allRows.AddRange(result.Rows);

                if (totalCount > 0)
                {
                    ExportProgress = Math.Min(99, (int)((double)allRows.Count / totalCount * 100));
                }
                else
                {
                    ExportProgress = Math.Min(99, pageCount * 10);
                }
                await InvokeAsync(StateHasChanged);

                cursor = result.NextCursor;
                hasMore = result.HasMore && !string.IsNullOrEmpty(cursor);

                if (pageCount > 1000)
                {
                    SetErrorMessage("Export exceeded maximum page limit (1000 pages).");
                    return;
                }
            }

            if (!allRows.Any())
            {
                SetErrorMessage("No data to export.");
                return;
            }

            var csvResult = new QueryExecutionResult
            {
                Rows = allRows,
                Columns = columns,
                TotalCount = allRows.Count
            };

            var csvContent = CsvExportService.GenerateCsvPreview(csvResult, int.MaxValue);
            var filename = CsvExportService.GenerateFilename(CurrentQuery.Name ?? "export");

            await DownloadCsvAsync(filename, csvContent);

            SetSuccessMessage($"Exported {allRows.Count} rows successfully.");
        }

        protected async Task DownloadCsvAsync(string filename, string content)
        {
            // Use JavaScript to trigger file download
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var base64 = Convert.ToBase64String(bytes);
            await JSRuntime.InvokeVoidAsync("downloadCsv", filename, base64);
        }

        protected void BackToBuilder()
        {
            CurrentMode = "build";
            StateHasChanged();
        }

        protected void UpdatePaginatedQueries()
        {
            PaginationResult = PaginationService.GetPage(AllQueries, CurrentPage, PageSize);
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            NavigateToPage(page, CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedQueriesAsync);
        }

        protected void GoToPreviousPage()
        {
            NavigateToPreviousPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedQueriesAsync);
        }

        protected void GoToNextPage()
        {
            NavigateToNextPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedQueriesAsync);
        }

        private Task UpdatePaginatedQueriesAsync()
        {
            UpdatePaginatedQueries();
            return Task.CompletedTask;
        }

        protected static string GetQueryTypeBadgeClass(QueryType queryType)
        {
            return queryType == QueryType.Raw ? "epi-badge--secondary" : "epi-badge--primary";
        }

        protected static string GetQueryTypeText(QueryType queryType)
        {
            return queryType == QueryType.Raw ? "Raw GraphQL" : "Visual";
        }

        protected static string FormatDate(DateTime? date)
        {
            return date?.ToString("yyyy-MM-dd HH:mm") ?? "";
        }

        private static string GetDefaultRawQuery()
        {
            return @"query ExportContent($limit: Int!, $cursor: String) {
  Content(limit: $limit, cursor: $cursor) {
    items {
      Name
      ContentLink {
        GuidValue
      }
      ContentType
      Language {
        Name
      }
    }
    cursor
    total
  }
}";
        }

        protected static IEnumerable<string> AvailableOperators => new[]
        {
            "eq",
            "neq",
            "contains",
            "startsWith",
            "gt",
            "lt",
            "gte",
            "lte"
        };

        protected static string GetOperatorDisplayName(string op)
        {
            return op switch
            {
                "eq" => "equals",
                "neq" => "not equals",
                "contains" => "contains",
                "startsWith" => "starts with",
                "gt" => "greater than",
                "lt" => "less than",
                "gte" => "greater or equal",
                "lte" => "less or equal",
                _ => op
            };
        }
    }
}
