using Bit.Bortable;
using Bit.Belemetry;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Shared.Features.Identity;

namespace Boilerplate.Workbench;

/// <summary>
/// Registers Client.Core's services with stand-ins for what needs the app's server, then the Workbench's own services.
/// </summary>
public static partial class Program
{
    extension(WebApplicationBuilder builder)
    {
        public void AddWorkbenchProjectServices()
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            // Wins over Client.Core's, which only tries to add one: a view's accent color comes from its own store, not the browser.
            services.AddSingleton(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.None, FirstPaintStrategy = BitAccentColorFirstPaintStrategy.None });
            services.AddClientCoreProjectServices(configuration);

            services.AddHttpContextAccessor();
            // Stand-ins for what the app's client projects provide: device features, storage, sign-in state and error handling.
            services.AddScoped<IBitDeviceCoordinator, WorkbenchDeviceCoordinator>();
            services.AddScoped<WorkbenchStorageService>();
            services.AddScoped<IStorageService, WorkbenchViewStorage>();
            services.AddScoped<AuthenticationStateProvider, WorkbenchAuthenticationStateProvider>();
            // The access token, whether the session lately confirmed it's the user, and the refresh that confirms it.
            services.AddScoped<WorkbenchElevation>();
            services.AddScoped<IAuthTokenProvider, WorkbenchAuthTokenProvider>();
            services.AddScoped<IIdentityController, WorkbenchIdentityController>();
            services.AddScoped(sp =>
            {
                // Built as the app builds it, while the sign-in state itself comes from the stand-in provider above.
                var authManager = ActivatorUtilities.CreateInstance<AuthManager>(sp);
                authManager.OnInit();
                return authManager;
            });
            services.AddCascadingAuthenticationState();
            services.AddScoped<ClientExceptionHandlerBase, WorkbenchExceptionHandler>();
            services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());
            services.AddScoped(sp =>
            {
                // For code that asks for an HttpClient: it points at the Workbench itself, as the Workbench calls no app server, and
                // carries the view's access token, as the app's carries the user's, so what the Workbench serves at the app's
                // addresses knows who asked (See IWorkbenchEndpointRouteBuilderExtensions). The personal data export is answered in
                // the view, from its own account (See WorkbenchExportHandler).
                var request = sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request;
                var export = ActivatorUtilities.CreateInstance<WorkbenchExportHandler>(sp);
                export.InnerHandler = new SocketsHttpHandler();
                return new HttpClient(new WorkbenchBearerHandler(sp.GetRequiredService<IAuthTokenProvider>()) { InnerHandler = export })
                {
                    BaseAddress = request is null ? new Uri("http://localhost/") : new Uri($"{request.Scheme}://{request.Host}/")
                };
            });

            services.AddBitBortableServices();
            services.AddBitBmotionServices(options =>
            {
                options.ReducedMotion = BmReducedMotionMode.User;
                // Animates in CSS from the first paint, as content that waits for the circuit stays blank for seconds on a cold start.
                options.Prerender = BmPrerenderMode.Animate;
            });

            // Sends the browser's and the server's logs, traces and metrics to the OTLP endpoint the OTEL_ variables name, such as the Aspire dashboard's.
            services.AddBitBelemetryServices(options =>
            {
                options.ServiceName = "Boilerplate.Workbench";
                // With no consent banner, a visitor's browser reports errors only, while a developer's sends everything.
                options.Consent.Default = builder.Environment.IsDevelopment() ? BelemetryFeatures.All : BelemetryFeatures.Errors;
                options.Server.CollectDotnetTelemetry = true;
                options.Server.IgnoredRequestPaths.AddRange(["/_framework", "/_content", "/_blazor"]);
            });

            services.AddWorkbenchServices(configuration);

            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                // Room for the largest page state a browser hands back, at up to four bytes a character once it is JSON-escaped.
                .AddHubOptions(options => options.MaximumReceiveMessageSize = WorkbenchSnapshots.MaxLength * 4);
        }
    }
}
