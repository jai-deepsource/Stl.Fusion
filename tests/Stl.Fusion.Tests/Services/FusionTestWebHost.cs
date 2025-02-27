using System.Reflection;
using Microsoft.Extensions.Hosting;
using Stl.Fusion.Server;
#if NETCOREAPP
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#else
using Owin;
using System.Web.Http;
#endif

namespace Stl.Fusion.Tests.Services;

public class FusionTestWebHostOptions
{
#if NETFRAMEWORK
    public Type[]? ControllerTypes { get; set; }
#endif
}

public class FusionTestWebHost : TestWebHostBase
{
    public IServiceCollection BaseServices { get; }
    public FusionTestWebHostOptions Options { get; }

    public FusionTestWebHost(IServiceCollection baseServices, FusionTestWebHostOptions options)
    {
        BaseServices = baseServices;
        Options = options;
    }

    protected override void ConfigureHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            // Copy all services from the base service provider here
            services.AddRange(BaseServices);

            // Since we copy all services here,
            // only web-related ones must be added to services
            services.AddFusion(fusion => {
                fusion.AddWebServer();
#if NETCOREAPP
                fusion.AddAuthentication(auth => auth.AddServer());
#endif
            });

#if NETFRAMEWORK
            if (Options.ControllerTypes != null)
                services.AddControllersAsServices(Options.ControllerTypes);
#else
            // Web
            services.AddRouting();
            services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

            // Testing
            services.AddHostedService<ApplicationPartsLogger>();
#endif
        });
    }

#if NETCOREAPP
    protected override void ConfigureWebHost(IWebHostBuilder webHost)
    {
        webHost.Configure((ctx, app) => {
            app.UseWebSockets();
            app.UseFusionSession();

            // API controllers
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(name: "DefaultApi", pattern: "api/{controller}/{action}");
                endpoints.MapControllers();
                endpoints.MapFusionWebSocketServer();
            });
        });
    }
#else
    protected override void ConfigureHttp(IServiceProvider services, HttpConfiguration config)
    {
        base.ConfigureHttp(services, config);

        config.Formatters.Insert(0, new TextMediaTypeFormatter());
    }

    protected override void ConfigureAppBuilder(IServiceProvider services, IAppBuilder builder)
    {
        base.ConfigureAppBuilder(services, builder);

        builder.MapFusionWebSocketServer(services);
    }
#endif
}
