using System.Text.Json;
using Microsoft.AspNetCore.Components;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Repositories;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData
{
    /// <summary>
    /// Base class for the Custom Data management Blazor component.
    /// Provides UI state management and service orchestration.
    /// </summary>
    public class CustomDataManagementComponentBase : ManagementComponentBase<CustomDataSourceModel, CustomDataSourceModel>
    {
        [Inject]
        protected ICustomDataSchemaService SchemaService { get; set; } = null!;

        [Inject]
        protected ICustomDataService DataService { get; set; } = null!;

        [Inject]
        protected ICustomDataValidationService ValidationService { get; set; } = null!;

        [Inject]
        protected INdJsonBuilderService NdJsonBuilder { get; set; } = null!;

        [Inject]
        protected ISchemaParserService SchemaParser { get; set; } = null!;

        [Inject]
        protected IPaginationService<CustomDataSourceModel> PaginationService { get; set; } = null!;

        [Inject]
        protected IPaginationService<CustomDataItemModel> DataPaginationService { get; set; } = null!;

        [Inject]
        protected IExternalDataImportService ImportService { get; set; } = null!;

        [Inject]
        protected IImportConfigurationRepository ImportConfigRepository { get; set; } = null!;

        [Inject]
        protected IApiSchemaInferenceService ApiSchemaInferenceService { get; set; } = null!;

        // UI Mode State
        protected string CurrentMode { get; set; } = "list"; // list, schema, data
        protected string EditorMode { get; set; } = "visual"; // visual, raw

        // Schema Editor State
        protected CreateSchemaRequest CurrentSchema { get; set; } = new();
        protected string RawSchemaJson { get; set; } = string.Empty;
        protected bool ShowFullSyncWarning { get; set; }
        protected bool ConfirmedFullSync { get; set; }
        protected bool IsEditingSchema { get; set; }

        // Data Editor State
        protected List<CustomDataItemModel> DataItems { get; set; } = new();
        protected string RawNdJson { get; set; } = string.Empty;
        protected CustomDataItemModel? EditingItem { get; set; }
        protected string CurrentSourceId { get; set; } = string.Empty;
        protected ContentTypeSchemaModel? SelectedContentType { get; set; }

        // Sources List State
        protected IList<CustomDataSourceModel> AllSources { get; set; } = new List<CustomDataSourceModel>();
        protected PaginationResult<CustomDataSourceModel>? PaginationResult { get; set; }
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;

        // Data Items Pagination State
        protected PaginationResult<CustomDataItemModel>? DataPaginationResult { get; set; }
        protected int DataCurrentPage { get; set; } = 1;
        protected int DataPageSize { get; set; } = 10;
        protected int DataTotalPages => DataPaginationResult?.TotalPages ?? 0;
        protected int DataTotalItems => DataPaginationResult?.TotalItems ?? 0;

        // Loading States
        protected bool IsLoadingSchema { get; set; }
        protected bool IsSyncing { get; set; }

        // Delete Confirmation State
        protected bool ShowDeleteConfirmation { get; set; }
        protected string? SourceIdToDelete { get; set; }
        protected bool IsDeleting { get; set; }

        // Clear Data Confirmation State
        protected bool ShowClearDataConfirmation { get; set; }
        protected bool IsClearingData { get; set; }

        // Import State
        protected List<ImportConfigurationModel> ImportConfigurations { get; set; } = new();
        protected ImportConfigurationModel? EditingImportConfig { get; set; }
        protected bool IsTestingConnection { get; set; }
        protected bool IsImporting { get; set; }
        protected string? TestConnectionResult { get; set; }
        protected bool TestConnectionSuccess { get; set; }
        protected string? TestConnectionSample { get; set; }
        protected ImportResult? LastImportResult { get; set; }
        protected List<CustomDataItemModel> ImportPreviewItems { get; set; } = new();
        protected List<string> ImportPreviewWarnings { get; set; } = new();
        protected bool ShowImportPreview { get; set; }
        protected bool ShowImportConfirmation { get; set; }
        protected bool ShowImportSection { get; set; }
        protected string ImportNdJsonPreview { get; set; } = string.Empty;
        protected bool ShowNdJsonPreview { get; set; }
        protected ImportConfigurationModel? ImportConfigToDelete { get; set; }
        protected bool ShowDeleteImportConfigConfirmation { get; set; }
        protected ImportConfigurationModel? ImportConfigToRun { get; set; }
        protected bool ShowRunImportConfigConfirmation { get; set; }

        // Debug information
        protected string DebugInfo { get; set; } = string.Empty;
        protected bool ShowDebugInfo { get; set; }

        // API Schema Import State
        protected bool ShowApiSchemaImport { get; set; }
        protected string ApiSchemaUrl { get; set; } = string.Empty;
        protected string ApiSchemaContentTypeName { get; set; } = string.Empty;
        protected string ApiSchemaJsonPath { get; set; } = string.Empty;
        protected Dictionary<string, string> ApiSchemaHeaders { get; set; } = new();
        protected string NewApiHeaderKey { get; set; } = string.Empty;
        protected string NewApiHeaderValue { get; set; } = string.Empty;
        protected bool IsInferringSchema { get; set; }
        protected ApiSchemaInferenceResult? ApiSchemaInferenceResult { get; set; }
        protected bool ShowApiSchemaPreview { get; set; }

        // Available property types for dropdowns
        protected static IEnumerable<string> AvailablePropertyTypes => PropertyTypeModel.AvailableTypes;

        protected override async Task LoadDataAsync()
        {
            await LoadSources();
        }

        protected async Task LoadSources()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                AllSources = (await SchemaService.GetAllSourcesAsync()).ToList();
                UpdatePaginatedSources();
            }, "loading sources");
        }

        protected void UpdatePaginatedSources()
        {
            PaginationResult = PaginationService.GetPage(AllSources, CurrentPage, PageSize);
            StateHasChanged();
        }

        #region Navigation Methods

        protected void BackToList()
        {
            CurrentMode = "list";
            CurrentSchema = new CreateSchemaRequest();
            DataItems = new List<CustomDataItemModel>();
            RawSchemaJson = string.Empty;
            RawNdJson = string.Empty;
            ShowFullSyncWarning = false;
            ConfirmedFullSync = false;
            IsEditingSchema = false;
            EditingItem = null;
            ClearMessages();
        }

        protected void ShowCreateSchemaForm()
        {
            CurrentMode = "schema";
            EditorMode = "visual";
            IsEditingSchema = false;
            CurrentSchema = new CreateSchemaRequest
            {
                Languages = new List<string> { "en" },
                ContentTypes = new List<ContentTypeSchemaModel>
                {
                    new ContentTypeSchemaModel
                    {
                        Name = "Item",
                        Properties = new List<PropertyTypeModel>
                        {
                            new PropertyTypeModel { Name = "Name", Type = "String", IsSearchable = true }
                        }
                    }
                }
            };
            RawSchemaJson = SchemaParser.ModelToDisplayJson(CurrentSchema);
            ShowFullSyncWarning = false;
            ConfirmedFullSync = false;
            ClearMessages();
        }

        protected async Task ShowEditSchemaForm(string sourceId)
        {
            IsLoadingSchema = true;
            StateHasChanged();

            try
            {
                var source = await SchemaService.GetSourceByIdAsync(sourceId);
                if (source == null)
                {
                    SetErrorMessage($"Source '{sourceId}' not found.");
                    return;
                }

                CurrentMode = "schema";
                EditorMode = "visual";
                IsEditingSchema = true;
                CurrentSchema = new CreateSchemaRequest
                {
                    SourceId = source.SourceId,
                    Label = source.Label,
                    Languages = source.Languages,
                    PropertyTypes = source.PropertyTypes,
                    ContentTypes = source.ContentTypes
                };
                RawSchemaJson = SchemaParser.ModelToDisplayJson(CurrentSchema);
                ShowFullSyncWarning = false;
                ConfirmedFullSync = false;
                ClearMessages();
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error loading source: {ex.Message}");
            }
            finally
            {
                IsLoadingSchema = false;
                StateHasChanged();
            }
        }

        protected async Task ShowDataManagement(string sourceId)
        {
            IsLoadingSchema = true;
            StateHasChanged();

            try
            {
                var source = await SchemaService.GetSourceByIdAsync(sourceId);
                if (source == null)
                {
                    SetErrorMessage($"Source '{sourceId}' not found.");
                    return;
                }

                CurrentMode = "data";
                EditorMode = "visual";
                CurrentSourceId = sourceId;
                CurrentSchema = new CreateSchemaRequest
                {
                    SourceId = source.SourceId,
                    Label = source.Label,
                    Languages = source.Languages,
                    PropertyTypes = source.PropertyTypes,
                    ContentTypes = source.ContentTypes
                };
                DataItems = new List<CustomDataItemModel>();
                RawNdJson = string.Empty;
                EditingItem = null;

                if (source.ContentTypes.Any())
                {
                    SelectedContentType = source.ContentTypes.First();
                    // Automatically load data from Graph for the first content type
                    await LoadDataFromGraph();
                }

                ClearMessages();
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error loading source: {ex.Message}");
            }
            finally
            {
                IsLoadingSchema = false;
                StateHasChanged();
            }
        }

        protected async Task LoadDataFromGraph()
        {
            if (SelectedContentType == null || string.IsNullOrEmpty(CurrentSourceId))
            {
                return;
            }

            IsLoadingSchema = true;
            var propertyNames = SelectedContentType.Properties.Select(p => p.Name).ToList();

            // Use the first language from the schema, or "en" as default
            var language = CurrentSchema.Languages.FirstOrDefault() ?? "en";

            DebugInfo = $"Loading data for content type: {SelectedContentType.Name}\n";
            DebugInfo += $"Source ID: {CurrentSourceId}\n";
            DebugInfo += $"Language: {language}\n";
            DebugInfo += $"Properties: {string.Join(", ", propertyNames)}\n";
            StateHasChanged();

            try
            {
                DebugInfo += $"Calling GetAllItemsAsync...\n";

                // Fetch all items - service handles pagination automatically (100 items per page)
                var (items, queryDebugInfo) = await DataService.GetAllItemsWithDebugAsync(
                    CurrentSourceId,
                    SelectedContentType.Name,
                    propertyNames,
                    language,
                    10000); // Max limit - will fetch all pages up to this amount

                DataItems = items.ToList();
                UpdateDataPagination();

                DebugInfo += $"\n=== GRAPHQL QUERY DEBUG ===\n{queryDebugInfo}\n";
                DebugInfo += $"\nReceived {DataItems.Count} items from Graph.\n";

                if (DataItems.Any())
                {
                    SetSuccessMessage($"Loaded {DataItems.Count} items from Graph.");
                }
                else
                {
                    DebugInfo += "No items returned. Possible causes:\n";
                    DebugInfo += "- Data may still be indexing (wait 30-60 seconds after sync)\n";
                    DebugInfo += "- Check Optimizely Graph Portal > Sync Logs for errors\n";
                    DebugInfo += "- Property names may not match schema\n";
                }
            }
            catch (Exception ex)
            {
                DebugInfo += $"ERROR: {ex.Message}\n";
                SetErrorMessage($"Error loading data from Graph: {ex.Message}");
            }
            finally
            {
                IsLoadingSchema = false;
                StateHasChanged();
            }
        }

        protected void ToggleDebugInfo()
        {
            ShowDebugInfo = !ShowDebugInfo;
            StateHasChanged();
        }

        #endregion

        #region Schema Editor Methods

        protected void SwitchToRawMode()
        {
            if (CurrentMode == "schema")
            {
                RawSchemaJson = SchemaParser.ModelToDisplayJson(CurrentSchema);
            }
            else if (CurrentMode == "data")
            {
                RawNdJson = NdJsonBuilder.BuildNdJson(DataItems);
            }
            EditorMode = "raw";
            StateHasChanged();
        }

        protected void SwitchToVisualMode()
        {
            if (CurrentMode == "schema")
            {
                try
                {
                    var parsed = SchemaParser.JsonToModel(RawSchemaJson);
                    // Preserve source ID if editing
                    if (IsEditingSchema)
                    {
                        parsed.SourceId = CurrentSchema.SourceId;
                    }
                    CurrentSchema = parsed;
                }
                catch (Exception ex)
                {
                    SetErrorMessage($"Invalid JSON: {ex.Message}");
                    return;
                }
            }
            else if (CurrentMode == "data")
            {
                try
                {
                    DataItems = NdJsonBuilder.ParseNdJson(RawNdJson).ToList();
                }
                catch (Exception ex)
                {
                    SetErrorMessage($"Invalid NdJSON: {ex.Message}");
                    return;
                }
            }
            EditorMode = "visual";
            ClearMessages();
            StateHasChanged();
        }

        protected void AddContentType()
        {
            CurrentSchema.ContentTypes.Add(new ContentTypeSchemaModel
            {
                Name = $"Type{CurrentSchema.ContentTypes.Count + 1}",
                Properties = new List<PropertyTypeModel>
                {
                    new PropertyTypeModel { Name = "Name", Type = "String", IsSearchable = true }
                }
            });
            StateHasChanged();
        }

        protected void RemoveContentType(ContentTypeSchemaModel contentType)
        {
            CurrentSchema.ContentTypes.Remove(contentType);
            StateHasChanged();
        }

        protected void AddProperty(ContentTypeSchemaModel contentType)
        {
            contentType.Properties.Add(new PropertyTypeModel
            {
                Name = $"Property{contentType.Properties.Count + 1}",
                Type = "String",
                IsSearchable = true
            });
            StateHasChanged();
        }

        protected void RemoveProperty(ContentTypeSchemaModel contentType, PropertyTypeModel property)
        {
            contentType.Properties.Remove(property);
            StateHasChanged();
        }

        #region API Schema Import

        protected void ShowApiSchemaImportDialog()
        {
            ShowApiSchemaImport = true;
            ApiSchemaUrl = string.Empty;
            ApiSchemaContentTypeName = string.Empty;
            ApiSchemaJsonPath = string.Empty;
            ApiSchemaHeaders.Clear();
            NewApiHeaderKey = string.Empty;
            NewApiHeaderValue = string.Empty;
            ApiSchemaInferenceResult = null;
            ShowApiSchemaPreview = false;
            ClearMessages();
            StateHasChanged();
        }

        protected void CloseApiSchemaImportDialog()
        {
            ShowApiSchemaImport = false;
            ApiSchemaInferenceResult = null;
            ShowApiSchemaPreview = false;
            StateHasChanged();
        }

        protected void AddApiHeader()
        {
            if (!string.IsNullOrWhiteSpace(NewApiHeaderKey))
            {
                ApiSchemaHeaders[NewApiHeaderKey] = NewApiHeaderValue;
                NewApiHeaderKey = string.Empty;
                NewApiHeaderValue = string.Empty;
                StateHasChanged();
            }
        }

        protected void RemoveApiHeader(string key)
        {
            ApiSchemaHeaders.Remove(key);
            StateHasChanged();
        }

        protected async Task InferSchemaFromApi()
        {
            if (string.IsNullOrWhiteSpace(ApiSchemaUrl))
            {
                SetErrorMessage("Please enter an API URL");
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiSchemaContentTypeName))
            {
                SetErrorMessage("Please enter a content type name");
                return;
            }

            IsInferringSchema = true;
            ClearMessages();
            StateHasChanged();

            try
            {
                ApiSchemaInferenceResult = await ApiSchemaInferenceService.InferSchemaFromApiAsync(
                    ApiSchemaUrl,
                    ApiSchemaContentTypeName,
                    string.IsNullOrWhiteSpace(ApiSchemaJsonPath) ? null : ApiSchemaJsonPath,
                    ApiSchemaHeaders.Count > 0 ? ApiSchemaHeaders : null);

                if (ApiSchemaInferenceResult.Success)
                {
                    ShowApiSchemaPreview = true;
                    SetSuccessMessage($"Successfully inferred schema with {ApiSchemaInferenceResult.ContentType?.Properties.Count ?? 0} properties");
                }
                else
                {
                    SetErrorMessage(ApiSchemaInferenceResult.ErrorMessage ?? "Failed to infer schema");
                }

                if (!string.IsNullOrEmpty(ApiSchemaInferenceResult.DebugInfo))
                {
                    DebugInfo = ApiSchemaInferenceResult.DebugInfo;
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error inferring schema: {ex.Message}");
            }
            finally
            {
                IsInferringSchema = false;
                StateHasChanged();
            }
        }

        protected void ApplyApiSchemaToContentType()
        {
            if (ApiSchemaInferenceResult?.ContentType == null)
                return;

            // Add the inferred content type to the current schema
            CurrentSchema.ContentTypes.Add(ApiSchemaInferenceResult.ContentType);

            SetSuccessMessage($"Added content type '{ApiSchemaInferenceResult.ContentType.Name}' with {ApiSchemaInferenceResult.ContentType.Properties.Count} properties");

            // Close the dialog
            CloseApiSchemaImportDialog();
        }

        protected void ToggleApiPropertySearchable(PropertyTypeModel property)
        {
            property.IsSearchable = !property.IsSearchable;
            StateHasChanged();
        }

        protected void RemoveApiInferredProperty(PropertyTypeModel property)
        {
            ApiSchemaInferenceResult?.ContentType?.Properties.Remove(property);
            StateHasChanged();
        }

        #endregion

        protected void AddLanguage()
        {
            CurrentSchema.Languages.Add(string.Empty);
            StateHasChanged();
        }

        protected void RemoveLanguage(int index)
        {
            if (index >= 0 && index < CurrentSchema.Languages.Count)
            {
                CurrentSchema.Languages.RemoveAt(index);
                StateHasChanged();
            }
        }

        protected async Task SaveSchemaFullSync()
        {
            // Validate schema first
            var validationResult = ValidationService.ValidateSchema(CurrentSchema);
            if (!validationResult.IsValid)
            {
                SetErrorMessage(string.Join("; ", validationResult.Errors));
                return;
            }

            // Check for full sync warning
            if (!ConfirmedFullSync && IsEditingSchema)
            {
                ShowFullSyncWarning = true;
                StateHasChanged();
                return;
            }

            IsSyncing = true;
            StateHasChanged();

            try
            {
                await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
                {
                    if (IsEditingSchema)
                    {
                        await SchemaService.UpdateSchemaFullAsync(CurrentSchema);
                        SetSuccessMessage("Schema updated successfully (full sync).");
                    }
                    else
                    {
                        await SchemaService.CreateSchemaAsync(CurrentSchema);
                        SetSuccessMessage("Schema created successfully.");
                    }
                    await LoadSources();
                    BackToList();
                }, "saving schema");
            }
            finally
            {
                IsSyncing = false;
                ShowFullSyncWarning = false;
                ConfirmedFullSync = false;
                StateHasChanged();
            }
        }

        protected async Task SaveSchemaPartialSync()
        {
            var validationResult = ValidationService.ValidateSchema(CurrentSchema);
            if (!validationResult.IsValid)
            {
                SetErrorMessage(string.Join("; ", validationResult.Errors));
                return;
            }

            if (!IsEditingSchema)
            {
                SetErrorMessage("Partial sync requires the source to exist. Use full sync to create a new source.");
                return;
            }

            IsSyncing = true;
            StateHasChanged();

            try
            {
                await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
                {
                    var updateRequest = new UpdateSchemaRequest
                    {
                        SourceId = CurrentSchema.SourceId,
                        Label = CurrentSchema.Label,
                        Languages = CurrentSchema.Languages,
                        PropertyTypes = CurrentSchema.PropertyTypes,
                        ContentTypes = CurrentSchema.ContentTypes,
                        IsPartialUpdate = true
                    };
                    await SchemaService.UpdateSchemaPartialAsync(updateRequest);
                    SetSuccessMessage("Schema updated successfully (partial sync).");
                    await LoadSources();
                }, "updating schema");
            }
            finally
            {
                IsSyncing = false;
                StateHasChanged();
            }
        }

        protected void ConfirmFullSyncWarning()
        {
            ConfirmedFullSync = true;
            ShowFullSyncWarning = false;
            StateHasChanged();
        }

        protected void CancelFullSyncWarning()
        {
            ShowFullSyncWarning = false;
            StateHasChanged();
        }

        protected void ShowDeleteConfirmationDialog(string sourceId)
        {
            SourceIdToDelete = sourceId;
            ShowDeleteConfirmation = true;
            ClearMessages();
            StateHasChanged();
        }

        protected void CancelDeleteConfirmation()
        {
            ShowDeleteConfirmation = false;
            SourceIdToDelete = null;
            StateHasChanged();
        }

        protected async Task ConfirmDeleteSource()
        {
            if (string.IsNullOrEmpty(SourceIdToDelete))
            {
                return;
            }

            IsDeleting = true;
            StateHasChanged();

            try
            {
                await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
                {
                    var sourceId = SourceIdToDelete!;
                    await SchemaService.DeleteSourceAsync(sourceId);
                    SetSuccessMessage($"Source '{sourceId}' deleted successfully.");
                    ShowDeleteConfirmation = false;
                    SourceIdToDelete = null;
                    await LoadSources();
                }, "deleting source");
            }
            finally
            {
                IsDeleting = false;
                StateHasChanged();
            }
        }

        #endregion

        #region Data Editor Methods

        protected async Task OnContentTypeSelected(ChangeEventArgs e)
        {
            var typeName = e.Value?.ToString();
            SelectedContentType = CurrentSchema.ContentTypes.FirstOrDefault(ct => ct.Name == typeName);
            DataItems.Clear();
            EditingItem = null;
            UpdateDataPagination();
            StateHasChanged();

            // Load data for the selected content type
            if (SelectedContentType != null)
            {
                await LoadDataFromGraph();
            }
        }

        protected void AddDataItem()
        {
            EditingItem = new CustomDataItemModel
            {
                Id = Guid.NewGuid().ToString(),
                ContentType = SelectedContentType?.Name ?? string.Empty,
                LanguageRouting = CurrentSchema.Languages.FirstOrDefault() ?? "en",
                Properties = new Dictionary<string, object?>()
            };

            // Initialize properties from schema
            if (SelectedContentType != null)
            {
                foreach (var prop in SelectedContentType.Properties)
                {
                    EditingItem.Properties[prop.Name] = GetDefaultValue(prop.Type);
                }
            }

            StateHasChanged();
        }

        protected void EditDataItem(CustomDataItemModel item)
        {
            EditingItem = new CustomDataItemModel
            {
                Id = item.Id,
                ContentType = item.ContentType,
                LanguageRouting = item.LanguageRouting,
                Properties = new Dictionary<string, object?>(item.Properties)
            };
            StateHasChanged();
        }

        protected void SaveDataItem()
        {
            if (EditingItem == null)
            {
                return;
            }

            var validationResult = ValidationService.ValidateDataItem(EditingItem, SelectedContentType);
            if (!validationResult.IsValid)
            {
                SetErrorMessage(string.Join("; ", validationResult.Errors));
                return;
            }

            var existingIndex = DataItems.FindIndex(i => i.Id == EditingItem.Id);
            if (existingIndex >= 0)
            {
                DataItems[existingIndex] = EditingItem;
            }
            else
            {
                DataItems.Add(EditingItem);
            }

            EditingItem = null;
            UpdateDataPagination();
            SetSuccessMessage("Item saved to local list. Click 'Sync to Graph' to push changes.");
        }

        protected void CancelEditItem()
        {
            EditingItem = null;
            StateHasChanged();
        }

        protected void RemoveDataItem(CustomDataItemModel item)
        {
            DataItems.Remove(item);
            UpdateDataPagination();
            StateHasChanged();
        }

        protected async Task SyncDataToGraph()
        {
            if (!DataItems.Any())
            {
                SetErrorMessage("No data items to sync.");
                return;
            }

            IsSyncing = true;
            StateHasChanged();

            try
            {
                // Show the NdJSON being sent for debugging
                var ndJson = NdJsonBuilder.BuildNdJson(DataItems);
                DebugInfo = $"=== SYNC DEBUG INFO ===\n";
                DebugInfo += $"Source ID: {CurrentSourceId}\n";
                DebugInfo += $"Content Type: {SelectedContentType?.Name ?? "N/A"}\n";
                DebugInfo += $"Items to sync: {DataItems.Count}\n";
                DebugInfo += $"Languages in schema: {string.Join(", ", CurrentSchema.Languages)}\n\n";
                DebugInfo += $"NdJSON payload:\n{ndJson}\n";
                StateHasChanged();

                var jobId = Guid.NewGuid().ToString();
                var request = new SyncDataRequest
                {
                    SourceId = CurrentSourceId,
                    Items = DataItems,
                    JobId = jobId
                };

                DebugInfo += $"Job ID: {jobId}\n";
                DebugInfo += $"Calling SyncDataAsync...\n";
                StateHasChanged();

                var syncResponse = await DataService.SyncDataAsync(request);

                DebugInfo += $"\n=== SYNC API RESPONSE ===\n{syncResponse}\n";
                DebugInfo += "\nIMPORTANT: Check Optimizely Graph Portal > Sync Logs to verify indexing status.\n";
                DebugInfo += "Data typically takes 30-60 seconds to be indexed and queryable.\n";
                SetSuccessMessage($"Successfully synced {DataItems.Count} items to Graph. Data may take 30-60 seconds to be indexed.");
            }
            catch (Exception ex)
            {
                DebugInfo += $"\nSync ERROR: {ex.Message}\n";
                if (ex.InnerException != null)
                {
                    DebugInfo += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                SetErrorMessage($"Error syncing data: {ex.Message}");
            }
            finally
            {
                IsSyncing = false;
                StateHasChanged();
            }
        }

        protected void ShowClearDataConfirmationDialog()
        {
            ShowClearDataConfirmation = true;
            ClearMessages();
            StateHasChanged();
        }

        protected void CancelClearDataConfirmation()
        {
            ShowClearDataConfirmation = false;
            StateHasChanged();
        }

        protected async Task ConfirmClearAllData()
        {
            IsClearingData = true;
            StateHasChanged();

            try
            {
                await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
                {
                    await DataService.ClearAllDataAsync(CurrentSourceId);
                    DataItems.Clear();
                    UpdateDataPagination();
                    ShowClearDataConfirmation = false;
                    SetSuccessMessage("All data cleared from Graph.");
                }, "clearing data");
            }
            finally
            {
                IsClearingData = false;
                StateHasChanged();
            }
        }

        protected void UpdateDataPagination()
        {
            DataPaginationResult = DataPaginationService.GetPage(DataItems, DataCurrentPage, DataPageSize);
            StateHasChanged();
        }

        protected void SetPropertyValue(string propertyName, object? value)
        {
            if (EditingItem != null)
            {
                EditingItem.Properties[propertyName] = value;
            }
        }

        protected object? GetPropertyValue(string propertyName)
        {
            if (EditingItem?.Properties.TryGetValue(propertyName, out var value) == true)
            {
                return value;
            }
            return null;
        }

        #endregion

        #region Pagination Methods

        protected void GoToPage(int page)
        {
            NavigateToPage(page, CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedSourcesAsync);
        }

        protected void GoToPreviousPage()
        {
            NavigateToPreviousPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedSourcesAsync);
        }

        protected void GoToNextPage()
        {
            NavigateToNextPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedSourcesAsync);
        }

        private Task UpdatePaginatedSourcesAsync()
        {
            UpdatePaginatedSources();
            return Task.CompletedTask;
        }

        protected void DataGoToPage(int page)
        {
            NavigateToPage(page, DataCurrentPage, DataTotalPages, (p) => DataCurrentPage = p, UpdateDataPaginationAsync);
        }

        protected void DataGoToPreviousPage()
        {
            NavigateToPreviousPage(DataCurrentPage, (p) => DataCurrentPage = p, UpdateDataPaginationAsync);
        }

        protected void DataGoToNextPage()
        {
            NavigateToNextPage(DataCurrentPage, DataTotalPages, (p) => DataCurrentPage = p, UpdateDataPaginationAsync);
        }

        private Task UpdateDataPaginationAsync()
        {
            UpdateDataPagination();
            return Task.CompletedTask;
        }

        #endregion

        #region Helper Methods

        protected static object? GetDefaultValue(string propertyType)
        {
            return propertyType switch
            {
                "String" => string.Empty,
                "Int" => 0,
                "Float" => 0.0,
                "Boolean" => false,
                "[String]" => new List<string>(),
                "[Int]" => new List<int>(),
                "[Float]" => new List<double>(),
                _ => null
            };
        }

        protected static string GetInputType(string propertyType)
        {
            return propertyType switch
            {
                "Int" => "number",
                "Float" => "number",
                "Boolean" => "checkbox",
                "Date" => "date",
                "DateTime" => "datetime-local",
                _ => "text"
            };
        }

        protected string PreviewSchemaJson()
        {
            return SchemaParser.ModelToDisplayJson(CurrentSchema);
        }

        protected string PreviewNdJson()
        {
            return NdJsonBuilder.BuildNdJson(DataItems);
        }

        #endregion

        #region Import Methods

        protected void ToggleImportSection()
        {
            ShowImportSection = !ShowImportSection;
            if (ShowImportSection)
            {
                _ = LoadImportConfigurations();
            }
            StateHasChanged();
        }

        protected async Task LoadImportConfigurations()
        {
            if (string.IsNullOrEmpty(CurrentSourceId))
            {
                return;
            }

            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                var configs = await ImportConfigRepository.GetBySourceIdAsync(CurrentSourceId);
                ImportConfigurations = configs.Select(MapEntityToModel).ToList();
            }, "loading import configurations");
        }

        protected void ShowNewImportConfigForm()
        {
            EditingImportConfig = new ImportConfigurationModel
            {
                TargetSourceId = CurrentSourceId,
                TargetContentType = SelectedContentType?.Name ?? string.Empty,
                LanguageRouting = CurrentSchema.Languages.FirstOrDefault()
            };

            // Pre-populate field mappings from schema
            if (SelectedContentType != null)
            {
                foreach (var prop in SelectedContentType.Properties)
                {
                    EditingImportConfig.FieldMappings.Add(new FieldMapping
                    {
                        TargetProperty = prop.Name,
                        SourcePath = ToCamelCase(prop.Name) // Default to camelCase (common JSON convention)
                    });
                }
            }

            TestConnectionResult = null;
            TestConnectionSample = null;
            TestConnectionSuccess = false;
            ImportPreviewItems.Clear();
            ImportPreviewWarnings.Clear();
            ShowImportPreview = false;
            LastImportResult = null;
            ClearMessages();
            StateHasChanged();
        }

        protected void EditImportConfig(ImportConfigurationModel config)
        {
            EditingImportConfig = new ImportConfigurationModel
            {
                Id = config.Id,
                Name = config.Name,
                Description = config.Description,
                TargetSourceId = config.TargetSourceId,
                TargetContentType = config.TargetContentType,
                ApiUrl = config.ApiUrl,
                HttpMethod = config.HttpMethod,
                AuthType = config.AuthType,
                AuthKeyOrUsername = config.AuthKeyOrUsername,
                AuthValueOrPassword = config.AuthValueOrPassword,
                FieldMappings = config.FieldMappings.Select(m => new FieldMapping
                {
                    SourcePath = m.SourcePath,
                    TargetProperty = m.TargetProperty,
                    Transformation = m.Transformation,
                    DefaultValue = m.DefaultValue
                }).ToList(),
                IdFieldMapping = config.IdFieldMapping,
                LanguageRouting = config.LanguageRouting,
                JsonPath = config.JsonPath,
                CustomHeaders = new Dictionary<string, string>(config.CustomHeaders),
                IsActive = config.IsActive
            };

            TestConnectionResult = null;
            TestConnectionSample = null;
            TestConnectionSuccess = false;
            ImportPreviewItems.Clear();
            ImportPreviewWarnings.Clear();
            ShowImportPreview = false;
            LastImportResult = null;
            ClearMessages();
            StateHasChanged();
        }

        protected void CancelEditImportConfig()
        {
            EditingImportConfig = null;
            TestConnectionResult = null;
            TestConnectionSample = null;
            ImportPreviewItems.Clear();
            ImportPreviewWarnings.Clear();
            ShowImportPreview = false;
            LastImportResult = null;
            StateHasChanged();
        }

        protected async Task TestConnection()
        {
            if (EditingImportConfig == null || string.IsNullOrWhiteSpace(EditingImportConfig.ApiUrl))
            {
                SetErrorMessage("Please enter an API URL.");
                return;
            }

            IsTestingConnection = true;
            TestConnectionResult = null;
            TestConnectionSample = null;
            TestConnectionSuccess = false;
            StateHasChanged();

            try
            {
                // Debug: Log the values being sent
                System.Diagnostics.Debug.WriteLine($"[TestConnection] ApiUrl: '{EditingImportConfig.ApiUrl}'");
                System.Diagnostics.Debug.WriteLine($"[TestConnection] JsonPath: '{EditingImportConfig.JsonPath}'");

                var (success, message, sample) = await ImportService.TestConnectionAsync(EditingImportConfig);
                TestConnectionSuccess = success;
                TestConnectionResult = message;
                TestConnectionSample = sample;

                if (!success)
                {
                    SetErrorMessage($"Connection test failed: {message}");
                }
                else
                {
                    SetSuccessMessage("Connection successful!");
                }
            }
            catch (Exception ex)
            {
                TestConnectionSuccess = false;
                TestConnectionResult = $"Error: {ex.Message}";
                SetErrorMessage($"Connection test error: {ex.Message}");
            }
            finally
            {
                IsTestingConnection = false;
                StateHasChanged();
            }
        }

        protected async Task PreviewImport()
        {
            if (EditingImportConfig == null || SelectedContentType == null)
            {
                SetErrorMessage("Please configure the import settings first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingImportConfig.IdFieldMapping))
            {
                SetErrorMessage("Please specify the ID field mapping.");
                return;
            }

            IsImporting = true;
            ImportPreviewItems.Clear();
            ImportPreviewWarnings.Clear();
            ImportNdJsonPreview = string.Empty;
            ShowNdJsonPreview = false;
            StateHasChanged();

            try
            {
                var (items, warnings) = await ImportService.PreviewImportAsync(
                    EditingImportConfig,
                    SelectedContentType);

                ImportPreviewItems = items.Take(20).ToList();
                ImportPreviewWarnings = warnings;
                ShowImportPreview = true;

                // Generate NdJSON preview from the items
                if (ImportPreviewItems.Any())
                {
                    ImportNdJsonPreview = GenerateNdJsonPreview(ImportPreviewItems.Take(5).ToList());
                    SetSuccessMessage($"Preview loaded: {ImportPreviewItems.Count} items (showing first 20)");
                }
                else
                {
                    SetErrorMessage("No items could be mapped from the external data. Check your field mappings.");
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Preview error: {ex.Message}");
            }
            finally
            {
                IsImporting = false;
                StateHasChanged();
            }
        }

        protected void ToggleNdJsonPreview()
        {
            ShowNdJsonPreview = !ShowNdJsonPreview;
            StateHasChanged();
        }

        private string GenerateNdJsonPreview(List<CustomDataItemModel> items)
        {
            if (SelectedContentType == null || string.IsNullOrEmpty(CurrentSourceId))
            {
                return string.Empty;
            }

            try
            {
                // Set the content type on items if not already set
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.ContentType))
                    {
                        item.ContentType = SelectedContentType.Name;
                    }
                }
                return NdJsonBuilder.BuildNdJson(items);
            }
            catch (Exception ex)
            {
                return $"Error generating NdJSON: {ex.Message}";
            }
        }

        protected void ShowImportConfirmationDialog()
        {
            ShowImportConfirmation = true;
            StateHasChanged();
        }

        protected void CancelImportConfirmation()
        {
            ShowImportConfirmation = false;
            StateHasChanged();
        }

        protected async Task ExecuteImport()
        {
            if (EditingImportConfig == null || SelectedContentType == null)
            {
                return;
            }

            ShowImportConfirmation = false;
            IsImporting = true;
            StateHasChanged();

            try
            {
                LastImportResult = await ImportService.ExecuteImportAsync(
                    EditingImportConfig,
                    SelectedContentType,
                    CurrentSourceId);

                if (LastImportResult.Success)
                {
                    SetSuccessMessage($"Import completed: {LastImportResult.ItemsImported} items imported.");

                    // Update the config with last import stats if it's a saved config
                    if (EditingImportConfig.Id.HasValue)
                    {
                        var entity = await ImportConfigRepository.GetByIdAsync(EditingImportConfig.Id.Value);
                        if (entity != null)
                        {
                            entity.LastImportAt = DateTime.UtcNow;
                            entity.LastImportCount = LastImportResult.ItemsImported;
                            await ImportConfigRepository.UpdateAsync(entity);
                            await LoadImportConfigurations();
                        }
                    }

                    // Reload data from Graph
                    await LoadDataFromGraph();
                }
                else
                {
                    SetErrorMessage($"Import failed: {string.Join(", ", LastImportResult.Errors)}");
                }

                if (LastImportResult.Warnings.Any())
                {
                    DebugInfo = $"Import Warnings:\n{string.Join("\n", LastImportResult.Warnings)}";
                }

                if (!string.IsNullOrEmpty(LastImportResult.DebugInfo))
                {
                    DebugInfo += $"\n\n{LastImportResult.DebugInfo}";
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Import error: {ex.Message}");
            }
            finally
            {
                IsImporting = false;
                StateHasChanged();
            }
        }

        protected async Task SaveImportConfiguration()
        {
            if (EditingImportConfig == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingImportConfig.Name))
            {
                SetErrorMessage("Please enter a name for this import configuration.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingImportConfig.ApiUrl))
            {
                SetErrorMessage("Please enter an API URL.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingImportConfig.IdFieldMapping))
            {
                SetErrorMessage("Please specify the ID field mapping.");
                return;
            }

            IsLoading = true;
            StateHasChanged();

            try
            {
                var entity = MapModelToEntity(EditingImportConfig);

                if (EditingImportConfig.Id.HasValue)
                {
                    entity.Id = EditingImportConfig.Id.Value;
                    await ImportConfigRepository.UpdateAsync(entity);
                    SetSuccessMessage("Import configuration updated.");
                }
                else
                {
                    var created = await ImportConfigRepository.CreateAsync(entity);
                    EditingImportConfig.Id = created.Id;
                    SetSuccessMessage("Import configuration saved.");
                }

                // Return to the list view
                EditingImportConfig = null;
                TestConnectionResult = null;
                TestConnectionSample = null;
                ImportPreviewItems.Clear();
                ImportPreviewWarnings.Clear();
                ShowImportPreview = false;
                LastImportResult = null;

                await LoadImportConfigurations();
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error saving configuration: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected void ShowDeleteImportConfigDialog(ImportConfigurationModel config)
        {
            ImportConfigToDelete = config;
            ShowDeleteImportConfigConfirmation = true;
            StateHasChanged();
        }

        protected void CancelDeleteImportConfig()
        {
            ImportConfigToDelete = null;
            ShowDeleteImportConfigConfirmation = false;
            StateHasChanged();
        }

        protected async Task ConfirmDeleteImportConfiguration()
        {
            if (ImportConfigToDelete?.Id == null)
            {
                return;
            }

            ShowDeleteImportConfigConfirmation = false;
            var configName = ImportConfigToDelete.Name;

            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ImportConfigRepository.DeleteAsync(ImportConfigToDelete.Id.Value);
                SetSuccessMessage($"Import configuration '{configName}' deleted.");
                ImportConfigToDelete = null;
                await LoadImportConfigurations();
            }, "deleting import configuration");
        }

        protected void ShowRunImportConfigDialog(ImportConfigurationModel config)
        {
            ImportConfigToRun = config;
            ShowRunImportConfigConfirmation = true;
            StateHasChanged();
        }

        protected void CancelRunImportConfig()
        {
            ImportConfigToRun = null;
            ShowRunImportConfigConfirmation = false;
            StateHasChanged();
        }

        protected async Task ConfirmRunImportConfiguration()
        {
            if (ImportConfigToRun == null)
            {
                return;
            }

            ShowRunImportConfigConfirmation = false;
            var configToRun = ImportConfigToRun;
            ImportConfigToRun = null;

            await RunSavedImport(configToRun);
        }

        protected async Task RunSavedImport(ImportConfigurationModel config)
        {
            // Find the content type schema for this config
            var contentType = CurrentSchema.ContentTypes.FirstOrDefault(ct =>
                string.Equals(ct.Name, config.TargetContentType, StringComparison.OrdinalIgnoreCase));

            if (contentType == null)
            {
                SetErrorMessage($"Content type '{config.TargetContentType}' not found in schema.");
                return;
            }

            IsImporting = true;
            StateHasChanged();

            try
            {
                var result = await ImportService.ExecuteImportAsync(config, contentType, CurrentSourceId);

                if (result.Success)
                {
                    SetSuccessMessage($"Import '{config.Name}' completed: {result.ItemsImported} items imported.");

                    // Update the config with last import stats
                    if (config.Id.HasValue)
                    {
                        var entity = await ImportConfigRepository.GetByIdAsync(config.Id.Value);
                        if (entity != null)
                        {
                            entity.LastImportAt = DateTime.UtcNow;
                            entity.LastImportCount = result.ItemsImported;
                            await ImportConfigRepository.UpdateAsync(entity);
                            await LoadImportConfigurations();
                        }
                    }

                    // Reload data from Graph
                    await LoadDataFromGraph();
                }
                else
                {
                    SetErrorMessage($"Import '{config.Name}' failed: {string.Join(", ", result.Errors)}");
                }

                if (result.Warnings.Any())
                {
                    DebugInfo = $"Import Warnings:\n{string.Join("\n", result.Warnings)}";
                }

                if (!string.IsNullOrEmpty(result.DebugInfo))
                {
                    DebugInfo += $"\n\n{result.DebugInfo}";
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Import error: {ex.Message}");
            }
            finally
            {
                IsImporting = false;
                StateHasChanged();
            }
        }

        protected void AddFieldMapping()
        {
            EditingImportConfig?.FieldMappings.Add(new FieldMapping());
            StateHasChanged();
        }

        protected void RemoveFieldMapping(FieldMapping mapping)
        {
            EditingImportConfig?.FieldMappings.Remove(mapping);
            StateHasChanged();
        }

        protected void AddCustomHeader()
        {
            if (EditingImportConfig != null)
            {
                var headerCount = EditingImportConfig.CustomHeaders.Count;
                EditingImportConfig.CustomHeaders[$"Header{headerCount + 1}"] = string.Empty;
                StateHasChanged();
            }
        }

        protected void RemoveCustomHeader(string key)
        {
            EditingImportConfig?.CustomHeaders.Remove(key);
            StateHasChanged();
        }

        private ImportConfigurationModel MapEntityToModel(ImportConfiguration entity)
        {
            var model = new ImportConfigurationModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                TargetSourceId = entity.TargetSourceId,
                TargetContentType = entity.TargetContentType,
                ApiUrl = entity.ApiUrl,
                HttpMethod = entity.HttpMethod,
                AuthType = entity.AuthType,
                AuthKeyOrUsername = entity.AuthKeyOrUsername,
                AuthValueOrPassword = entity.AuthValueOrPassword,
                IdFieldMapping = entity.IdFieldMapping,
                LanguageRouting = entity.LanguageRouting,
                JsonPath = entity.JsonPath,
                IsActive = entity.IsActive,
                LastImportAt = entity.LastImportAt,
                LastImportCount = entity.LastImportCount
            };

            // Deserialize field mappings
            if (!string.IsNullOrEmpty(entity.FieldMappingsJson))
            {
                try
                {
                    model.FieldMappings = JsonSerializer.Deserialize<List<FieldMapping>>(entity.FieldMappingsJson)
                        ?? new List<FieldMapping>();
                }
                catch
                {
                    model.FieldMappings = new List<FieldMapping>();
                }
            }

            // Deserialize custom headers
            if (!string.IsNullOrEmpty(entity.CustomHeadersJson))
            {
                try
                {
                    model.CustomHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.CustomHeadersJson)
                        ?? new Dictionary<string, string>();
                }
                catch
                {
                    model.CustomHeaders = new Dictionary<string, string>();
                }
            }

            return model;
        }

        private ImportConfiguration MapModelToEntity(ImportConfigurationModel model)
        {
            return new ImportConfiguration
            {
                Name = model.Name,
                Description = model.Description,
                TargetSourceId = model.TargetSourceId,
                TargetContentType = model.TargetContentType,
                ApiUrl = model.ApiUrl,
                HttpMethod = model.HttpMethod,
                AuthType = model.AuthType,
                AuthKeyOrUsername = model.AuthKeyOrUsername,
                AuthValueOrPassword = model.AuthValueOrPassword,
                FieldMappingsJson = JsonSerializer.Serialize(model.FieldMappings),
                IdFieldMapping = model.IdFieldMapping,
                LanguageRouting = model.LanguageRouting,
                JsonPath = model.JsonPath,
                CustomHeadersJson = JsonSerializer.Serialize(model.CustomHeaders),
                IsActive = model.IsActive,
                LastImportAt = model.LastImportAt,
                LastImportCount = model.LastImportCount
            };
        }

        /// <summary>
        /// Converts a PascalCase string to camelCase.
        /// </summary>
        private static string ToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Length == 1)
            {
                return value.ToLowerInvariant();
            }

            return char.ToLowerInvariant(value[0]) + value[1..];
        }

        #endregion
    }
}
