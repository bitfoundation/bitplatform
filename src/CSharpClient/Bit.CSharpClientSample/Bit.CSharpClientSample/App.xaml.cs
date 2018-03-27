using Autofac;
using Bit.CSharpClientSample.ViewModels;
using Bit.CSharpClientSample.Views;
using Bit.Model.Events;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using IdentityModel.Client;
using Prism;
using Prism.Autofac;
using Prism.Events;
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

            Container.Resolve<IEventAggregator>()
                .GetEvent<TokenExpiredEvent>()
                .Subscribe(async tokenExpiredEvent => await NavigationService.NavigateAsync("Login"), ThreadOption.UIThread);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterNavigation<NavigationPage>("Nav");

            containerRegistry.RegisterNavigation<LoginView, LoginViewModel>("Login");
            containerRegistry.RegisterNavigation<MainView, MainViewModel>("Main");

            Simple.OData.Client.V4Adapter.Reference();

            containerRegistry.GetBuilder()
                .RegisterType<HttpClientHandler>()
                .Named<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler)
                .SingleInstance();

            containerRegistry.GetBuilder().Register<HttpMessageHandler>(c =>
            {
                return new AuthenticatedHttpMessageHandler(c.Resolve<IEventAggregator>(), c.Resolve<ISecurityService>(), c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            })
            .Named<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler)
            .SingleInstance();

            containerRegistry.GetBuilder().Register<IODataClient>(c =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);
                IODataClient odataClient = new ODataClient(new ODataClientSettings(new Uri(c.Resolve<IClientAppProfile>().HostUri, "odata/Test/"))
                {
                    OnCreateMessageHandler = () => authenticatedHttpMessageHandler,
                    RenewHttpConnection = false
                });
                return odataClient;
            });

            containerRegistry.GetBuilder().Register<HttpClient>(c =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = c.ResolveNamed<HttpMessageHandler>(ContractKeys.AuthenticatedHttpMessageHandler);
                HttpClient httpClient = new HttpClient(authenticatedHttpMessageHandler) { BaseAddress = c.Resolve<IClientAppProfile>().HostUri };
                return httpClient;
            }).SingleInstance();

            containerRegistry.RegisterSingleton<ISecurityService, DefaultSecurityService>();

            containerRegistry.GetBuilder().Register<IClientAppProfile>(c => new DefaultClientAppProfile
            {
                HostUri = new Uri("http://indie-ir001.ngrok.io/"),
                OAuthRedirectUri = new Uri("Test://oauth2redirect"),
                AppName = "Test"
            }).SingleInstance();

            containerRegistry.RegisterSingleton<IDateTimeProvider, DefaultDateTimeProvider>();

            containerRegistry.GetBuilder().Register(c => AccountStore.Create()).SingleInstance();

            containerRegistry.GetBuilder().Register<IContainerProvider>(c => Container).SingleInstance();

            containerRegistry.GetBuilder().Register<TokenClient>((c, parameters) =>
            {
                return new TokenClient(address: new Uri(c.Resolve<IClientAppProfile>().HostUri, "core/connect/token").ToString(), clientId: parameters.Named<string>("clientId"), clientSecret: parameters.Named<string>("secret"), innerHttpMessageHandler: c.ResolveNamed<HttpMessageHandler>(ContractKeys.DefaultHttpMessageHandler));
            });
        }
    }
}
