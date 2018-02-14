using Autofac;
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
            await NavigationService.NavigateAsync("Nav/Main");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>("Nav");

            containerRegistry.RegisterForNavigation<MainView, MainViewModel>("Main");

            Simple.OData.Client.V4Adapter.Reference();

            containerRegistry.GetBuilder().Register(c => new ODataClient(new Uri(c.Resolve<IConfigProvider>().HostUri, "odata/Test/")));
            containerRegistry.GetBuilder().Register(c => new HttpClient() { BaseAddress = c.Resolve<IConfigProvider>().HostUri }).SingleInstance();
            containerRegistry.GetBuilder().Register(c =>
            {
                IConfigProvider configProvider = c.Resolve<IConfigProvider>();
                return new TokenClient(address: new Uri(configProvider.HostUri, "core/connect/token").ToString(), clientId: configProvider.OAuthResourceOwnerFlowClientId, clientSecret: configProvider.OAuthResourceOwnerFlowSecret);
            }).SingleInstance();
            containerRegistry.Register<BitOAuth2Authenticator>();
            containerRegistry.Register<OAuthLoginPresenter>();
            containerRegistry.Register<ISecurityService, DefaultSecurityService>();
            containerRegistry.Register<IConfigProvider, TestConfigProvider>();

            containerRegistry.GetBuilder().Register(c => AccountStore.Create()).SingleInstance();
        }
    }
}
