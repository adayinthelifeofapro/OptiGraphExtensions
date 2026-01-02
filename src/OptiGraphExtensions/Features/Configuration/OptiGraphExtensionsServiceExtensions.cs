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

        services.AddHttpClient<ISynonymGraphSyncService, SynonymGraphSyncService>();
        services.AddHttpClient<IPinnedResultsGraphSyncService, PinnedResultsGraphSyncService>();
        services.AddHttpClient<IContentSearchService, ContentSearchService>();

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
        services.AddScoped<IAuthenticationService, AuthenticationService>();
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

        // Register request mappers
        services.AddScoped<IRequestMapper<SynonymModel, CreateSynonymRequest, UpdateSynonymRequest>, SynonymRequestMapper>();
        services.AddScoped<IRequestMapper<PinnedResultModel, CreatePinnedResultRequest, UpdatePinnedResultRequest>, PinnedResultRequestMapper>();
        services.AddScoped<IRequestMapper<PinnedResultsCollectionModel, CreatePinnedResultsCollectionRequest, UpdatePinnedResultsCollectionRequest>, PinnedResultsCollectionRequestMapper>();
        
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
    }
}
