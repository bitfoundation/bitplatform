using Autofac;
using Bit.CSharpClientSample.Data;
using Bit.CSharpClientSample.ViewModels;
using Bit.CSharpClientSample.Views;
using Bit.Model.Events;
using Bit.Tests.Model.Dto;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Plugin.Connectivity.Abstractions;
using Prism;
using Prism.Autofac;
using Prism.Events;
using Prism.Ioc;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Bit.CSharpClientSample
{
    public partial class App : BitApplication
    {
        public App(IPlatformInitializer initializer)
                    : base(initializer)
        {

        }

        protected override async void OnInitialized()
        {
            InitializeComponent();

            bool isLoggedIn = await Container.Resolve<ISecurityService>().IsLoggedInAsync();

            if (isLoggedIn)
            {
                await NavigationService.NavigateAsync("Nav/Main");
            }
            else
            {
                await NavigationService.NavigateAsync("Login");
            }

            IEventAggregator eventAggregator = Container.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<TokenExpiredEvent>()
                .Subscribe(async tokenExpiredEvent => await NavigationService.NavigateAsync("Login"), ThreadOption.UIThread);

            base.OnInitialized();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>("Nav");

            containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>("Login");
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>("Main");

            containerRegistry.GetBuilder().Register<IClientAppProfile>(c => new DefaultClientAppProfile
            {
                HostUri = new Uri("http://indie-ir001.ngrok.io/"),
                OAuthRedirectUri = new Uri("Test://oauth2redirect"),
                AppName = "Test",
                ODataRoute = "odata/Test/"
            }).SingleInstance();

            containerRegistry.RegisterRequiredServices();
            containerRegistry.RegisterHttpClient();
            containerRegistry.RegisterODataClient();
            containerRegistry.RegisterIdentityClient();

            containerRegistry.Register<SampleDbContext>();

            containerRegistry.GetBuilder().Register(c =>
            {
                ISyncService syncService = new DefaultSyncService<SampleDbContext>(c.Resolve<IConnectivity>(), c.Resolve<IContainerProvider>());

                syncService.AddDtoSetSyncConfig(new DtoSetSyncConfig
                {
                    DtoSetName = nameof(SampleDbContext.TestCustomers),
                    OnlineDtoSet = odataClient => odataClient.For(nameof(SampleDbContext.TestCustomers)),
                    OfflineDtoSet = dbContext => dbContext.Set<TestCustomerDto>()
                });

                return syncService;

            }).SingleInstance();

            containerRegistry.RegisterPopupService();

            containerRegistry.RegisterForPopup<TestView, TestViewModel>("Test");

            base.RegisterTypes(containerRegistry);
        }
    }
}
