using Autofac;
using Bit.CSharpClientSample.ViewModels;
using Bit.CSharpClientSample.Views;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using IdentityModel.Client;
using Prism;
using Prism.Autofac;
using Prism.Ioc;
using Simple.OData.Client;
using System;
using System.Net.Http;
using Xamarin.Auth;
using Xamarin.Auth.Presenters;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Bit.CSharpClientSample
{
    public class TestConfigProvider : IConfigProvider
    {
        public Uri HostUri => new Uri("http://indie-ir001.ngrok.io/");

        public string OAuthImplicitFlowClientId => "Test";

        public Uri OAuthImplicitFlowRedirectUri => new Uri("Test://oauth2redirect");

        public string OAuthResourceOwnerFlowClientId => "TestResOwner";

        public string OAuthResourceOwnerFlowSecret => "secret";

        public string AppName => "Test";
    }

    public partial class App : PrismApplication
    {
        public App(IPlatformInitializer initializer)
                    : base(initializer)
        {

        }

        protected async override void OnInitialized()
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
            containerRegistry.RegisterForNavigation<NavigationPage>("Nav");

            containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>("Login");
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>("Main");

            Simple.OData.Client.V4Adapter.Reference();

            containerRegistry.GetBuilder().Register(c =>
            {
                ISecurityService securityService = c.Resolve<ISecurityService>();
                ODataClient odataClient = new ODataClient(new ODataClientSettings(new Uri(c.Resolve<IConfigProvider>().HostUri, "odata/Test/"))
                {
                    OnCreateMessageHandler = () => new TokenHandler(securityService, new HttpClientHandler())
                });
                return odataClient;
            });

            containerRegistry.GetBuilder().Register(c =>
            {
                ISecurityService securityService = c.Resolve<ISecurityService>();
                HttpClient httpClient = new HttpClient(new TokenHandler(securityService, new HttpClientHandler())) { BaseAddress = c.Resolve<IConfigProvider>().HostUri };
                return httpClient;
            }).SingleInstance();

            containerRegistry.GetBuilder().Register(c =>
            {
                IConfigProvider configProvider = c.Resolve<IConfigProvider>();
                return new TokenClient(address: new Uri(configProvider.HostUri, "core/connect/token").ToString(), clientId: configProvider.OAuthResourceOwnerFlowClientId, clientSecret: configProvider.OAuthResourceOwnerFlowSecret);
            }).SingleInstance();

            containerRegistry.RegisterSingleton<BitOAuth2Authenticator>();
            containerRegistry.RegisterSingleton<OAuthLoginPresenter>();
            containerRegistry.RegisterSingleton<ISecurityService, DefaultSecurityService>();
            containerRegistry.RegisterSingleton<IConfigProvider, TestConfigProvider>();
            containerRegistry.RegisterSingleton<IDateTimeProvider, DefaultDateTimeProvider>();

            containerRegistry.GetBuilder().Register(c => AccountStore.Create()).SingleInstance();
        }
    }
}
