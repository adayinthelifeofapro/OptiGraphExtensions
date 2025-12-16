using Microsoft.AspNetCore.Components;

using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Components;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.StopWords.Models;
using OptiGraphExtensions.Features.StopWords.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.StopWords
{
    public class StopWordManagementComponentBase : ManagementComponentBase<StopWord, StopWordModel>
    {
        [Inject]
        protected IStopWordApiService StopWordApiService { get; set; } = null!;

        [Inject]
        protected IPaginationService<StopWord> PaginationService { get; set; } = null!;

        [Inject]
        protected IStopWordValidationService ValidationService { get; set; } = null!;

        [Inject]
        protected IRequestMapper<StopWordModel, CreateStopWordRequest, UpdateStopWordRequest> RequestMapper { get; set; } = null!;

        [Inject]
        protected ILanguageService LanguageService { get; set; } = null!;

        protected PaginationResult<StopWord>? PaginationResult { get; set; }
        protected IList<StopWord> AllStopWords { get; set; } = new List<StopWord>();
        protected IList<StopWord> FilteredStopWords { get; set; } = new List<StopWord>();
        protected StopWordModel NewStopWord { get; set; } = new();
        protected StopWordModel EditingStopWord { get; set; } = new();
        protected bool IsEditing { get; set; }
        protected bool IsSyncing { get; set; }
        protected bool IsDeleting { get; set; }

        protected IEnumerable<LanguageInfo> AvailableLanguages { get; set; } = Enumerable.Empty<LanguageInfo>();
        protected string SelectedLanguageFilter { get; set; } = string.Empty;

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => PaginationResult?.TotalPages ?? 0;
        protected int TotalItems => PaginationResult?.TotalItems ?? 0;
        protected List<StopWord> StopWords => PaginationResult?.Items?.ToList() ?? new List<StopWord>();

        protected override async Task LoadDataAsync()
        {
            LoadLanguages();
            await LoadStopWords();
        }

        protected void LoadLanguages()
        {
            AvailableLanguages = LanguageService.GetEnabledLanguages();

            // Set default language for new stop word if we have languages available
            if (AvailableLanguages.Any() && string.IsNullOrEmpty(NewStopWord.Language))
            {
                NewStopWord.Language = AvailableLanguages.First().LanguageCode;
            }
        }

        protected async Task LoadStopWords()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                AllStopWords = await StopWordApiService.GetStopWordsAsync();
                ApplyLanguageFilter();
            }, "loading stop words");
        }

        protected void ApplyLanguageFilter()
        {
            if (string.IsNullOrEmpty(SelectedLanguageFilter))
            {
                FilteredStopWords = AllStopWords;
            }
            else
            {
                FilteredStopWords = AllStopWords.Where(s => s.Language == SelectedLanguageFilter).ToList();
            }

            CurrentPage = 1;
            UpdatePaginatedStopWords();
        }

        protected void OnLanguageFilterChanged(ChangeEventArgs e)
        {
            SelectedLanguageFilter = e.Value?.ToString() ?? string.Empty;
            ApplyLanguageFilter();
        }

        protected async Task CreateStopWord()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidateModel(NewStopWord);
                var request = RequestMapper.MapToCreateRequest(NewStopWord);
                await StopWordApiService.CreateStopWordAsync(request);
                var selectedLanguage = NewStopWord.Language;
                NewStopWord = new StopWordModel { Language = selectedLanguage };
                SetSuccessMessage("Stop word created successfully.");
                await LoadStopWords();
            }, "creating stop word", showLoading: false);
        }

        protected void StartEdit(StopWord stopWord)
        {
            IsEditing = true;
            EditingStopWord = new StopWordModel
            {
                Id = stopWord.Id,
                Word = stopWord.Word,
                Language = stopWord.Language
            };
        }

        protected async Task UpdateStopWord()
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await ValidateModel(EditingStopWord);
                var request = RequestMapper.MapToUpdateRequest(EditingStopWord);
                await StopWordApiService.UpdateStopWordAsync(EditingStopWord.Id, request);
                SetSuccessMessage("Stop word updated successfully.");
                CancelEdit();
                await LoadStopWords();
            }, "updating stop word", showLoading: false);
        }

        protected void CancelEdit()
        {
            IsEditing = false;
            EditingStopWord = new StopWordModel();
        }

        protected async Task DeleteStopWord(Guid id)
        {
            await ExecuteWithLoadingAndErrorHandlingAsync(async () =>
            {
                await StopWordApiService.DeleteStopWordAsync(id);
                SetSuccessMessage("Stop word deleted successfully.");
                await LoadStopWords();
            }, "deleting stop word", showLoading: false);
        }

        protected async Task<bool> ConfirmDelete()
        {
            return await Task.FromResult(true);
        }

        protected async Task SyncStopWordsToOptimizelyGraph()
        {
            await ExecuteWithSyncHandlingAsync(async () =>
            {
                await StopWordApiService.SyncStopWordsToOptimizelyGraphAsync();
                SetSuccessMessage("Successfully synced all stop words to Optimizely Graph (grouped by language).");
            }, "syncing stop words to Optimizely Graph");
        }

        protected async Task SyncStopWordsForSelectedLanguage()
        {
            if (string.IsNullOrEmpty(SelectedLanguageFilter))
            {
                SetErrorMessage("Please select a language to sync.");
                return;
            }

            await ExecuteWithSyncHandlingAsync(async () =>
            {
                await StopWordApiService.SyncStopWordsForLanguageAsync(SelectedLanguageFilter);
                var languageName = AvailableLanguages.FirstOrDefault(l => l.LanguageCode == SelectedLanguageFilter)?.DisplayName ?? SelectedLanguageFilter;
                SetSuccessMessage($"Successfully synced stop words for '{languageName}' to Optimizely Graph.");
            }, "syncing stop words for language to Optimizely Graph");
        }

        protected async Task DeleteAllStopWordsFromGraph()
        {
            if (string.IsNullOrEmpty(SelectedLanguageFilter))
            {
                SetErrorMessage("Please select a language to delete stop words from Graph.");
                return;
            }

            await ExecuteWithDeleteHandlingAsync(async () =>
            {
                await StopWordApiService.DeleteAllStopWordsFromGraphAsync(SelectedLanguageFilter);
                var languageName = AvailableLanguages.FirstOrDefault(l => l.LanguageCode == SelectedLanguageFilter)?.DisplayName ?? SelectedLanguageFilter;
                SetSuccessMessage($"Successfully deleted all stop words for '{languageName}' from Optimizely Graph.");
            }, "deleting stop words from Optimizely Graph");
        }

        protected void UpdatePaginatedStopWords()
        {
            PaginationResult = PaginationService.GetPage(FilteredStopWords, CurrentPage, PageSize);
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            NavigateToPage(page, CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedStopWordsAsync);
        }

        protected void GoToPreviousPage()
        {
            NavigateToPreviousPage(CurrentPage, (p) => CurrentPage = p, UpdatePaginatedStopWordsAsync);
        }

        protected void GoToNextPage()
        {
            NavigateToNextPage(CurrentPage, TotalPages, (p) => CurrentPage = p, UpdatePaginatedStopWordsAsync);
        }

        protected string GetLanguageDisplayName(string? languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return "Unknown";
            }

            return AvailableLanguages.FirstOrDefault(l => l.LanguageCode == languageCode)?.DisplayName ?? languageCode;
        }

        private Task UpdatePaginatedStopWordsAsync()
        {
            UpdatePaginatedStopWords();
            return Task.CompletedTask;
        }

        private Task ValidateModel(StopWordModel model)
        {
            var validationResult = ValidationService.ValidateStopWord(model);
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

        private async Task ExecuteWithDeleteHandlingAsync(Func<Task> operation, string operationName)
        {
            try
            {
                IsDeleting = true;
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
                IsDeleting = false;
                StateHasChanged();
            }
        }
    }
}
