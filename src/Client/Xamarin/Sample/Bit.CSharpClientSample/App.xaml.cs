using Autofac;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models.Events;
using Bit.CSharpClientSample.Data;
using Bit.CSharpClientSample.ViewModels;
using Bit.CSharpClientSample.Views;
using Bit.Http.Contracts;
using Bit.Sync.ODataEntityFrameworkCore.Contracts;
using Bit.Tests.Model.Dto;
using Bit.View;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
[assembly: Xamarin.Forms.Internals.Preserve]

namespace Bit.CSharpClientSample
{
    public partial class App : BitApplication
    {
        static App()
        {
            BitCSharpClientControls.XamlInit();
            BitApplication.XamlInit();
        }

        public App(IPlatformInitializer initializer)
                    : base(initializer)
        {

        }

        protected override async Task OnInitializedAsync()
        {
            InitializeComponent();

#if ANDROID || IOS
            if (Device.RuntimePlatform != Device.UWP && Microsoft.Maui.Essentials.DeviceInfo.DeviceType == Microsoft.Maui.Essentials.DeviceType.Physical)
            {
                Microsoft.Maui.Essentials.Accelerometer.Start(Microsoft.Maui.Essentials.SensorSpeed.UI);
                Microsoft.Maui.Essentials.Accelerometer.ShakeDetected += async delegate
                {
                    await LocalTelemetryService.Current.OpenConsole();
                };
            }
#else
            if (Device.RuntimePlatform != Device.UWP && Xamarin.Essentials.DeviceInfo.DeviceType == Xamarin.Essentials.DeviceType.Physical)
            {
                Xamarin.Essentials.Accelerometer.Start(Xamarin.Essentials.SensorSpeed.UI);
                Xamarin.Essentials.Accelerometer.ShakeDetected += async delegate
                {
                    await LocalTelemetryService.Current.OpenConsole();
                };
            }
#endif

            bool isLoggedIn = await Container.Resolve<ISecurityService>().IsLoggedInAsync();

            if (isLoggedIn)
            {
                await NavigationService.NavigateAsync("/Nav/Main");
            }
            else
            {
                await NavigationService.NavigateAsync("/Nav/Login");
            }

            IEventAggregator eventAggregator = Container.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<UnauthorizedResponseEvent>()
                .SubscribeAsync(async tokenExpiredEvent => await NavigationService.NavigateAsync("/Nav/Login"), ThreadOption.UIThread);

            await base.OnInitializedAsync();
        }

        protected override void RegisterTypes(IDependencyManager dependencyManager, IContainerRegistry containerRegistry, ContainerBuilder containerBuilder, IServiceCollection services)
        {
            containerRegistry.RegisterForNav<NavigationPage>("Nav");

            containerRegistry.RegisterForNav<LoginView, LoginViewModel>("Login");
            containerRegistry.RegisterForNav<MainView, MainViewModel>("Main");
            containerRegistry.RegisterForNav<TestView, TestViewModel>("Test");
            containerRegistry.RegisterForNav<SampleView, SampleViewModel>("Sample");

            containerRegistry.RegisterForRegionNav<RegionAView, RegionAViewModel>("RegionA");
            containerRegistry.RegisterForRegionNav<RegionBView, RegionBViewModel>("RegionB");
            containerRegistry.RegisterForRegionNav<RegionCView, RegionCViewModel>("RegionC");
            containerRegistry.RegisterForRegionNav<RegionDView, RegionDViewModel>("RegionD");

            dependencyManager.RegisterUsing(resolver => new AnimateNavigation(() => true));

            const string developerMachineIp = "192.168.0.179";

#if ANDROID || IOS
            bool isVirtual = Microsoft.Maui.Essentials.DeviceInfo.DeviceType == Microsoft.Maui.Essentials.DeviceType.Virtual;
#else
            bool isVirtual = Xamarin.Essentials.DeviceInfo.DeviceType == Xamarin.Essentials.DeviceType.Virtual;
#endif

            containerBuilder.Register<IClientAppProfile>(c => new DefaultClientAppProfile
            {
                HostUri = new Uri((Device.RuntimePlatform == Device.Android && isVirtual) ? "http://10.0.2.2" : Device.RuntimePlatform == Device.UWP ? "http://127.0.0.1" : $"http://{developerMachineIp}"),
                OAuthRedirectUri = new Uri("test-oauth://"),
                AppName = "Test",
                ODataRoute = "odata/Test/"
            }).SingleInstance();

            dependencyManager.RegisterRequiredServices();
            dependencyManager.RegisterHttpClient();
            dependencyManager.RegisterODataClient();
            dependencyManager.RegisterIdentityClient();
            dependencyManager.RegisterRefitClient();
            dependencyManager.RegisterRefitService<ISimpleApi>();

            dependencyManager.RegisterDbContext<SampleDbContext>();

            dependencyManager.RegisterDefaultSyncService((serviceProvider, syncService) =>
            {
                syncService.AddDtoSetSyncConfig(new DtoSetSyncConfig
                {
                    DtoSetName = nameof(SampleDbContext.TestCustomers),
                    OnlineDtoSet = odataClient => odataClient.For(nameof(SampleDbContext.TestCustomers)),
                    OfflineDtoSet = dbContext => dbContext.Set<TestCustomerDto>()
                });
            });

#if DEBUG
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddDebug();
            });
#endif

            dependencyManager.RegisterSignalr();

            base.RegisterTypes(dependencyManager, containerRegistry, containerBuilder, services);
        }
    }
}
