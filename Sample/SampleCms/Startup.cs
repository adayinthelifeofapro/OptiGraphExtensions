using EPiServer.Cms.Shell;
using EPiServer.Cms.Shell.UI;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.ContentApi.Core.DependencyInjection;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

using Geta.Optimizely.Sitemaps;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.Configuration;

using Stott.Optimizely.RobotsHandler.Configuration;
using Stott.Security.Optimizely.Features.Configuration;

namespace SampleCms;

public class Startup
{
    private readonly IWebHostEnvironment _webHostingEnvironment;

    public Startup(IWebHostEnvironment webHostingEnvironment)
    {
        _webHostingEnvironment = webHostingEnvironment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (_webHostingEnvironment.IsDevelopment())
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(_webHostingEnvironment.ContentRootPath, "App_Data"));

            services.Configure<SchedulerOptions>(options => options.Enabled = false);
        }

        services.AddCmsAspNetIdentity<ApplicationUser>()
                .AddCms()
                .AddAdminUserRegistration(x => x.Behavior = RegisterAdminUserBehaviors.Enabled | RegisterAdminUserBehaviors.LocalRequestsOnly)
                .AddEmbeddedLocalization<Startup>();

        services.AddServerSideBlazor();

        services.AddSitemaps(x =>
        {
            x.EnableRealtimeSitemap = false;
            x.EnableRealtimeCaching = true;
            x.RealtimeCacheExpirationInMinutes = 60;
        });

        services.ConfigureContentApiOptions(o =>
        {
            o.IncludeInternalContentRoots = true;
            o.IncludeSiteHosts = true;
            // o.EnablePreviewFeatures = true; // optional
        });

        services.AddContentDeliveryApi();

        services.AddContentGraph();

        services.AddStottSecurity();
        services.AddRobotsHandler();
        services.AddOptiGraphExtensions(optiGraphExtensionsSetupOptions =>
        {
            optiGraphExtensionsSetupOptions.ConnectionStringName = "EPiServerDB";
        },
        authorizationOptions =>
        {
            authorizationOptions.AddPolicy(OptiGraphExtensionsConstants.AuthorizationPolicy, policy =>
            {
                policy.RequireRole("WebAdmins");
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseStottSecurity();
        app.UseOptiGraphExtensions();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapContent();
            endpoints.MapBlazorHub();
            endpoints.MapControllers();
        });
    }
}
