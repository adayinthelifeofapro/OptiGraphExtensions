using Microsoft.EntityFrameworkCore;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.CustomData.Repositories
{
    /// <summary>
    /// Repository implementation for managing import configurations.
    /// </summary>
    public class ImportConfigurationRepository : IImportConfigurationRepository
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public ImportConfigurationRepository(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public async Task<IEnumerable<ImportConfiguration>> GetAllAsync()
        {
            return await _dataContext.ImportConfigurations
                .AsNoTracking()
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ImportConfiguration>> GetBySourceIdAsync(string sourceId)
        {
            return await _dataContext.ImportConfigurations
                .AsNoTracking()
                .Where(c => c.TargetSourceId == sourceId)
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ImportConfiguration?> GetByIdAsync(Guid id)
        {
            return await _dataContext.ImportConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ImportConfiguration> CreateAsync(ImportConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);

            config.Id = Guid.NewGuid();
            config.CreatedAt = DateTime.UtcNow;

            _dataContext.ImportConfigurations.Add(config);
            await _dataContext.SaveChangesAsync();
            return config;
        }

        public async Task<ImportConfiguration> UpdateAsync(ImportConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);

            // Get the existing entity from database
            var existing = await _dataContext.ImportConfigurations.FindAsync(config.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"ImportConfiguration with ID {config.Id} not found.");
            }

            // Update the existing entity's properties
            existing.Name = config.Name;
            existing.Description = config.Description;
            existing.TargetSourceId = config.TargetSourceId;
            existing.TargetContentType = config.TargetContentType;
            existing.ApiUrl = config.ApiUrl;
            existing.HttpMethod = config.HttpMethod;
            existing.AuthType = config.AuthType;
            existing.AuthKeyOrUsername = config.AuthKeyOrUsername;
            existing.AuthValueOrPassword = config.AuthValueOrPassword;
            existing.FieldMappingsJson = config.FieldMappingsJson;
            existing.IdFieldMapping = config.IdFieldMapping;
            existing.LanguageRouting = config.LanguageRouting;
            existing.JsonPath = config.JsonPath;
            existing.CustomHeadersJson = config.CustomHeadersJson;
            existing.IsActive = config.IsActive;
            existing.LastImportAt = config.LastImportAt;
            existing.LastImportCount = config.LastImportCount;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = config.UpdatedBy;

            await _dataContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var config = await _dataContext.ImportConfigurations.FindAsync(id);
            if (config == null)
            {
                return false;
            }

            _dataContext.ImportConfigurations.Remove(config);
            await _dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dataContext.ImportConfigurations.AnyAsync(c => c.Id == id);
        }
    }
}
