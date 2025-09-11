using Microsoft.AspNetCore.Components;

using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms
{
    public class SynonymManagementComponentBase : ComponentBase
    {
        [Inject]
        protected ISynonymApiService SynonymApiService { get; set; } = null!;

        [Inject]
        protected IPaginationService<Synonym> PaginationService { get; set; } = null!;

        [Inject]
        protected ISynonymValidationService ValidationService { get; set; } = null!;

        protected PaginationResult<Synonym>? PaginationResult { get; set; }
        protected IList<Synonym> AllSynonyms { get; set; } = new List<Synonym>();
        protected SynonymModel NewSynonym { get; set; } = new();
        protected SynonymModel EditingSynonym { get; set; } = new();
        protected bool IsEditing { get; set; }
        protected bool IsLoading { get; set; }
        protected bool IsSyncing { get; set; }
        protected string? ErrorMessage { get; set; }
        protected string? SuccessMessage { get; set; }

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<Synonym> Synonyms => PaginationResult?.Items?.ToList() ?? new List<Synonym>();

        protected override async Task OnInitializedAsync()
        {
            await LoadSynonyms();
        }

        protected async Task LoadSynonyms()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                AllSynonyms = await SynonymApiService.GetSynonymsAsync();
                UpdatePaginatedSynonyms();
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authenticated. Please log in to access synonyms.";
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
                ErrorMessage = $"Unexpected error loading synonyms: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task CreateSynonym()
        {
            try
            {
                var validationResult = ValidationService.ValidateSynonym(NewSynonym);
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    return;
                }

                var request = new CreateSynonymRequest
                {
                    Synonym = NewSynonym.Synonym?.Trim()
                };

                await SynonymApiService.CreateSynonymAsync(request);
                
                NewSynonym = new SynonymModel();
                SuccessMessage = "Synonym created successfully.";
                ErrorMessage = null;
                await LoadSynonyms();
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authorized to create synonyms. Please ensure you are logged in and have the required permissions.";
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
                ErrorMessage = $"Unexpected error creating synonym: {ex.Message}";
                SuccessMessage = null;
            }
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
            try
            {
                var validationResult = ValidationService.ValidateSynonym(EditingSynonym);
                if (!validationResult.IsValid)
                {
                    ErrorMessage = validationResult.ErrorMessage;
                    return;
                }

                var request = new UpdateSynonymRequest
                {
                    Synonym = EditingSynonym.Synonym?.Trim()
                };

                await SynonymApiService.UpdateSynonymAsync(EditingSynonym.Id, request);

                SuccessMessage = "Synonym updated successfully.";
                ErrorMessage = null;
                CancelEdit();
                await LoadSynonyms();
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
                ErrorMessage = $"Unexpected error updating synonym: {ex.Message}";
                SuccessMessage = null;
            }
        }

        protected void CancelEdit()
        {
            IsEditing = false;
            EditingSynonym = new SynonymModel();
        }

        protected async Task DeleteSynonym(Guid id)
        {
            try
            {
                await SynonymApiService.DeleteSynonymAsync(id);
                
                SuccessMessage = "Synonym deleted successfully.";
                ErrorMessage = null;
                await LoadSynonyms();
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
                ErrorMessage = $"Unexpected error deleting synonym: {ex.Message}";
                SuccessMessage = null;
            }
        }

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

        protected async Task SyncSynonymsToOptimizelyGraph()
        {
            try
            {
                IsSyncing = true;
                ClearMessages();

                await SynonymApiService.SyncSynonymsToOptimizelyGraphAsync();
                
                SuccessMessage = "Successfully synced synonyms to Optimizely Graph.";
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "You are not authenticated. Please log in to sync synonyms.";
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
                ErrorMessage = $"Unexpected error syncing synonyms: {ex.Message}";
            }
            finally
            {
                IsSyncing = false;
            }
        }


        protected void UpdatePaginatedSynonyms()
        {
            PaginationResult = PaginationService.GetPage(AllSynonyms, CurrentPage, PageSize);
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            if (PaginationResult != null && page >= 1 && page <= PaginationResult.TotalPages)
            {
                CurrentPage = page;
                UpdatePaginatedSynonyms();
            }
        }

        protected void GoToPreviousPage()
        {
            if (PaginationResult?.HasPreviousPage == true)
            {
                CurrentPage--;
                UpdatePaginatedSynonyms();
            }
        }

        protected void GoToNextPage()
        {
            if (PaginationResult?.HasNextPage == true)
            {
                CurrentPage++;
                UpdatePaginatedSynonyms();
            }
        }

    }
}