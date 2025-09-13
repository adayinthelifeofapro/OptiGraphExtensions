using EPiServer.Shell.Modules;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Repositories;
using OptiGraphExtensions.Features.Synonyms.Services;
using OptiGraphExtensions.Features.PinnedResults.Repositories;
using OptiGraphExtensions.Features.PinnedResults.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Validation;
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

        // API Controllers with JSON configuration to handle circular references
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // HttpClient for API calls
        services.AddHttpClient();
        services.AddHttpContextAccessor();

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

    // ...existing code...