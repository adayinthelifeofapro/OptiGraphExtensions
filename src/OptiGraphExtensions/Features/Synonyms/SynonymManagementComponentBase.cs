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
        protected ISynonymService SynonymService { get; set; } = null!;

        [Inject]
        protected ISynonymGraphSyncService GraphSyncService { get; set; } = null!;

        [Inject]
        protected IPaginationService<Synonym> PaginationService { get; set; } = null!;

        [Inject]
        protected ISynonymValidationService ValidationService { get; set; } = null!;

        [Inject]
        protected ILanguageService LanguageService { get; set; } = null!;

        protected PaginationResult<Synonym>? PaginationResult { get; set; }
        protected IList<Synonym> AllSynonyms { get; set; } = new List<Synonym>();
        protected IList<Synonym> FilteredSynonyms { get; set; } = new List<Synonym>();
        protected SynonymModel NewSynonym { get; set; } = new();
        protected SynonymModel EditingSynonym { get; set; } = new();
        protected bool IsEditing { get; set; }
        protected bool IsSyncing { get; set; }

        protected IEnumerable<LanguageInfo> AvailableLanguages { get; set; } = Enumerable.Empty<LanguageInfo>();
        protected string SelectedLanguageFilter { get; set; } = string.Empty;
        protected SynonymSlot? SelectedSlotFilter { get; set; }

        protected static IEnumerable<SynonymSlot> AvailableSlots => Enum.GetValues<SynonymSlot>();

        protected static string GetSlotDisplayName(SynonymSlot slot) => slot switch
        {
            SynonymSlot.ONE => "Slot ONE",
            SynonymSlot.TWO => "Slot TWO",
            _ => slot.ToString()
        };

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<Synonym> Synonyms => PaginationResult?.Items?.ToList() ?? new List<Synonym>();

        protected override async Task LoadDataAsync()
        {
            LoadLanguages();
            await LoadSynonyms();
        }

        protected void LoadLanguages()
        {
            AvailableLanguages = LanguageService.GetEnabledLanguages();

            // Set default language for new synonym if we have languages available
            if (AvailableLanguages.Any() && string.IsNullOrEmpty(NewSynonym.Language))
            {
                NewSynonym.Language = AvailableLanguages.First().LanguageCode;
            }
        }

        protected async Task LoadSynonyms()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                AllSynonyms = (await SynonymService.GetAllSynonymsAsync()).ToList();
                ApplyFilters();
            }, "loading synonyms");
        }

        protected void ApplyFilters()
        {
            IEnumerable<Synonym> filtered = AllSynonyms;

            if (!string.IsNullOrEmpty(SelectedLanguageFilter))
            {
                filtered = filtered.Where(s => s.Language == SelectedLanguageFilter);
            }

            if (SelectedSlotFilter.HasValue)
            {
                filtered = filtered.Where(s => s.Slot == SelectedSlotFilter.Value);
            }

            FilteredSynonyms = filtered.ToList();
            CurrentPage = 1;
            UpdatePaginatedSynonyms();
        }

        protected void OnLanguageFilterChanged(ChangeEventArgs e)
        {
            SelectedLanguageFilter = e.Value?.ToString() ?? string.Empty;
            ApplyFilters();
        }

        protected void OnSlotFilterChanged(ChangeEventArgs e)
        {
            var value = e.Value?.ToString();
            if (string.IsNullOrEmpty(value) || !Enum.TryParse<SynonymSlot>(value, out var slot))
            {
                SelectedSlotFilter = null;
            }
            else
            {
                SelectedSlotFilter = slot;
            }
            ApplyFilters();
        }

        protected bool HasActiveFilters => !string.IsNullOrEmpty(SelectedLanguageFilter) || SelectedSlotFilter.HasValue;

        protected async Task CreateSynonym()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidateModel(NewSynonym);
                
                await SynonymService.CreateSynonymAsync(
                    NewSynonym.Synonym!, 
                    NewSynonym.Language!, 
                    NewSynonym.Slot
                );

                var selectedLanguage = NewSynonym.Language;
                NewSynonym = new SynonymModel { Language = selectedLanguage };
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
                Synonym = synonym.SynonymItem,
                Language = synonym.Language,
                Slot = synonym.Slot
            };
        }

        protected async Task UpdateSynonym()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidateModel(EditingSynonym);
                
                await SynonymService.UpdateSynonymAsync(
                    EditingSynonym.Id,
                    EditingSynonym.Synonym!,
                    EditingSynonym.Language!,
                    EditingSynonym.Slot
                );

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
                await SynonymService.DeleteSynonymAsync(id);
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
                await GraphSyncService.SyncSynonymsToOptimizelyGraphAsync();
                SetSuccessMessage("Successfully synced all synonyms to Optimizely Graph (grouped by language).");
            }, "syncing synonyms to Optimizely Graph");
        }

        protected async Task SyncSynonymsForSelectedLanguage()
        {
            if (string.IsNullOrEmpty(SelectedLanguageFilter))
            {
                SetErrorMessage("Please select a language to sync.");
                return;
            }

            await ExecuteWithSyncHandlingAsync(async () =>
            {
                await GraphSyncService.SyncSynonymsForLanguageAsync(SelectedLanguageFilter);
                var languageName = AvailableLanguages.FirstOrDefault(l => l.LanguageCode == SelectedLanguageFilter)?.DisplayName ?? SelectedLanguageFilter;
                SetSuccessMessage($"Successfully synced synonyms for '{languageName}' to Optimizely Graph.");
            }, "syncing synonyms for language to Optimizely Graph");
        }

        protected void UpdatePaginatedSynonyms()
        {
            PaginationResult = PaginationService.GetPage(FilteredSynonyms, CurrentPage, PageSize);
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

        protected string GetLanguageDisplayName(string? languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return "Unknown";
            }

            return AvailableLanguages.FirstOrDefault(l => l.LanguageCode == languageCode)?.DisplayName ?? languageCode;
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