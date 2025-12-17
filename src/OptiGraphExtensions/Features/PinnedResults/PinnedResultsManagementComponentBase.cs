using Microsoft.AspNetCore.Components;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;
using OptiGraphExtensions.Features.ContentSearch.Models;
using OptiGraphExtensions.Features.ContentSearch.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults
{
    public class PinnedResultsManagementComponentBase : ManagementComponentBase<PinnedResult, PinnedResultModel>, IDisposable
    {
        [Inject]
        protected IPinnedResultsApiService ApiService { get; set; } = null!;

        [Inject]
        protected IPinnedResultsCollectionService CollectionService { get; set; } = null!;

        [Inject]
        protected IPaginationService<PinnedResult> PaginationService { get; set; } = null!;

        [Inject]
        protected IPinnedResultsValidationService ValidationService { get; set; } = null!;

        [Inject]
        protected IRequestMapper<PinnedResultsCollectionModel, CreatePinnedResultsCollectionRequest, UpdatePinnedResultsCollectionRequest> CollectionRequestMapper { get; set; } = null!;

        [Inject]
        protected IRequestMapper<PinnedResultModel, CreatePinnedResultRequest, UpdatePinnedResultRequest> PinnedResultRequestMapper { get; set; } = null!;

        [Inject]
        protected ILanguageService LanguageService { get; set; } = null!;

        [Inject]
        protected IContentSearchService ContentSearchService { get; set; } = null!;

        // Languages
        protected IEnumerable<LanguageInfo> AvailableLanguages { get; set; } = Enumerable.Empty<LanguageInfo>();
        protected string SelectedLanguageFilter { get; set; } = string.Empty;

        // Collections Management
        protected IList<PinnedResultsCollection> Collections { get; set; } = new List<PinnedResultsCollection>();
        protected PinnedResultsCollectionModel NewCollection { get; set; } = new();

        // Pinned Results Management
        protected PaginationResult<PinnedResult>? PaginationResult { get; set; }
        protected IList<PinnedResult> AllPinnedResults { get; set; } = new List<PinnedResult>();
        protected IList<PinnedResult> FilteredPinnedResults { get; set; } = new List<PinnedResult>();
        protected PinnedResultModel NewPinnedResult { get; set; } = new();
        protected PinnedResultModel EditingPinnedResult { get; set; } = new();
        protected bool IsEditingPinnedResult { get; set; }
        protected Guid? SelectedCollectionId { get; set; }

        // State Management
        protected bool IsSyncing { get; set; }

        // Content Search Autocomplete State (New Pinned Result)
        protected string ContentSearchText { get; set; } = string.Empty;
        protected IList<ContentSearchResult> ContentSearchResults { get; set; } = new List<ContentSearchResult>();
        protected IList<string> AvailableContentTypes { get; set; } = new List<string>();
        protected string SelectedContentTypeFilter { get; set; } = string.Empty;
        protected bool IsSearching { get; set; }
        protected bool ShowSearchDropdown { get; set; }
        protected ContentSearchResult? SelectedContent { get; set; }

        // Content Search Autocomplete State (Editing Pinned Result)
        protected string EditingContentSearchText { get; set; } = string.Empty;
        protected IList<ContentSearchResult> EditingContentSearchResults { get; set; } = new List<ContentSearchResult>();
        protected bool IsEditingSearching { get; set; }
        protected bool ShowEditingSearchDropdown { get; set; }
        protected ContentSearchResult? EditingSelectedContent { get; set; }

        // Debounce timers for search
        private System.Timers.Timer? _searchDebounceTimer;
        private System.Timers.Timer? _editingSearchDebounceTimer;
        private const int DebounceDelayMs = 300;
        private bool _disposed;

        // Pagination
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<PinnedResult> PinnedResults => PaginationResult?.Items?.ToList() ?? new List<PinnedResult>();

        protected override async Task LoadDataAsync()
        {
            LoadLanguages();
            await LoadCollections();
            await LoadContentTypesAsync();
        }

        protected void LoadLanguages()
        {
            AvailableLanguages = LanguageService.GetEnabledLanguages();

            // Set default language for new pinned result if we have languages available
            if (AvailableLanguages.Any() && string.IsNullOrEmpty(NewPinnedResult.Language))
            {
                NewPinnedResult.Language = AvailableLanguages.First().LanguageCode;
            }
        }

        protected string GetLanguageDisplayName(string? languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return "Unknown";
            }

            return AvailableLanguages.FirstOrDefault(l => l.LanguageCode == languageCode)?.DisplayName ?? languageCode;
        }

        protected void ApplyLanguageFilter()
        {
            if (string.IsNullOrEmpty(SelectedLanguageFilter))
            {
                FilteredPinnedResults = AllPinnedResults;
            }
            else
            {
                FilteredPinnedResults = AllPinnedResults.Where(p => p.Language == SelectedLanguageFilter).ToList();
            }

            CurrentPage = 1;
            UpdatePaginatedPinnedResults();
        }

        protected void OnLanguageFilterChanged(ChangeEventArgs e)
        {
            SelectedLanguageFilter = e.Value?.ToString() ?? string.Empty;
            ApplyLanguageFilter();
        }

        #region Content Search Autocomplete

        protected async Task LoadContentTypesAsync()
        {
            try
            {
                AvailableContentTypes = await ContentSearchService.GetAvailableContentTypesAsync();
            }
            catch
            {
                // Content types are optional - don't fail if we can't load them
                AvailableContentTypes = new List<string>();
            }
        }

        protected void OnContentSearchInput(ChangeEventArgs e)
        {
            ContentSearchText = e.Value?.ToString() ?? string.Empty;

            // Clear selection if user is typing
            SelectedContent = null;
            NewPinnedResult.TargetKey = string.Empty;

            // Cancel previous timer
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer?.Dispose();

            if (string.IsNullOrWhiteSpace(ContentSearchText) || ContentSearchText.Length < 2)
            {
                ContentSearchResults = new List<ContentSearchResult>();
                ShowSearchDropdown = false;
                StateHasChanged();
                return;
            }

            // Start new debounce timer
            _searchDebounceTimer = new System.Timers.Timer(DebounceDelayMs);
            _searchDebounceTimer.Elapsed += async (sender, args) =>
            {
                _searchDebounceTimer?.Stop();
                await InvokeAsync(async () => await PerformContentSearchAsync());
            };
            _searchDebounceTimer.AutoReset = false;
            _searchDebounceTimer.Start();
        }

        protected void OnEditingContentSearchInput(ChangeEventArgs e)
        {
            EditingContentSearchText = e.Value?.ToString() ?? string.Empty;

            EditingSelectedContent = null;
            EditingPinnedResult.TargetKey = string.Empty;

            _editingSearchDebounceTimer?.Stop();
            _editingSearchDebounceTimer?.Dispose();

            if (string.IsNullOrWhiteSpace(EditingContentSearchText) || EditingContentSearchText.Length < 2)
            {
                EditingContentSearchResults = new List<ContentSearchResult>();
                ShowEditingSearchDropdown = false;
                StateHasChanged();
                return;
            }

            _editingSearchDebounceTimer = new System.Timers.Timer(DebounceDelayMs);
            _editingSearchDebounceTimer.Elapsed += async (sender, args) =>
            {
                _editingSearchDebounceTimer?.Stop();
                await InvokeAsync(async () => await PerformEditingContentSearchAsync());
            };
            _editingSearchDebounceTimer.AutoReset = false;
            _editingSearchDebounceTimer.Start();
        }

        protected async Task PerformContentSearchAsync()
        {
            try
            {
                IsSearching = true;
                StateHasChanged();

                ContentSearchResults = await ContentSearchService.SearchContentAsync(
                    ContentSearchText,
                    string.IsNullOrEmpty(SelectedContentTypeFilter) ? null : SelectedContentTypeFilter,
                    NewPinnedResult.Language,
                    10);

                ShowSearchDropdown = ContentSearchResults.Any();
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Search failed: {ex.Message}");
                ContentSearchResults = new List<ContentSearchResult>();
                ShowSearchDropdown = false;
            }
            finally
            {
                IsSearching = false;
                StateHasChanged();
            }
        }

        protected async Task PerformEditingContentSearchAsync()
        {
            try
            {
                IsEditingSearching = true;
                StateHasChanged();

                EditingContentSearchResults = await ContentSearchService.SearchContentAsync(
                    EditingContentSearchText,
                    string.IsNullOrEmpty(SelectedContentTypeFilter) ? null : SelectedContentTypeFilter,
                    EditingPinnedResult.Language,
                    10);

                ShowEditingSearchDropdown = EditingContentSearchResults.Any();
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Search failed: {ex.Message}");
                EditingContentSearchResults = new List<ContentSearchResult>();
                ShowEditingSearchDropdown = false;
            }
            finally
            {
                IsEditingSearching = false;
                StateHasChanged();
            }
        }

        protected void SelectContent(ContentSearchResult result)
        {
            SelectedContent = result;
            NewPinnedResult.TargetKey = result.GuidValue;
            ContentSearchText = result.DisplayText;
            ShowSearchDropdown = false;
            ContentSearchResults = new List<ContentSearchResult>();
            StateHasChanged();
        }

        protected void SelectEditingContent(ContentSearchResult result)
        {
            EditingSelectedContent = result;
            EditingPinnedResult.TargetKey = result.GuidValue;
            EditingContentSearchText = result.DisplayText;
            ShowEditingSearchDropdown = false;
            EditingContentSearchResults = new List<ContentSearchResult>();
            StateHasChanged();
        }

        protected void OnContentTypeFilterChanged(ChangeEventArgs e)
        {
            SelectedContentTypeFilter = e.Value?.ToString() ?? string.Empty;

            // Re-search if we have text
            if (!string.IsNullOrWhiteSpace(ContentSearchText) && ContentSearchText.Length >= 2)
            {
                _ = PerformContentSearchAsync();
            }
        }

        protected void ClearContentSelection()
        {
            SelectedContent = null;
            NewPinnedResult.TargetKey = string.Empty;
            ContentSearchText = string.Empty;
            ContentSearchResults = new List<ContentSearchResult>();
            ShowSearchDropdown = false;
            StateHasChanged();
        }

        protected void ClearEditingContentSelection()
        {
            EditingSelectedContent = null;
            EditingPinnedResult.TargetKey = string.Empty;
            EditingContentSearchText = string.Empty;
            EditingContentSearchResults = new List<ContentSearchResult>();
            ShowEditingSearchDropdown = false;
            StateHasChanged();
        }

        protected void CloseSearchDropdown()
        {
            ShowSearchDropdown = false;
            StateHasChanged();
        }

        protected void CloseEditingSearchDropdown()
        {
            ShowEditingSearchDropdown = false;
            StateHasChanged();
        }

        #endregion

        #region Collections Management

        protected async Task LoadCollections()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                Collections = await LoadCollectionsWithFallback();
            }, "loading collections");
        }

        protected async Task CreateCollection()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidateCollectionModel(NewCollection);
                var request = CollectionRequestMapper.MapToCreateRequest(NewCollection);
                var createdCollection = await ApiService.CreateCollectionAsync(request);
                await HandleCollectionSync(createdCollection);
                NewCollection = new();
                SetSuccessMessage("Collection created successfully.");
                await LoadCollections();
            }, "creating collection", showLoading: false);
        }


        protected async Task DeleteCollection(Guid id)
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ApiService.DeleteCollectionAsync(id);
                SetSuccessMessage("Collection deleted successfully.");
                await LoadCollections();
            }, "deleting collection", showLoading: false);
        }

        protected async Task ConfirmAndDeleteCollection(Guid id)
        {
            // In a real application, you might want to use a proper modal dialog service
            // For now, we'll directly call DeleteCollection
            // The confirmation can be handled by a JavaScript interop if needed
            await DeleteCollection(id);
        }


        protected async Task SyncCollectionsFromOptimizelyGraph()
        {
            await ExecuteWithSyncHandlingAsync(async () =>
            {
                // Use the CollectionService which properly updates the database
                var syncedCollections = await CollectionService.SyncCollectionsFromGraphAsync();
                var collectionCount = syncedCollections?.Count() ?? 0;
                SetSuccessMessage($"Successfully synced {collectionCount} collections from Optimizely Graph.");
                await LoadCollections();
            }, "syncing collections from Optimizely Graph");
        }

        #endregion

        #region Pinned Results Management

        protected async Task LoadPinnedResults()
        {
            if (!SelectedCollectionId.HasValue) return;

            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                AllPinnedResults = await ApiService.GetPinnedResultsAsync(SelectedCollectionId.Value);
                ApplyLanguageFilter();
            }, "loading pinned results");
        }

        protected async Task CreatePinnedResult()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                NewPinnedResult.CollectionId = SelectedCollectionId ?? Guid.Empty;
                await ValidatePinnedResultModel(NewPinnedResult);
                var request = PinnedResultRequestMapper.MapToCreateRequest(NewPinnedResult);
                await ApiService.CreatePinnedResultAsync(request);
                NewPinnedResult = new();
                // Reset content search state
                ClearContentSelection();
                SetSuccessMessage("Pinned result created successfully.");
                await LoadPinnedResults();
            }, "creating pinned result", showLoading: false);
        }

        protected async Task UpdatePinnedResult()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidatePinnedResultModel(EditingPinnedResult);
                var request = PinnedResultRequestMapper.MapToUpdateRequest(EditingPinnedResult);
                await ApiService.UpdatePinnedResultAsync(EditingPinnedResult.Id, request);
                SetSuccessMessage("Pinned result updated successfully.");
                CancelEditPinnedResult();
                await LoadPinnedResults();
            }, "updating pinned result", showLoading: false);
        }

        protected async Task DeletePinnedResult(Guid id)
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ApiService.DeletePinnedResultAsync(id);
                SetSuccessMessage("Pinned result deleted successfully.");
                await LoadPinnedResults();
            }, "deleting pinned result", showLoading: false);
        }

        protected void StartEditPinnedResult(PinnedResult pinnedResult)
        {
            IsEditingPinnedResult = true;
            EditingPinnedResult = MapToPinnedResultModel(pinnedResult);
            // Initialize editing search text with current target key
            EditingContentSearchText = pinnedResult.TargetKey ?? string.Empty;
            EditingSelectedContent = null;
        }

        protected void CancelEditPinnedResult()
        {
            IsEditingPinnedResult = false;
            EditingPinnedResult = new PinnedResultModel();
            // Clear editing search state
            ClearEditingContentSelection();
        }

        protected async Task OnCollectionChanged(ChangeEventArgs args)
        {
            if (Guid.TryParse(args.Value?.ToString(), out var collectionId))
            {
                SelectedCollectionId = collectionId;
                await LoadPinnedResults();
            }
            else
            {
                SelectedCollectionId = null;
                AllPinnedResults = new List<PinnedResult>();
                FilteredPinnedResults = new List<PinnedResult>();
                PaginationResult = null;
            }
        }

        protected async Task SyncPinnedResultsFromOptimizelyGraph()
        {
            if (!SelectedCollectionId.HasValue) return;

            await ExecuteWithSyncHandlingAsync(async () =>
            {
                await ApiService.SyncPinnedResultsFromOptimizelyGraphAsync(SelectedCollectionId.Value);
                SetSuccessMessage("Successfully synced pinned results from Optimizely Graph.");
                await LoadPinnedResults();
            }, "syncing pinned results from Optimizely Graph");
        }

        #endregion

        #region Pagination

        protected void UpdatePaginatedPinnedResults()
        {
            PaginationResult = PaginationService.GetPage(FilteredPinnedResults, CurrentPage, PageSize);
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            NavigateToPage(page, CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedPinnedResultsAsync);
        }

        protected void GoToPreviousPage()
        {
            NavigateToPreviousPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedPinnedResultsAsync);
        }

        protected void GoToNextPage()
        {
            NavigateToNextPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedPinnedResultsAsync);
        }

        protected void GoToFirstPage()
        {
            NavigateToFirstPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedPinnedResultsAsync);
        }

        protected void GoToLastPage()
        {
            NavigateToLastPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedPinnedResultsAsync);
        }

        private Task UpdatePaginatedPinnedResultsAsync()
        {
            UpdatePaginatedPinnedResults();
            return Task.CompletedTask;
        }

        #endregion

        #region Utility Methods

        protected Task<bool> ConfirmDelete()
        {
            // For now, we'll return true. In a production app, you'd want to implement
            // a proper confirmation dialog using JavaScript interop or a modal component
            return Task.FromResult(true);
        }

        #endregion

        #region Helper Methods

        private async Task<IList<PinnedResultsCollection>> LoadCollectionsWithFallback()
        {
            try
            {
                return await ApiService.GetCollectionsAsync();
            }
            catch
            {
                // Fallback to collection service if API service fails
                try
                {
                    return (await CollectionService.GetAllCollectionsAsync()).ToList();
                }
                catch
                {
                    return new List<PinnedResultsCollection>();
                }
            }
        }

        private async Task HandleCollectionSync(PinnedResultsCollection collection)
        {
            try
            {
                await ApiService.SyncCollectionToOptimizelyGraphAsync(collection);
            }
            catch
            {
                // Sync failure shouldn't prevent collection creation success
                SetSuccessMessage("Collection created successfully. Note: Sync to Optimizely Graph failed.");
            }
        }

        private Task ValidateCollectionModel(PinnedResultsCollectionModel model)
        {
            var validationResult = ValidationService.ValidateCollection(model);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.ErrorMessage);
            }
            return Task.CompletedTask;
        }

        private Task ValidatePinnedResultModel(PinnedResultModel model)
        {
            var validationResult = ValidationService.ValidatePinnedResult(model);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.ErrorMessage);
            }
            return Task.CompletedTask;
        }

        private static PinnedResultsCollectionModel MapToCollectionModel(PinnedResultsCollection collection)
        {
            return new PinnedResultsCollectionModel
            {
                Id = collection.Id,
                Title = collection.Title,
                IsActive = collection.IsActive
            };
        }

        private static PinnedResultModel MapToPinnedResultModel(PinnedResult pinnedResult)
        {
            return new PinnedResultModel
            {
                Id = pinnedResult.Id,
                CollectionId = pinnedResult.CollectionId,
                Phrases = pinnedResult.Phrases,
                TargetKey = pinnedResult.TargetKey,
                Language = pinnedResult.Language,
                Priority = pinnedResult.Priority,
                IsActive = pinnedResult.IsActive
            };
        }

        private async Task ExecuteWithSyncHandlingAsync(Func<Task> operation, string operationName)
        {
            try
            {
                IsSyncing = true;
                ClearMessages();
                StateHasChanged();

                await ErrorHandler.ExecuteWithErrorHandlingAsync(operation, operationName);
            }
            catch (ComponentException ex)
            {
                SetErrorMessage(ex.Message);
            }
            finally
            {
                IsSyncing = false;
                StateHasChanged();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _searchDebounceTimer?.Stop();
                _searchDebounceTimer?.Dispose();
                _editingSearchDebounceTimer?.Stop();
                _editingSearchDebounceTimer?.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}