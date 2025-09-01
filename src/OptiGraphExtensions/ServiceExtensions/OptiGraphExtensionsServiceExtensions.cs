using EPiServer.Shell.Modules;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OptiGraphExtensions.ServiceExtensions;

public static class OptiGraphExtensionsServiceExtensions
{
    public static IServiceCollection AddOptiGraphExtensions(
        this IServiceCollection services,
        Action<AuthorizationOptions>? authorizationOptions = null)
    {
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

        // If you are extending the CMS Editor Interface with an IFrameComponent, then you will need to declare the module here.
        // This will require a corresponding module.config file in the modules/_protected/OptiGraphExtensions folder within the website.
        services.Configure<ProtectedModuleOptions>(
            options =>
            {
                if (!options.Items.Any(x => string.Equals(x.Name, "OptiGraphExtensions", StringComparison.OrdinalIgnoreCase)))
                {
                    options.Items.Add(new ModuleDetails { Name = "OptiGraphExtensions" });
                }
            });

        return services;
    }

    public static IApplicationBuilder UseOptiGraphExtensions(this IApplicationBuilder builder)
    {
        // Set up your pipelines here.

        return builder;
    }
}
