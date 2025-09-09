using Microsoft.AspNetCore.Components;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults
{
    public class PinnedResultsManagementComponentBase : ComponentBase
    {
        [Inject]
        protected IPinnedResultsApiService ApiService { get; set; } = null!;

        [Inject]
        protected IPaginationService<PinnedResult> PaginationService { get; set; } = null!;

        [Inject]
        protected IPinnedResultsValidationService ValidationService { get; set; } = null!;

        protected IList<PinnedResultsCollection> Collections { get; set; } = new List<PinnedResultsCollection>();
        protected PinnedResultsCollectionModel NewCollection { get; set; } = new();
        protected PinnedResultsCollectionModel EditingCollection { get; set; } = new();
        protected bool IsEditingCollection { get; set; }

        protected PaginationResult<PinnedResult>? PaginationResult { get; set; }
        protected IList<PinnedResult> AllPinnedResults { get; set; } = new List<PinnedResult>();
        protected PinnedResultModel NewPinnedResult { get; set; } = new();
        protected PinnedResultModel EditingPinnedResult { get; set; } = new();
        protected bool IsEditingPinnedResult { get; set; }
        protected Guid? SelectedCollectionId { get; set; }

        protected bool IsLoading { get; set; }
        protected bool IsSyncing { get; set; }
        protected string? ErrorMessage { get; set; }
        protected string? SuccessMessage { get; set; }

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<PinnedResult> PinnedResults => PaginationResult?.Items?.ToList() ?? new List<PinnedResult>();

        protected override async Task OnInitializedAsync()
        {
            await LoadCollections();
        }

        #region Collections Management

        protected async Task LoadCollections()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                Collections = await ApiService.GetCollectionsAsync();
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authenticated. Please log in to access collections.";
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error loading collections: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task CreateCollection()
        {
            try
            {
                var validationResult = ValidationService.ValidateCollection(NewCollection);
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    return;
                }

                var request = new CreatePinnedResultsCollectionRequest
                {
                    Title = NewCollection.Title?.Trim(),
                    IsActive = NewCollection.IsActive
                };

                var createdCollection = await ApiService.CreateCollectionAsync(request);
                
                try
                {
                    await ApiService.SyncCollectionToOptimizelyGraphAsync(createdCollection);
                    SuccessMessage = "Collection created successfully and synced to Optimizely Graph.";
                }
                catch
                {
                    SuccessMessage = "Collection created successfully, but sync to Optimizely Graph failed.";
                }
                
                NewCollection = new PinnedResultsCollectionModel();
                ErrorMessage = null;
                await LoadCollections();
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authorized to create collections. Please ensure you are logged in and have the required permissions.";
                SuccessMessage = null;
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error creating collection: {ex.Message}";
                SuccessMessage = null;
            }
        }

        protected void StartEditCollection(PinnedResultsCollection collection)
        {
            IsEditingCollection = true;
            EditingCollection = new PinnedResultsCollectionModel
            {
                Id = collection.Id,
                Title = collection.Title,
                IsActive = collection.IsActive
            };
        }

        protected async Task UpdateCollection()
        {
            try
            {
                var validationResult = ValidationService.ValidateCollection(EditingCollection);
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    return;
                }

                var request = new UpdatePinnedResultsCollectionRequest
                {
                    Title = EditingCollection.Title?.Trim(),
                    IsActive = EditingCollection.IsActive
                };

                await ApiService.UpdateCollectionAsync(EditingCollection.Id, request);

                SuccessMessage = "Collection updated successfully.";
                ErrorMessage = null;
                CancelEditCollection();
                await LoadCollections();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error updating collection: {ex.Message}";
                SuccessMessage = null;
            }
        }

        protected void CancelEditCollection()
        {
            IsEditingCollection = false;
            EditingCollection = new PinnedResultsCollectionModel();
        }

        protected async Task DeleteCollection(Guid id)
        {
            try
            {
                await ApiService.DeleteCollectionAsync(id);

                SuccessMessage = "Collection deleted successfully.";
                ErrorMessage = null;
                
                if (SelectedCollectionId == id)
                {
                    SelectedCollectionId = null;
                    AllPinnedResults.Clear();
                    PaginationResult = null;
                }
                
                await LoadCollections();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error deleting collection: {ex.Message}";
                SuccessMessage = null;
            }
        }

        #endregion

        #region Pinned Results Management

        protected async Task OnCollectionChanged(ChangeEventArgs args)
        {
            if (Guid.TryParse(args.Value?.ToString(), out var collectionId))
            {
                SelectedCollectionId = collectionId;
                NewPinnedResult.CollectionId = collectionId;
                await LoadPinnedResults();
            }
            else
            {
                SelectedCollectionId = null;
                AllPinnedResults.Clear();
                PaginationResult = null;
            }
        }

        protected async Task LoadPinnedResults()
        {
            if (!SelectedCollectionId.HasValue) return;

            try
            {
                IsLoading = true;
                ClearMessages();

                AllPinnedResults = await ApiService.GetPinnedResultsAsync(SelectedCollectionId.Value);
                UpdatePaginatedPinnedResults();
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authenticated. Please log in to access pinned results.";
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error loading pinned results: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task CreatePinnedResult()
        {
            try
            {
                if (!SelectedCollectionId.HasValue)
                {
                    ErrorMessage = "Please select a collection first.";
                    return;
                }

                NewPinnedResult.CollectionId = SelectedCollectionId.Value;
                var validationResult = ValidationService.ValidatePinnedResult(NewPinnedResult);
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    return;
                }

                var request = new CreatePinnedResultRequest
                {
                    CollectionId = SelectedCollectionId.Value,
                    Phrases = NewPinnedResult.Phrases?.Trim(),
                    TargetKey = NewPinnedResult.TargetKey?.Trim(),
                    Language = NewPinnedResult.Language?.Trim(),
                    Priority = NewPinnedResult.Priority,
                    IsActive = NewPinnedResult.IsActive
                };

                await ApiService.CreatePinnedResultAsync(request);
                
                NewPinnedResult = new PinnedResultModel { CollectionId = SelectedCollectionId.Value };
                SuccessMessage = "Pinned result created successfully.";
                ErrorMessage = null;
                await LoadPinnedResults();
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authorized to create pinned results. Please ensure you are logged in and have the required permissions.";
                SuccessMessage = null;
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error creating pinned result: {ex.Message}";
                SuccessMessage = null;
            }
        }

        protected void StartEditPinnedResult(PinnedResult pinnedResult)
        {
            IsEditingPinnedResult = true;
            EditingPinnedResult = new PinnedResultModel
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

        protected async Task UpdatePinnedResult()
        {
            try
            {
                var validationResult = ValidationService.ValidatePinnedResult(EditingPinnedResult);
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    return;
                }

                var request = new UpdatePinnedResultRequest
                {
                    Phrases = EditingPinnedResult.Phrases?.Trim(),
                    TargetKey = EditingPinnedResult.TargetKey?.Trim(),
                    Language = EditingPinnedResult.Language?.Trim(),
                    Priority = EditingPinnedResult.Priority,
                    IsActive = EditingPinnedResult.IsActive
                };

                await ApiService.UpdatePinnedResultAsync(EditingPinnedResult.Id, request);

                SuccessMessage = "Pinned result updated successfully.";
                ErrorMessage = null;
                CancelEditPinnedResult();
                await LoadPinnedResults();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error updating pinned result: {ex.Message}";
                SuccessMessage = null;
            }
        }

        protected void CancelEditPinnedResult()
        {
            IsEditingPinnedResult = false;
            EditingPinnedResult = new PinnedResultModel();
        }

        protected async Task DeletePinnedResult(Guid id)
        {
            try
            {
                await ApiService.DeletePinnedResultAsync(id);
                
                SuccessMessage = "Pinned result deleted successfully.";
                ErrorMessage = null;
                await LoadPinnedResults();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
                SuccessMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error deleting pinned result: {ex.Message}";
                SuccessMessage = null;
            }
        }

        #endregion

        #region Utility Methods

        protected void ClearMessages()
        {
            ErrorMessage = null;
            SuccessMessage = null;
        }

        protected async Task<bool> ConfirmDelete()
        {
            // For now, we'll return true. In a production app, you'd want to implement
            // a proper confirmation dialog using JavaScript interop or a modal component
            return await Task.FromResult(true);
        }

        protected void UpdatePaginatedPinnedResults()
        {
            PaginationResult = PaginationService.GetPage(AllPinnedResults, CurrentPage, PageSize);
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            if (PaginationResult != null && page >= 1 && page <= PaginationResult.TotalPages)
            {
                CurrentPage = page;
                UpdatePaginatedPinnedResults();
            }
        }

        protected void GoToPreviousPage()
        {
            if (PaginationResult?.HasPreviousPage == true)
            {
                CurrentPage--;
                UpdatePaginatedPinnedResults();
            }
        }

        protected void GoToNextPage()
        {
            if (PaginationResult?.HasNextPage == true)
            {
                CurrentPage++;
                UpdatePaginatedPinnedResults();
            }
        }

        #endregion

        #region Optimizely Graph Synchronization

        protected async Task SyncPinnedResultsToOptimizelyGraph()
        {
            try
            {
                if (!SelectedCollectionId.HasValue)
                {
                    ErrorMessage = "Please select a collection first.";
                    return;
                }

                IsSyncing = true;
                ClearMessages();

                await ApiService.SyncPinnedResultsToOptimizelyGraphAsync(SelectedCollectionId.Value);
                
                SuccessMessage = "Successfully synced pinned results to Optimizely Graph.";
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authenticated. Please log in to sync pinned results.";
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error syncing pinned results: {ex.Message}";
            }
            finally
            {
                IsSyncing = false;
            }
        }

        #endregion
    }
}