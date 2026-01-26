using EPiServer.Shell.Modules;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Repositories;
using OptiGraphExtensions.Features.Synonyms.Services;
using OptiGraphExtensions.Features.PinnedResults.Repositories;
using OptiGraphExtensions.Features.PinnedResults.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.ContentSearch.Services;
using OptiGraphExtensions.Features.ContentSearch.Services.Abstractions;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Validation;
using OptiGraphExtensions.Features.Common.Caching;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.Webhooks.Services;
using OptiGraphExtensions.Features.Webhooks.Services.Abstractions;
using OptiGraphExtensions.Features.QueryLibrary.Repositories;
using OptiGraphExtensions.Features.QueryLibrary.Services;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;
using OptiGraphExtensions.Features.RequestLogs.Services;
using OptiGraphExtensions.Features.RequestLogs.Services.Abstractions;
using OptiGraphExtensions.Features.CustomData.Services;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Repositories;

namespace OptiGraphExtensions.Features.Configuration;

public static class OptiGraphExtensionsServiceExtensions
{
    public static IServiceCollection AddOptiGraphExtensions(
        this IServiceCollection services,
        Action<OptiGraphExtensionsSetupOptions>? setUpOptions = null,
        Action<AuthorizationOptions>? authorizationOptions = null)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

        // Handle null CSP Setup Options.
        var concreteOptions = new OptiGraphExtensionsSetupOptions();
        if (setUpOptions != null)
        {
            setUpOptions(concreteOptions);
        }
        else
        {
            concreteOptions.ConnectionStringName = "EPiServerDB";
        }

        // Authorization
        if (authorizationOptions != null)
        {
            services.AddAuthorization(authorizationOptions);
        }
        else
        {
            var allowedRoles = new List<string> { "CmsAdmins", "Administrator", "WebAdmins" };
            services.AddAuthorization(authorizationOptions =>
            {
                authorizationOptions.AddPolicy(OptiGraphExtensionsConstants.AuthorizationPolicy, policy =>
                {
                    policy.RequireRole(allowedRoles);
                });
            });
        }

        // API Controllers
        services.AddControllers();

        // HttpClient for API calls with cookie forwarding for local API requests
        services.AddHttpContextAccessor();
        services.TryAddTransient<CookieForwardingHandler>();

        // Configure HTTP clients with appropriate timeouts
        var defaultTimeout = TimeSpan.FromSeconds(30);
        var longRunningTimeout = TimeSpan.FromSeconds(60);

        services.AddHttpClient<ISynonymGraphSyncService, SynonymGraphSyncService>()
            .ConfigureHttpClient(client => client.Timeout = defaultTimeout);
        services.AddHttpClient<IPinnedResultsGraphSyncService, PinnedResultsGraphSyncService>()
            .ConfigureHttpClient(client => client.Timeout = defaultTimeout);
        services.AddHttpClient<IContentSearchService, ContentSearchService>()
            .ConfigureHttpClient(client => client.Timeout = defaultTimeout);
        services.AddHttpClient<IWebhookService, WebhookService>()
            .ConfigureHttpClient(client => client.Timeout = defaultTimeout);
        services.AddHttpClient<ISchemaDiscoveryService, SchemaDiscoveryService>()
            .ConfigureHttpClient(client => client.Timeout = longRunningTimeout);
        services.AddHttpClient<IQueryExecutionService, QueryExecutionService>()
            .ConfigureHttpClient(client => client.Timeout = longRunningTimeout);
        services.AddHttpClient<IRequestLogService, RequestLogService>()
            .ConfigureHttpClient(client => client.Timeout = defaultTimeout);
        services.AddHttpClient<ICustomDataSchemaService, CustomDataSchemaService>()
            .ConfigureHttpClient(client => client.Timeout = longRunningTimeout);
        services.AddHttpClient<ICustomDataService, CustomDataService>()
            .ConfigureHttpClient(client => client.Timeout = longRunningTimeout);
        services.AddHttpClient<IExternalDataImportService, ExternalDataImportService>()
            .ConfigureHttpClient(client => client.Timeout = longRunningTimeout);
        services.AddHttpClient<IApiSchemaInferenceService, ApiSchemaInferenceService>()
            .ConfigureHttpClient(client => client.Timeout = defaultTimeout);

        // Database
        var connectionStringName = string.IsNullOrWhiteSpace(concreteOptions.ConnectionStringName) ? "EPiServerDB" : concreteOptions.ConnectionStringName;
        var connectionString = configuration?.GetConnectionString(connectionStringName) ?? string.Empty;
        services.SetUpOptiGraphExtensionsDatabase(connectionString);

        // If you are extending the CMS Editor Interface with an IFrameComponent, then you will need to declare the module here.
        // This will require a corresponding module.config file in the modules/_protected/OptiGraphExtensions folder within the website.
        services.Configure<ProtectedModuleOptions>(
            options =>
            {
                if (!options.Items.Any(x => string.Equals(x.Name, OptiGraphExtensionsConstants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                {
                    options.Items.Add(new ModuleDetails { Name = OptiGraphExtensionsConstants.ModuleName });
                }
            });

        return services;
    }

    public static void UseOptiGraphExtensions(this IApplicationBuilder builder)
    {
        using var serviceScope = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = serviceScope.ServiceProvider.GetService<OptiGraphExtensionsDataContext>();
        context?.Database.Migrate();
    }

    internal static void SetUpOptiGraphExtensionsDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OptiGraphExtensionsDataContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("OptiGraphExtensions");
            });

            // Enable connection pooling and optimize for performance
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(true);
            options.EnableDetailedErrors(false);
        }, ServiceLifetime.Scoped);

        services.AddScoped<IOptiGraphExtensionsDataContext, OptiGraphExtensionsDataContext>();
        services.AddScoped(provider => new Lazy<IOptiGraphExtensionsDataContext>(() => provider.GetRequiredService<IOptiGraphExtensionsDataContext>()));

        // Register caching services
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();

        // Register common services
        services.AddScoped<IGraphConfigurationValidator, GraphConfigurationValidator>();
        services.AddScoped<IOptiGraphConfigurationService, OptiGraphConfigurationService>();
        services.AddScoped<IComponentErrorHandler, ComponentErrorHandler>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped(typeof(IPaginationService<>), typeof(PaginationService<>));
        
        // Register validation services
        services.AddScoped<IValidationService<CreateSynonymRequest>, AttributeValidationService<CreateSynonymRequest>>();
        services.AddScoped<IValidationService<UpdateSynonymRequest>, AttributeValidationService<UpdateSynonymRequest>>();
        services.AddScoped<IValidationService<CreatePinnedResultRequest>, AttributeValidationService<CreatePinnedResultRequest>>();
        services.AddScoped<IValidationService<UpdatePinnedResultRequest>, AttributeValidationService<UpdatePinnedResultRequest>>();
        services.AddScoped<IValidationService<CreatePinnedResultsCollectionRequest>, AttributeValidationService<CreatePinnedResultsCollectionRequest>>();
        services.AddScoped<IValidationService<UpdatePinnedResultsCollectionRequest>, AttributeValidationService<UpdatePinnedResultsCollectionRequest>>();

        // Register synonym services with caching decorators
        services.AddScoped<SynonymRepository>();
        services.AddScoped<ISynonymRepository>(provider =>
        {
            var baseRepository = provider.GetRequiredService<SynonymRepository>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            return new CachedSynonymRepository(baseRepository, cacheService);
        });
        services.AddScoped<ISynonymService, SynonymService>();
        services.AddScoped<ISynonymGraphSyncService, SynonymGraphSyncService>();
        
        // Register pinned results repositories and services with caching decorators
        services.AddScoped<PinnedResultRepository>();
        services.AddScoped<IPinnedResultRepository>(provider =>
        {
            var baseRepository = provider.GetRequiredService<PinnedResultRepository>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            return new CachedPinnedResultRepository(baseRepository, cacheService);
        });

        services.AddScoped<PinnedResultsCollectionRepository>();
        services.AddScoped<IPinnedResultsCollectionRepository>(provider =>
        {
            var baseRepository = provider.GetRequiredService<PinnedResultsCollectionRepository>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            return new CachedPinnedResultsCollectionRepository(baseRepository, cacheService);
        });

        services.AddScoped<IPinnedResultService, PinnedResultService>();
        services.AddScoped<IPinnedResultsGraphSyncService, PinnedResultsGraphSyncService>();
        services.AddScoped<IPinnedResultsCollectionService, PinnedResultsCollectionService>();

        services.AddScoped<IPinnedResultsValidationService, PinnedResultsValidationService>();
        services.AddScoped<ISynonymValidationService, SynonymValidationService>();

        // Register webhook services
        services.AddScoped<IWebhookValidationService, WebhookValidationService>();

        // Register request log services
        services.AddScoped<IRequestLogExportService, RequestLogExportService>();

        // Register Query Library services
        services.AddScoped<SavedQueryRepository>();
        services.AddScoped<ISavedQueryRepository>(provider =>
        {
            var baseRepository = provider.GetRequiredService<SavedQueryRepository>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            return new CachedSavedQueryRepository(baseRepository, cacheService);
        });
        services.AddScoped<IQueryBuilderService, QueryBuilderService>();
        services.AddScoped<IRawQueryService, RawQueryService>();
        services.AddScoped<ICsvExportService, CsvExportService>();
        services.AddScoped<ISavedQueryService, SavedQueryService>();

        // Register Custom Data services
        services.AddScoped<ICustomDataValidationService, CustomDataValidationService>();
        services.AddScoped<INdJsonBuilderService, NdJsonBuilderService>();
        services.AddScoped<ISchemaParserService, SchemaParserService>();
        services.AddScoped<IValidationService<CreateSchemaRequest>, AttributeValidationService<CreateSchemaRequest>>();
        services.AddScoped<IValidationService<SyncDataRequest>, AttributeValidationService<SyncDataRequest>>();

        // Register Import Configuration repository
        services.AddScoped<IImportConfigurationRepository, ImportConfigurationRepository>();

        // Register Import Execution History repository
        services.AddScoped<IImportExecutionHistoryRepository, ImportExecutionHistoryRepository>();

        // Register Scheduled Import services
        services.AddScoped<IScheduledImportService, ScheduledImportService>();
        services.AddScoped<IImportNotificationService, ImportNotificationService>();
    }
}
