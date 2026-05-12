using EPiServer.Cms.Shell;
using EPiServer.Cms.Shell.UI;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Cms.UI.VisitorGroups;
using EPiServer.Data;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

using Optimizely.Graph.DependencyInjection;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.Configuration;

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

        services.Configure<DataAccessOptions>(options =>
        {
            options.UpdateDatabaseCompatibilityLevel = true;
        });

        services.AddCmsAspNetIdentity<ApplicationUser>()
                .AddCms()
                .AddVisitorGroupsUI()
                .AddAdminUserRegistration(x => x.Behavior = RegisterAdminUserBehaviors.Enabled | RegisterAdminUserBehaviors.LocalRequestsOnly)
                .AddEmbeddedLocalization<Startup>();

        services.AddServerSideBlazor();

        services.AddContentGraph();

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
