using Microsoft.AspNetCore.Components;

using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms
{
    public class SynonymManagementComponentBase : ManagementComponentBase<Synonym, SynonymModel>
    {
        [Inject]
        protected ISynonymApiService SynonymApiService { get; set; } = null!;

        [Inject]
        protected IPaginationService<Synonym> PaginationService { get; set; } = null!;

        [Inject]
        protected ISynonymValidationService ValidationService { get; set; } = null!;

        [Inject]
        protected IRequestMapper<SynonymModel, CreateSynonymRequest, UpdateSynonymRequest> RequestMapper { get; set; } = null!;

        protected PaginationResult<Synonym>? PaginationResult { get; set; }
        protected IList<Synonym> AllSynonyms { get; set; } = new List<Synonym>();
        protected SynonymModel NewSynonym { get; set; } = new();
        protected SynonymModel EditingSynonym { get; set; } = new();
        protected bool IsEditing { get; set; }
        protected bool IsSyncing { get; set; }

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<Synonym> Synonyms => PaginationResult?.Items?.ToList() ?? new List<Synonym>();

        protected override async Task LoadDataAsync()
        {
            await LoadSynonyms();
        }

        protected async Task LoadSynonyms()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                AllSynonyms = await SynonymApiService.GetSynonymsAsync();
                UpdatePaginatedSynonyms();
            }, "loading synonyms");
        }

        protected async Task CreateSynonym()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidateModel(NewSynonym);
                var request = RequestMapper.MapToCreateRequest(NewSynonym);
                await SynonymApiService.CreateSynonymAsync(request);
                NewSynonym = new();
                SetSuccessMessage("Synonym created successfully.");
                await LoadSynonyms();
            }, "creating synonym", showLoading: false);
        }

        protected void StartEdit(Synonym synonym)
        {
            IsEditing = true;
            EditingSynonym = new SynonymModel
            {
                Id = synonym.Id,
                Synonym = synonym.SynonymItem
            };
        }

        protected async Task UpdateSynonym()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidateModel(EditingSynonym);
                var request = RequestMapper.MapToUpdateRequest(EditingSynonym);
                await SynonymApiService.UpdateSynonymAsync(EditingSynonym.Id, request);
                SetSuccessMessage("Synonym updated successfully.");
                CancelEdit();
                await LoadSynonyms();
            }, "updating synonym", showLoading: false);
        }

        protected void CancelEdit()
        {
            IsEditing = false;
            EditingSynonym = new SynonymModel();
        }

        protected async Task DeleteSynonym(Guid id)
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await SynonymApiService.DeleteSynonymAsync(id);
                SetSuccessMessage("Synonym deleted successfully.");
                await LoadSynonyms();
            }, "deleting synonym", showLoading: false);
        }


        protected async Task<bool> ConfirmDelete()
        {
            // For now, we'll return true. In a production app, you'd want to implement
            // a proper confirmation dialog using JavaScript interop or a modal component
            return await Task.FromResult(true);
        }

        protected async Task SyncSynonymsToOptimizelyGraph()
        {
            await ExecuteWithSyncHandlingAsync(async () =>
            {
                await SynonymApiService.SyncSynonymsToOptimizelyGraphAsync();
                SetSuccessMessage("Successfully synced synonyms to Optimizely Graph.");
            }, "syncing synonyms to Optimizely Graph");
        }


        protected void UpdatePaginatedSynonyms()
        {
            PaginationResult = PaginationService.GetPage(AllSynonyms, CurrentPage, PageSize);
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            NavigateToPage(page, CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedSynonymsAsync);
        }

        protected void GoToPreviousPage()
        {
            NavigateToPreviousPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedSynonymsAsync);
        }

        protected void GoToNextPage()
        {
            NavigateToNextPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedSynonymsAsync);
        }

        private Task UpdatePaginatedSynonymsAsync()
        {
            UpdatePaginatedSynonyms();
            return Task.CompletedTask;
        }

        private Task ValidateModel(SynonymModel model)
        {
            var validationResult = ValidationService.ValidateSynonym(model);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.ErrorMessage);
            }
            return Task.CompletedTask;
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

    }
}