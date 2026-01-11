using System.Text.Json;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.QueryLibrary.Models;
using OptiGraphExtensions.Features.QueryLibrary.Repositories;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;

namespace OptiGraphExtensions.Features.QueryLibrary.Services
{
    /// <summary>
    /// Service for managing saved queries with mapping between entities and models.
    /// </summary>
    public class SavedQueryService : ISavedQueryService
    {
        private readonly ISavedQueryRepository _repository;
        private readonly IRawQueryService _rawQueryService;
        private readonly JsonSerializerOptions _jsonOptions;

        public SavedQueryService(
            ISavedQueryRepository repository,
            IRawQueryService rawQueryService)
        {
            _repository = repository;
            _rawQueryService = rawQueryService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<IEnumerable<SavedQueryModel>> GetAllQueriesAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(MapToModel);
        }

        public async Task<IEnumerable<SavedQueryModel>> GetActiveQueriesAsync()
        {
            var entities = await _repository.GetActiveAsync();
            return entities.Select(MapToModel);
        }

        public async Task<SavedQueryModel?> GetQueryByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity != null ? MapToModel(entity) : null;
        }

        public async Task<SavedQueryModel> CreateQueryAsync(SavedQueryModel model, string? createdBy = null)
        {
            var entity = MapToEntity(model);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = createdBy;

            var created = await _repository.CreateAsync(entity);
            return MapToModel(created);
        }

        public async Task<SavedQueryModel?> UpdateQueryAsync(Guid id, SavedQueryModel model, string? updatedBy = null)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return null;
            }

            // Update fields
            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.QueryType = model.QueryType;
            existing.ContentType = model.ContentType;
            existing.SelectedFieldsJson = SerializeFields(model.SelectedFields);
            existing.FiltersJson = SerializeFilters(model.Filters);
            existing.Language = model.Language;
            existing.SortField = model.SortField;
            existing.SortDescending = model.SortDescending;
            existing.RawGraphQuery = model.RawGraphQuery;
            existing.QueryVariablesJson = model.QueryVariablesJson;
            existing.PageSize = model.PageSize;
            existing.IsActive = model.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = updatedBy;

            var updated = await _repository.UpdateAsync(existing);
            return MapToModel(updated);
        }

        public async Task<bool> DeleteQueryAsync(Guid id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> QueryExistsAsync(Guid id)
        {
            return await _repository.ExistsAsync(id);
        }

        public QueryExecutionRequest ToExecutionRequest(SavedQueryModel model)
        {
            var request = new QueryExecutionRequest
            {
                QueryType = model.QueryType,
                ContentType = model.ContentType,
                SelectedFields = model.SelectedFields ?? new List<string>(),
                Filters = model.Filters ?? new List<QueryFilter>(),
                Language = model.Language,
                SortField = model.SortField,
                SortDescending = model.SortDescending,
                RawGraphQuery = model.RawGraphQuery,
                PageSize = model.PageSize > 0 ? model.PageSize : 100
            };

            // Parse query variables for raw queries
            if (model.QueryType == QueryType.Raw && !string.IsNullOrEmpty(model.QueryVariablesJson))
            {
                request.QueryVariables = _rawQueryService.ParseVariables(model.QueryVariablesJson);
            }

            return request;
        }

        private SavedQueryModel MapToModel(SavedQuery entity)
        {
            return new SavedQueryModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                QueryType = entity.QueryType,
                ContentType = entity.ContentType,
                SelectedFields = DeserializeFields(entity.SelectedFieldsJson),
                Filters = DeserializeFilters(entity.FiltersJson),
                Language = entity.Language,
                SortField = entity.SortField,
                SortDescending = entity.SortDescending,
                RawGraphQuery = entity.RawGraphQuery,
                QueryVariablesJson = entity.QueryVariablesJson,
                PageSize = entity.PageSize,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedAt = entity.UpdatedAt,
                UpdatedBy = entity.UpdatedBy
            };
        }

        private SavedQuery MapToEntity(SavedQueryModel model)
        {
            return new SavedQuery
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                QueryType = model.QueryType,
                ContentType = model.ContentType,
                SelectedFieldsJson = SerializeFields(model.SelectedFields),
                FiltersJson = SerializeFilters(model.Filters),
                Language = model.Language,
                SortField = model.SortField,
                SortDescending = model.SortDescending,
                RawGraphQuery = model.RawGraphQuery,
                QueryVariablesJson = model.QueryVariablesJson,
                PageSize = model.PageSize,
                IsActive = model.IsActive
            };
        }

        private List<string> DeserializeFields(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        private string SerializeFields(List<string>? fields)
        {
            if (fields == null || !fields.Any())
            {
                return "[]";
            }

            return JsonSerializer.Serialize(fields, _jsonOptions);
        }

        private List<QueryFilter> DeserializeFilters(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<QueryFilter>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<QueryFilter>>(json, _jsonOptions) ?? new List<QueryFilter>();
            }
            catch (JsonException)
            {
                return new List<QueryFilter>();
            }
        }

        private string SerializeFilters(List<QueryFilter>? filters)
        {
            if (filters == null || !filters.Any())
            {
                return "[]";
            }

            return JsonSerializer.Serialize(filters, _jsonOptions);
        }
    }
}
