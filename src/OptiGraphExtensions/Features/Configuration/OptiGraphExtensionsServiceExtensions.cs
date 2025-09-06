using EPiServer.Shell.Modules;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;

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
        });

        services.AddScoped<IOptiGraphExtensionsDataContext, OptiGraphExtensionsDataContext>();
        services.AddScoped(provider => new Lazy<IOptiGraphExtensionsDataContext>(() => provider.GetRequiredService<IOptiGraphExtensionsDataContext>()));
    }
}
