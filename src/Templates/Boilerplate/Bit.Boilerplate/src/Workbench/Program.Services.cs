using Bit.Bortable;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Workbench.Infrastructure.Services.StandIns;

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
            services.AddScoped<IStorageService, WorkbenchStorageService>();
            services.AddScoped<AuthenticationStateProvider, WorkbenchAuthenticationStateProvider>();
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
                // For code that asks for an HttpClient: it points at the Workbench itself, as the Workbench calls no app server.
                var request = sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request;
                return new HttpClient
                {
                    BaseAddress = request is null ? new Uri("http://localhost/") : new Uri($"{request.Scheme}://{request.Host}/")
                };
            });

            services.AddBitBortableServices();
            services.AddBitBmotionServices(options => options.ReducedMotion = BmReducedMotionMode.User);

            services.AddWorkbenchServices(configuration);

            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                // Room for the largest page state a browser hands back, at up to four bytes a character once it is JSON-escaped.
                .AddHubOptions(options => options.MaximumReceiveMessageSize = WorkbenchSnapshots.MaxLength * 4);
        }
    }
}
