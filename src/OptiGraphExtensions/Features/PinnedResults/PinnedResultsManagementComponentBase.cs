using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text;

namespace OptiGraphExtensions.Features.PinnedResults
{
    public class PinnedResultsManagementComponentBase : ComponentBase
    {
        [Inject]
        protected HttpClient HttpClient { get; set; } = null!;

        [Inject]
        protected IHttpContextAccessor? HttpContextAccessor { get; set; }

        [Inject]
        protected IConfiguration? Configuration { get; set; }

        // Collections properties
        protected List<PinnedResultsCollection> Collections { get; set; } = new();
        protected List<PinnedResultsCollection> AllCollections { get; set; } = new();
        protected PinnedResultsCollectionModel NewCollection { get; set; } = new();
        protected PinnedResultsCollectionModel EditingCollection { get; set; } = new();
        protected bool IsEditingCollection { get; set; } = false;

        // Pinned results properties
        protected List<PinnedResult> PinnedResults { get; set; } = new();
        protected List<PinnedResult> AllPinnedResults { get; set; } = new();
        protected PinnedResultModel NewPinnedResult { get; set; } = new();
        protected PinnedResultModel EditingPinnedResult { get; set; } = new();
        protected bool IsEditingPinnedResult { get; set; } = false;
        protected Guid? SelectedCollectionId { get; set; }

        // Shared state
        protected bool IsLoading { get; set; } = false;
        protected bool IsSyncing { get; set; } = false;
        protected string? ErrorMessage { get; set; }
        protected string? SuccessMessage { get; set; }

        // Pagination properties
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => (int)Math.Ceiling((double)AllPinnedResults.Count / PageSize);
        protected int TotalItems => AllPinnedResults.Count;

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
                var baseUrl = GetBaseUrl();
                
                var httpContext = HttpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var response = await HttpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections");
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var collections = System.Text.Json.JsonSerializer.Deserialize<List<PinnedResultsCollection>>(responseContent, new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            AllCollections = collections?.OrderBy(c => c.Title).ToList() ?? new();
                            Collections = AllCollections;
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            ErrorMessage = $"Error: API returned invalid JSON. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}";
                        }
                    }
                    else
                    {
                        ErrorMessage = $"Error loading collections: {response.StatusCode} - Content: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}";
                    }
                }
                else
                {
                    ErrorMessage = "Error: User is not authenticated. Please log in to access collections.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading collections: {ex.Message}";
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
                if (string.IsNullOrWhiteSpace(NewCollection.Title))
                {
                    ErrorMessage = "Collection title is required.";
                    return;
                }

                var request = new CreatePinnedResultsCollectionRequest
                {
                    Title = NewCollection.Title.Trim(),
                    IsActive = NewCollection.IsActive
                };

                var baseUrl = GetBaseUrl();
                var response = await HttpClient.PostAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections", request);
                
                if (response.IsSuccessStatusCode)
                {
                    NewCollection = new PinnedResultsCollectionModel();
                    SuccessMessage = "Collection created successfully.";
                    ErrorMessage = null;
                    await LoadCollections();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ErrorMessage = "Error: You are not authorized to create collections. Please ensure you are logged in and have the required permissions.";
                    SuccessMessage = null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Error creating collection: {response.StatusCode} - {response.ReasonPhrase}";
                    SuccessMessage = null;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating collection: {ex.Message}";
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
                if (string.IsNullOrWhiteSpace(EditingCollection.Title))
                {
                    ErrorMessage = "Collection title is required.";
                    return;
                }

                var request = new UpdatePinnedResultsCollectionRequest
                {
                    Title = EditingCollection.Title.Trim(),
                    IsActive = EditingCollection.IsActive
                };

                var baseUrl = GetBaseUrl();
                var response = await HttpClient.PutAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections/{EditingCollection.Id}", request);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Collection updated successfully.";
                    ErrorMessage = null;
                    CancelEditCollection();
                    await LoadCollections();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Collection not found.";
                }
                else
                {
                    ErrorMessage = $"Error updating collection: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating collection: {ex.Message}";
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
                var baseUrl = GetBaseUrl();
                var response = await HttpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections/{id}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Collection deleted successfully.";
                    ErrorMessage = null;
                    
                    // Reset selected collection if it was deleted
                    if (SelectedCollectionId == id)
                    {
                        SelectedCollectionId = null;
                        AllPinnedResults.Clear();
                        PinnedResults.Clear();
                    }
                    
                    await LoadCollections();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Collection not found.";
                }
                else
                {
                    ErrorMessage = $"Error deleting collection: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting collection: {ex.Message}";
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
                PinnedResults.Clear();
            }
        }

        protected async Task LoadPinnedResults()
        {
            if (!SelectedCollectionId.HasValue) return;

            try
            {
                IsLoading = true;
                var baseUrl = GetBaseUrl();
                
                var httpContext = HttpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var response = await HttpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results?collectionId={SelectedCollectionId}");
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var pinnedResults = System.Text.Json.JsonSerializer.Deserialize<List<PinnedResult>>(responseContent, new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            AllPinnedResults = pinnedResults?.OrderBy(pr => pr.Priority).ToList() ?? new();
                            UpdatePaginatedPinnedResults();
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            ErrorMessage = $"Error: API returned invalid JSON. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}";
                        }
                    }
                    else
                    {
                        ErrorMessage = $"Error loading pinned results: {response.StatusCode} - Content: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}";
                    }
                }
                else
                {
                    ErrorMessage = "Error: User is not authenticated. Please log in to access pinned results.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading pinned results: {ex.Message}";
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

                if (string.IsNullOrWhiteSpace(NewPinnedResult.Phrases) || 
                    string.IsNullOrWhiteSpace(NewPinnedResult.TargetKey) || 
                    string.IsNullOrWhiteSpace(NewPinnedResult.Language))
                {
                    ErrorMessage = "Phrases, Target Key, and Language are required.";
                    return;
                }

                var request = new CreatePinnedResultRequest
                {
                    CollectionId = SelectedCollectionId.Value,
                    Phrases = NewPinnedResult.Phrases.Trim(),
                    TargetKey = NewPinnedResult.TargetKey.Trim(),
                    Language = NewPinnedResult.Language.Trim(),
                    Priority = NewPinnedResult.Priority,
                    IsActive = NewPinnedResult.IsActive
                };

                var baseUrl = GetBaseUrl();
                var response = await HttpClient.PostAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results", request);
                
                if (response.IsSuccessStatusCode)
                {
                    NewPinnedResult = new PinnedResultModel { CollectionId = SelectedCollectionId.Value };
                    SuccessMessage = "Pinned result created successfully.";
                    ErrorMessage = null;
                    await LoadPinnedResults();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ErrorMessage = "Error: You are not authorized to create pinned results. Please ensure you are logged in and have the required permissions.";
                    SuccessMessage = null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Error creating pinned result: {response.StatusCode} - {response.ReasonPhrase}";
                    SuccessMessage = null;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating pinned result: {ex.Message}";
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
                if (string.IsNullOrWhiteSpace(EditingPinnedResult.Phrases) || 
                    string.IsNullOrWhiteSpace(EditingPinnedResult.TargetKey) || 
                    string.IsNullOrWhiteSpace(EditingPinnedResult.Language))
                {
                    ErrorMessage = "Phrases, Target Key, and Language are required.";
                    return;
                }

                var request = new UpdatePinnedResultRequest
                {
                    Phrases = EditingPinnedResult.Phrases.Trim(),
                    TargetKey = EditingPinnedResult.TargetKey.Trim(),
                    Language = EditingPinnedResult.Language.Trim(),
                    Priority = EditingPinnedResult.Priority,
                    IsActive = EditingPinnedResult.IsActive
                };

                var baseUrl = GetBaseUrl();
                var response = await HttpClient.PutAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results/{EditingPinnedResult.Id}", request);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Pinned result updated successfully.";
                    ErrorMessage = null;
                    CancelEditPinnedResult();
                    await LoadPinnedResults();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Pinned result not found.";
                }
                else
                {
                    ErrorMessage = $"Error updating pinned result: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating pinned result: {ex.Message}";
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
                var baseUrl = GetBaseUrl();
                var response = await HttpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results/{id}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Pinned result deleted successfully.";
                    ErrorMessage = null;
                    await LoadPinnedResults();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Pinned result not found.";
                }
                else
                {
                    ErrorMessage = $"Error deleting pinned result: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting pinned result: {ex.Message}";
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

        private string GetBaseUrl()
        {
            var request = HttpContextAccessor.HttpContext?.Request;
            if (request == null)
                return string.Empty;

            return $"{request.Scheme}://{request.Host}";
        }

        protected void UpdatePaginatedPinnedResults()
        {
            var startIndex = (CurrentPage - 1) * PageSize;
            PinnedResults = AllPinnedResults.Skip(startIndex).Take(PageSize).ToList();
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
                UpdatePaginatedPinnedResults();
            }
        }

        protected void GoToPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePaginatedPinnedResults();
            }
        }

        protected void GoToNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePaginatedPinnedResults();
            }
        }

        #endregion

        #region Model Classes

        protected class PinnedResultsCollectionModel
        {
            public Guid Id { get; set; }

            [Required(ErrorMessage = "Collection title is required")]
            [StringLength(255, ErrorMessage = "Collection title must be less than 255 characters")]
            public string? Title { get; set; }

            public bool IsActive { get; set; } = true;
        }

        protected class PinnedResultModel
        {
            public Guid Id { get; set; }

            public Guid CollectionId { get; set; }

            [Required(ErrorMessage = "Phrases are required")]
            [StringLength(1000, ErrorMessage = "Phrases must be less than 1000 characters")]
            public string? Phrases { get; set; }

            [Required(ErrorMessage = "Target Key is required")]
            [StringLength(100, ErrorMessage = "Target Key must be less than 100 characters")]
            public string? TargetKey { get; set; }

            [Required(ErrorMessage = "Language is required")]
            [StringLength(10, ErrorMessage = "Language must be less than 10 characters")]
            public string? Language { get; set; }

            public int Priority { get; set; } = 1;

            public bool IsActive { get; set; } = true;
        }

        #endregion
    }
}