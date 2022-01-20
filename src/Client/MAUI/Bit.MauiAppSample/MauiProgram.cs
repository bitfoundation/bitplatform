using System;
using Autofac;
using Bit.Client.Web.Blazor.Implementation;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.MauiAppSample.Implementations;
using Bit.MauiAppSample.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Essentials;
using Microsoft.Maui.Hosting;
using Simple.OData.Client;

namespace Bit.MauiAppSample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            BitExceptionHandlerBase.Current = new SampleExceptionHandler();

            Simple.OData.Client.V4Adapter.Reference();

            AssemblyContainer.Current.Init();

            var builder = MauiApp.CreateBuilder();

            builder.Host.UseServiceProviderFactory(new BitServiceProviderFactory(ConfigureServicesImpl));

            builder
                .RegisterBlazorMauiWebView()
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            return builder.Build();
        }

        public static void ConfigureServicesImpl(IDependencyManager dependencyManager, IServiceCollection services, ContainerBuilder containerBuilder)
        {
            dependencyManager.RegisterAutoMapper();

            dependencyManager.RegisterRequiredServices();
            dependencyManager.RegisterHttpClient();
            dependencyManager.RegisterODataClient((serviceProvider, settings) =>
            {
                settings.PreferredUpdateMethod = ODataUpdateMethod.Put;
            });
            dependencyManager.RegisterIdentityClient();

            const string developerMachineIp = "192.168.0.179";

            dependencyManager.RegisterUsing<IClientAppProfile>(c => new DefaultClientAppProfile
            {
                HostUri = new Uri((Device.RuntimePlatform == Device.Android && DeviceInfo.DeviceType == DeviceType.Virtual) ? "http://10.0.2.2" : Device.RuntimePlatform == Device.UWP ? "http://127.0.0.1" : $"http://{developerMachineIp}"),
                Environment = "Development",
                ODataRoute = "odata/Test",
                AppName = "Test"
            }, lifeCycle: DependencyLifeCycle.SingleInstance);

            services.AddBlazorWebView();
            services.AddAuthorizationCore(config =>
            {
                config.AddPolicy("IsLoggedIn", policy => policy.RequireClaim("UserId"));
            });

            services.AddScoped<AuthenticationStateProvider, SampleAuthenticationStateProvider>();
            dependencyManager.RegisterUsing(resolver => (SampleAuthenticationStateProvider)resolver.Resolve<AuthenticationStateProvider>(), lifeCycle: DependencyLifeCycle.Transient);

            dependencyManager.Register<LoginViewModel>(lifeCycle: DependencyLifeCycle.Transient);
            dependencyManager.Register<MainViewModel>(lifeCycle: DependencyLifeCycle.Transient);
            dependencyManager.Register<HomeViewModel>(lifeCycle: DependencyLifeCycle.Transient);
        }
    }
}
