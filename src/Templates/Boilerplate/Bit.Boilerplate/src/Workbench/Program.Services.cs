using Bit.Bortable;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Workbench.Infrastructure.Services.StandIns;

namespace Boilerplate.Workbench;

public static partial class Program
{
    extension(WebApplicationBuilder builder)
    {
        public void AddWorkbenchProjectServices()
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            services.AddSingleton(new BitAccentColorConfig { Persistence = BitAccentColorPersistence.None, FirstPaintStrategy = BitAccentColorFirstPaintStrategy.None });
            services.AddClientCoreProjectServices(configuration);

            services.AddHttpContextAccessor();
            services.AddScoped<IBitDeviceCoordinator, WorkbenchDeviceCoordinator>();
            services.AddScoped<IStorageService, WorkbenchStorageService>();
            services.AddScoped<AuthenticationStateProvider, WorkbenchAuthenticationStateProvider>();
            services.AddScoped(sp =>
            {
                var authManager = ActivatorUtilities.CreateInstance<AuthManager>(sp);
                authManager.OnInit();
                return authManager;
            });
            services.AddCascadingAuthenticationState();
            services.AddScoped<ClientExceptionHandlerBase, WorkbenchExceptionHandler>();
            services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());
            services.AddScoped(sp =>
            {
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
                .AddInteractiveServerComponents();
        }
    }
}
