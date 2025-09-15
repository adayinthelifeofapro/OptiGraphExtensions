using Microsoft.AspNetCore.Components;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults
{
    public class PinnedResultsManagementComponentBase : ManagementComponentBase<PinnedResult, PinnedResultModel>
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

        // Collections Management
        protected IList<PinnedResultsCollection> Collections { get; set; } = new List<PinnedResultsCollection>();
        protected PinnedResultsCollectionModel NewCollection { get; set; } = new();

        // Pinned Results Management
        protected PaginationResult<PinnedResult>? PaginationResult { get; set; }
        protected IList<PinnedResult> AllPinnedResults { get; set; } = new List<PinnedResult>();
        protected PinnedResultModel NewPinnedResult { get; set; } = new();
        protected PinnedResultModel EditingPinnedResult { get; set; } = new();
        protected bool IsEditingPinnedResult { get; set; }
        protected Guid? SelectedCollectionId { get; set; }

        // State Management
        protected bool IsSyncing { get; set; }

        // Pagination
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<PinnedResult> PinnedResults => PaginationResult?.Items?.ToList() ?? new List<PinnedResult>();

        protected override async Task LoadDataAsync()
        {
            await LoadCollections();
        }

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
                UpdatePaginatedPinnedResults();
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
        }

        protected void CancelEditPinnedResult()
        {
            IsEditingPinnedResult = false;
            EditingPinnedResult = new PinnedResultModel();
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
            PaginationResult = PaginationService.GetPage(AllPinnedResults, CurrentPage, PageSize);
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
    }
}