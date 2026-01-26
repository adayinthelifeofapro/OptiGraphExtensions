using System.Text.Json;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Navigation;

using Microsoft.Extensions.Logging;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Repositories;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.ScheduledJobs
{
    /// <summary>
    /// Scheduled job that executes due import configurations.
    /// Runs at configurable intervals and processes all imports that are scheduled to run.
    /// </summary>
    [ScheduledPlugIn(
        DisplayName = "External Data Import",
        Description = "Executes scheduled external data imports for custom data sources. Configure individual import schedules in the Custom Data Management UI.",
        GUID = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
    public class ExternalDataImportScheduledJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        public ExternalDataImportScheduledJob()
        {
            IsStoppable = true;
        }

        public override string Execute()
        {
            _stopSignaled = false;

            // Resolve services from the service locator (scheduled jobs are singletons)
            var serviceLocator = ServiceLocator.Current;
            var scheduledImportService = serviceLocator.GetInstance<IScheduledImportService>();
            var importService = serviceLocator.GetInstance<IExternalDataImportService>();
            var schemaService = serviceLocator.GetInstance<ICustomDataSchemaService>();
            var notificationService = serviceLocator.GetInstance<IImportNotificationService>();
            var importConfigRepository = serviceLocator.GetInstance<IImportConfigurationRepository>();
            var logger = serviceLocator.GetInstance<ILogger<ExternalDataImportScheduledJob>>();

            OnStatusChanged("Starting scheduled import check...");
            logger.LogInformation("External Data Import scheduled job started");

            var processedCount = 0;
            var successCount = 0;
            var failureCount = 0;
            var skippedCount = 0;

            try
            {
                // Get all due configurations
                var dueConfigs = scheduledImportService.GetDueConfigurationsAsync().GetAwaiter().GetResult();
                var configList = dueConfigs.ToList();

                logger.LogInformation("Found {Count} import configurations due for execution", configList.Count);

                if (configList.Count == 0)
                {
                    return "No import configurations are due for execution.";
                }

                foreach (var config in configList)
                {
                    if (_stopSignaled)
                    {
                        logger.LogInformation("Stop signal received, aborting job");
                        break;
                    }

                    OnStatusChanged($"Processing: {config.Name} ({processedCount + 1}/{configList.Count})");
                    logger.LogInformation("Processing import configuration: {ConfigName} (ID: {ConfigId})", config.Name, config.Id);

                    try
                    {
                        // Get the schema for this configuration's source
                        var schema = GetSchemaForConfig(config, schemaService, logger);
                        if (schema == null)
                        {
                            logger.LogWarning("Could not find schema for configuration {ConfigName}, skipping", config.Name);
                            skippedCount++;
                            continue;
                        }

                        // Map entity to model
                        var configModel = MapToModel(config);

                        // Track if this was previously failing
                        var wasRetry = config.ConsecutiveFailures > 0;
                        var retryAttempt = config.ConsecutiveFailures;

                        // Execute the import
                        var result = importService.ExecuteImportAsync(configModel, schema, config.TargetSourceId).GetAwaiter().GetResult();

                        // Record execution history
                        scheduledImportService.RecordExecutionAsync(config.Id, result, wasRetry, retryAttempt, wasScheduled: true).GetAwaiter().GetResult();

                        if (result.Success)
                        {
                            successCount++;
                            logger.LogInformation(
                                "Import {ConfigName} succeeded: {ItemsImported} items imported",
                                config.Name, result.ItemsImported);

                            // Update configuration tracking
                            scheduledImportService.UpdateConfigurationAfterExecutionAsync(config, wasSuccess: true).GetAwaiter().GetResult();

                            // Send recovery notification if this was a previously failing import
                            if (wasRetry)
                            {
                                notificationService.SendRecoveryNotificationAsync(config, result).GetAwaiter().GetResult();
                            }
                        }
                        else
                        {
                            failureCount++;
                            var errorMessage = string.Join("; ", result.Errors);
                            logger.LogWarning(
                                "Import {ConfigName} failed: {Error}",
                                config.Name, errorMessage);

                            // Update configuration tracking
                            scheduledImportService.UpdateConfigurationAfterExecutionAsync(config, wasSuccess: false, errorMessage).GetAwaiter().GetResult();

                            // Reload config to check updated failure count
                            var updatedConfig = importConfigRepository.GetByIdAsync(config.Id).GetAwaiter().GetResult();
                            if (updatedConfig != null && updatedConfig.ConsecutiveFailures >= updatedConfig.MaxRetries)
                            {
                                // Max retries reached - send notification
                                notificationService.SendFailureNotificationAsync(updatedConfig, result, updatedConfig.ConsecutiveFailures).GetAwaiter().GetResult();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        logger.LogError(ex, "Error executing import configuration {ConfigName}", config.Name);

                        // Record the failure
                        var errorResult = ImportResult.Failed(ex.Message);
                        scheduledImportService.RecordExecutionAsync(config.Id, errorResult, config.ConsecutiveFailures > 0, config.ConsecutiveFailures, wasScheduled: true).GetAwaiter().GetResult();
                        scheduledImportService.UpdateConfigurationAfterExecutionAsync(config, wasSuccess: false, ex.Message).GetAwaiter().GetResult();
                    }

                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in External Data Import scheduled job");
                return $"Job failed with error: {ex.Message}";
            }

            var message = $"Processed {processedCount} imports. Success: {successCount}, Failed: {failureCount}, Skipped: {skippedCount}";
            logger.LogInformation("External Data Import scheduled job completed: {Message}", message);

            return message;
        }

        public override void Stop()
        {
            _stopSignaled = true;
            base.Stop();
        }

        private static ContentTypeSchemaModel? GetSchemaForConfig(ImportConfiguration config, ICustomDataSchemaService schemaService, ILogger logger)
        {
            try
            {
                var sources = schemaService.GetAllSourcesAsync().GetAwaiter().GetResult();
                var source = sources.FirstOrDefault(s => s.SourceId == config.TargetSourceId);

                if (source == null)
                {
                    logger.LogWarning("Source {SourceId} not found for configuration {ConfigName}", config.TargetSourceId, config.Name);
                    return null;
                }

                var contentType = source.ContentTypes.FirstOrDefault(ct => ct.Name == config.TargetContentType);
                if (contentType == null)
                {
                    logger.LogWarning("Content type {ContentType} not found in source {SourceId} for configuration {ConfigName}",
                        config.TargetContentType, config.TargetSourceId, config.Name);
                    return null;
                }

                return contentType;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting schema for configuration {ConfigName}", config.Name);
                return null;
            }
        }

        private static ImportConfigurationModel MapToModel(ImportConfiguration entity)
        {
            return new ImportConfigurationModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                TargetSourceId = entity.TargetSourceId,
                TargetContentType = entity.TargetContentType,
                ApiUrl = entity.ApiUrl,
                HttpMethod = entity.HttpMethod,
                AuthType = entity.AuthType,
                AuthKeyOrUsername = entity.AuthKeyOrUsername,
                AuthValueOrPassword = entity.AuthValueOrPassword,
                FieldMappings = DeserializeFieldMappings(entity.FieldMappingsJson),
                IdFieldMapping = entity.IdFieldMapping,
                LanguageRouting = entity.LanguageRouting,
                JsonPath = entity.JsonPath,
                CustomHeaders = DeserializeCustomHeaders(entity.CustomHeadersJson),
                IsActive = entity.IsActive,
                LastImportAt = entity.LastImportAt,
                LastImportCount = entity.LastImportCount
            };
        }

        private static List<FieldMapping> DeserializeFieldMappings(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<FieldMapping>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<FieldMapping>>(json) ?? new List<FieldMapping>();
            }
            catch
            {
                return new List<FieldMapping>();
            }
        }

        private static Dictionary<string, string> DeserializeCustomHeaders(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
    }
}
