using Autofac;
using Bit.CSharpClientSample.ViewModels;
using Bit.CSharpClientSample.Views;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Prism;
using Prism.Autofac;
using Prism.Ioc;
using Simple.OData.Client;
using System;
using System.Net.Http;
using Xamarin.Auth;
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
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterNavigation<NavigationPage>("Nav");

            containerRegistry.RegisterNavigation<LoginView, LoginViewModel>("Login");
            containerRegistry.RegisterNavigation<MainView, MainViewModel>("Main");

            Simple.OData.Client.V4Adapter.Reference();

            containerRegistry.GetBuilder().Register(c =>
            {
                ISecurityService securityService = c.Resolve<ISecurityService>();
                IODataClient odataClient = new ODataClient(new ODataClientSettings(new Uri(c.Resolve<IClientAppProfile>().HostUri, "odata/Test/"))
                {
                    OnCreateMessageHandler = () => new TokenHandler(securityService, new HttpClientHandler())
                });
                return odataClient;
            });

            containerRegistry.GetBuilder().Register(c =>
            {
                ISecurityService securityService = c.Resolve<ISecurityService>();
                HttpClient httpClient = new HttpClient(new TokenHandler(securityService, new HttpClientHandler())) { BaseAddress = c.Resolve<IClientAppProfile>().HostUri };
                return httpClient;
            }).SingleInstance();

            containerRegistry.RegisterSingleton<ISecurityService, DefaultSecurityService>();
            containerRegistry.GetBuilder().Register<IClientAppProfile>(c => new DefaultClientAppProfile
            {
                HostUri = new Uri("http://indie-ir001.ngrok.io/"),
                OAuthRedirectUri = new Uri("Test://oauth2redirect"),
                AppName = "Test"
            });
            containerRegistry.RegisterSingleton<IDateTimeProvider, DefaultDateTimeProvider>();

            containerRegistry.GetBuilder().Register(c => AccountStore.Create()).SingleInstance();
        }
    }
}
