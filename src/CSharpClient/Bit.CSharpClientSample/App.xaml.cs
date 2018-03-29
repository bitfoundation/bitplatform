using Autofac;
using Bit.CSharpClientSample.ViewModels;
using Bit.CSharpClientSample.Views;
using Bit.Model.Events;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
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

            if (await Container.Resolve<ISecurityService>().IsLoggedInAsync())
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

            base.RegisterTypes(containerRegistry);
        }
    }
}
