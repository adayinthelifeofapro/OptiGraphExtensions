using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text;

namespace OptiGraphExtensions.Features.Synonyms
{
    public class SynonymManagementComponentBase : ComponentBase
    {
        [Inject]
        protected HttpClient HttpClient { get; set; } = null!;

        [Inject]
        protected IHttpContextAccessor? HttpContextAccessor { get; set; }

        [Inject]
        protected IConfiguration? Configuration { get; set; }

        protected List<Synonym> Synonyms { get; set; } = new();
        protected List<Synonym> AllSynonyms { get; set; } = new();
        protected SynonymModel NewSynonym { get; set; } = new();
        protected SynonymModel EditingSynonym { get; set; } = new();
        protected bool IsEditing { get; set; } = false;
        protected bool IsLoading { get; set; } = false;
        protected bool IsSyncing { get; set; } = false;
        protected string? ErrorMessage { get; set; }
        protected string? SuccessMessage { get; set; }

        // Pagination properties
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPages => (int)Math.Ceiling((double)AllSynonyms.Count / PageSize);
        protected int TotalItems => AllSynonyms.Count;

        protected override async Task OnInitializedAsync()
        {
            await LoadSynonyms();
        }

        protected async Task LoadSynonyms()
        {
            try
            {
                IsLoading = true;
                var baseUrl = GetBaseUrl();
                
                // Add authentication headers if available
                var httpContext = HttpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    // For debugging - let's see what we get back
                    var response = await HttpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms");
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            // Try to parse as JSON
                            var synonyms = System.Text.Json.JsonSerializer.Deserialize<List<Synonym>>(responseContent, new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            AllSynonyms = synonyms?.OrderBy(s => s.SynonymItem).ToList() ?? new();
                            UpdatePaginatedSynonyms();
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            ErrorMessage = $"Error: API returned invalid JSON. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}";
                        }
                    }
                    else
                    {
                        ErrorMessage = $"Error loading synonyms: {response.StatusCode} - Content: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}";
                    }
                }
                else
                {
                    ErrorMessage = "Error: User is not authenticated. Please log in to access synonyms.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading synonyms: {ex.Message}";
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
                if (string.IsNullOrWhiteSpace(NewSynonym.Synonym))
                {
                    ErrorMessage = "Synonym text is required.";
                    return;
                }

                var request = new CreateSynonymRequest
                {
                    Synonym = NewSynonym.Synonym.Trim()
                };

                var baseUrl = GetBaseUrl();
                var response = await HttpClient.PostAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms", request);
                
                if (response.IsSuccessStatusCode)
                {
                    NewSynonym = new SynonymModel();
                    SuccessMessage = "Synonym created successfully.";
                    ErrorMessage = null;
                    await LoadSynonyms();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ErrorMessage = "Error: You are not authorized to create synonyms. Please ensure you are logged in and have the required permissions.";
                    SuccessMessage = null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Error creating synonym: {response.StatusCode} - {response.ReasonPhrase}";
                    SuccessMessage = null;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating synonym: {ex.Message}";
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
                if (string.IsNullOrWhiteSpace(EditingSynonym.Synonym))
                {
                    ErrorMessage = "Synonym text is required.";
                    return;
                }

                var request = new UpdateSynonymRequest
                {
                    Synonym = EditingSynonym.Synonym.Trim()
                };

                var baseUrl = GetBaseUrl();
                var response = await HttpClient.PutAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms/{EditingSynonym.Id}", request);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Synonym updated successfully.";
                    ErrorMessage = null;
                    CancelEdit();
                    await LoadSynonyms();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Synonym not found.";
                }
                else
                {
                    ErrorMessage = $"Error updating synonym: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating synonym: {ex.Message}";
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
                var baseUrl = GetBaseUrl();
                var response = await HttpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms/{id}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Synonym deleted successfully.";
                    ErrorMessage = null;
                    await LoadSynonyms();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Synonym not found.";
                }
                else
                {
                    ErrorMessage = $"Error deleting synonym: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting synonym: {ex.Message}";
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
                ErrorMessage = null;
                SuccessMessage = null;

                var httpContext = HttpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    ErrorMessage = "Error: User is not authenticated. Please log in to sync synonyms.";
                    return;
                }

                // First, get all synonyms from our local API
                var baseUrl = GetBaseUrl();
                var response = await HttpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms");
                
                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = $"Error retrieving synonyms for sync: {response.StatusCode} - {response.ReasonPhrase}";
                    return;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                List<Synonym> synonymsToSync;

                try
                {
                    synonymsToSync = System.Text.Json.JsonSerializer.Deserialize<List<Synonym>>(responseContent, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Synonym>();
                }
                catch (System.Text.Json.JsonException)
                {
                    ErrorMessage = "Error: Failed to parse synonyms data for sync.";
                    return;
                }

                if (!synonymsToSync.Any())
                {
                    ErrorMessage = "No synonyms found to sync to Optimizely Graph.";
                    return;
                }

                // TODO: You'll need to configure these values based on your Optimizely Graph setup
                var graphGatewayUrl = GetOptimizelyGraphGatewayUrl();
                var hmacKey = GetOptimizelyGraphHmacKey();
                var hmacSecret = GetOptimizelyGraphHmacSecret();

                var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

                if (string.IsNullOrEmpty(graphGatewayUrl))
                {
                    ErrorMessage = "Error: Optimizely Graph Gateway URL not configured. Please configure your Graph settings.";
                    return;
                }

                if (string.IsNullOrEmpty(hmacKey) || string.IsNullOrEmpty(hmacSecret))
                {
                    ErrorMessage = "Error: Optimizely Graph HMAC credentials not configured. Please configure your authentication settings.";
                    return;
                }

                // Create the request to Optimizely Graph
                var graphApiUrl = $"{graphGatewayUrl}/resources/synonyms";
                
                // Create HTTP request message to add custom headers
                using var request = new HttpRequestMessage(HttpMethod.Put, graphApiUrl);
                request.Content = new StringContent(GetSynonyms(synonymsToSync), Encoding.UTF8, "text/plain");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeader);

                // Send to Optimizely Graph
                var graphResponse = await HttpClient.SendAsync(request);

                if (graphResponse.IsSuccessStatusCode)
                {
                    SuccessMessage = $"Successfully synced {synonymsToSync.Count} synonyms to Optimizely Graph.";
                }
                else
                {
                    var errorContent = await graphResponse.Content.ReadAsStringAsync();
                    ErrorMessage = $"Error syncing to Optimizely Graph: {graphResponse.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error syncing synonyms: {ex.Message}";
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private static string GetSynonyms(List<Synonym> synonymsToSync)
        {
            var synonymList = new StringBuilder();

            foreach (var synonym in synonymsToSync)
            {
                synonymList.AppendLine(synonym.SynonymItem);
            }

            return synonymList.ToString();
        }

        private string GetOptimizelyGraphGatewayUrl()
        {
            return Configuration["Optimizely:ContentGraph:GatewayAddress"] ?? "";
        }

        private string GetOptimizelyGraphHmacKey()
        {
            return Configuration["Optimizely:ContentGraph:AppKey"] ?? "";
        }

        private string GetOptimizelyGraphHmacSecret()
        {
            return Configuration["Optimizely:ContentGraph:Secret"] ?? "";
        }


        private string GetBaseUrl()
        {
            var request = HttpContextAccessor.HttpContext?.Request;
            if (request == null)
                return string.Empty;

            return $"{request.Scheme}://{request.Host}";
        }

        protected void UpdatePaginatedSynonyms()
        {
            var startIndex = (CurrentPage - 1) * PageSize;
            Synonyms = AllSynonyms.Skip(startIndex).Take(PageSize).ToList();
            StateHasChanged();
        }

        protected void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
                UpdatePaginatedSynonyms();
            }
        }

        protected void GoToPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePaginatedSynonyms();
            }
        }

        protected void GoToNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePaginatedSynonyms();
            }
        }

        protected class SynonymModel
        {
            public Guid Id { get; set; }

            [Required(ErrorMessage = "Synonym is required")]
            [StringLength(255, ErrorMessage = "Synonym must be less than 255 characters")]
            public string? Synonym { get; set; }
        }

        protected class CreateSynonymRequest
        {
            public string? Synonym { get; set; }
        }

        protected class UpdateSynonymRequest
        {
            public string? Synonym { get; set; }
        }
    }
}