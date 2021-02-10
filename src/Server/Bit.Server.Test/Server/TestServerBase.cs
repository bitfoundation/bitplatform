using Bit.Core.Implementations;
using Bit.Http.Implementations;
using Bit.OData.Implementations;
using Bit.Signalr;
using Bit.Signalr.Implementations;
using IdentityModel.Client;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Refit;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Test.Server
{
    public abstract class TestServerBase : ITestServer
    {
        public virtual string Uri { get; protected set; } = default!;

        public virtual ODataBatch BuildODataBatchClient(TokenResponse? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null)
        {
            return new ODataBatch(BuildODataClient(token, odataRouteName, odataClientSettings));
        }

        public virtual IODataClient BuildODataClient(TokenResponse? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null)
        {
            Action<HttpRequestMessage>? originalBeforeRequest = odataClientSettings?.BeforeRequest;

            odataClientSettings ??= new ODataClientSettings { };

            odataClientSettings.IncludeAnnotationsInResults = false;
            odataClientSettings.AdapterFactory = odataClientSettings.AdapterFactory ?? new DefaultODataAdapterFactory();

            if (odataClientSettings.BaseUri == default)
                odataClientSettings.BaseUri = new Uri($"{Uri}odata/{odataRouteName}/");

            odataClientSettings.BeforeRequest = (message) =>
            {
                if (token != null)
                {
                    message.Headers.Add("Authorization",
                        $"{token.TokenType} {token.AccessToken}");
                }

                originalBeforeRequest?.Invoke(message);
            };

            if (odataClientSettings.OnCreateMessageHandler == default)
                odataClientSettings.OnCreateMessageHandler = GetHttpMessageHandler;

            odataClientSettings.NameMatchResolver = ODataNameMatchResolver.AlpahumericCaseInsensitive;

            return new ODataClient(odataClientSettings);
        }

        protected abstract HttpMessageHandler GetHttpMessageHandler();

        public virtual async Task<IHubProxy> BuildSignalRClient(TokenResponse? token = null, Action<string, dynamic>? onMessageReceived = null)
        {
            HubConnection hubConnection = new HubConnection(Uri);

            if (token != null)
                hubConnection.Headers.Add("Authorization", $"{token.TokenType} {token.AccessToken}");

            IHubProxy hubProxy = hubConnection.CreateHubProxy(nameof(MessagesHub));

            if (onMessageReceived != null)
            {
                hubProxy.On<string, string>("OnMessageReceived", (key, dataAsJson) =>
                {
                    onMessageReceived(key, new TestSignalRMessageContentFormatter().Deserialize<dynamic>(dataAsJson));
                });
            }

            await hubConnection.Start(new ServerSentEventsTransport(new SignalRHttpClient(GetHttpMessageHandler())));

            return hubProxy;
        }

        public class TestSignalRMessageContentFormatter : SignalRMessageContentFormatter
        {
            public override T Deserialize<T>(string objAsStr)
            {
                return JsonConvert.DeserializeObject<T>(objAsStr, GetSettings())!;
            }
        }

        public abstract void Dispose();

        public virtual RemoteWebDriver BuildWebDriver(RemoteWebDriverOptions? options = null)
        {
            if (options == null)
                options = new RemoteWebDriverOptions();

            //FirefoxDriver driver = new FirefoxDriver();

            ChromeOptions chromeOptions = new ChromeOptions
            {

            };

            //chromeOptions.AddArguments("--lang=fa");

            ChromeDriver driver = new ChromeDriver(chromeOptions);

            //InternetExplorerDriver driver = new InternetExplorerDriver();

            //DesiredCapabilities capabilities = new DesiredCapabilities();

            //capabilities.SetCapability("deviceName", "donatello");
            //capabilities.SetCapability("platformVersion", "4.2.2");
            //capabilities.SetCapability("uid", "192.168.21.80:5555");
            //capabilities.SetCapability("fullReset", "True");
            //capabilities.SetCapability(MobileCapabilityType.App, "Browser");
            //capabilities.SetCapability("platformName", "Android");

            //AndroidDriver<AndroidElement> driver = new AndroidDriver<AndroidElement>(new Uri("http://localhost:4723/wd/hub"), capabilities);

            try
            {
                if (options.Uri != null)
                    driver.Navigate().GoToUrl($@"{Uri}{options.Uri}");
                else if (options.Token != null)
                    driver.Navigate().GoToUrl($@"{Uri}SignIn#id_token=0&access_token={options.Token.AccessToken}&token_type={options.Token.TokenType}&expires_in=86400&scope=openid profile user_info&state={{""pathname"":""/""}}&session_state=0");
                else
                    driver.Navigate().GoToUrl(Uri);

                if (options.ClientSideTest == true)
                    driver.GetElementById("testsConsole");
            }
            catch
            {
                driver.Dispose();
                throw;
            }

            return driver;
        }

        public virtual void Initialize(string uri)
        {
            Uri = uri;
        }

        public virtual async Task<TokenResponse> Login(string userName, string password, string clientId, string secret = "secret", IDictionary<string, string?>? acr_values = null)
        {
            if (userName == null)
                throw new ArgumentNullException(nameof(userName));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (clientId == null)
                throw new ArgumentNullException(nameof(clientId));

            if (secret == null)
                throw new ArgumentNullException(nameof(secret));

            HttpClient client = BuildHttpClient();

            var parameters = new Dictionary<string, string> { };

            if (acr_values != null)
            {
                parameters.Add("acr_values", string.Join(" ", acr_values.Select(p => $"{p.Key}:{p.Value}")));
            }

            TokenResponse tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = "core/connect/token",
                ClientSecret = secret,
                ClientId = clientId,
                Scope = "openid profile user_info",
                UserName = userName,
                Password = password,
                Parameters = parameters
            });

            if (tokenResponse.IsError)
            {
                throw tokenResponse.Exception ?? new Exception($"{tokenResponse.Error} {tokenResponse.Raw}");
            }

            return tokenResponse;
        }

        public virtual string GetLoginUrl(string? client_id = null, Uri? redirect_uri = null, object? state = null, IDictionary<string, string?>? acr_values = null)
        {
            state ??= new { };

            string relativeUri = $"InvokeLogin?state={JsonConvert.SerializeObject(state)}";

            if (redirect_uri != null)
                relativeUri += $"&redirect_uri={redirect_uri}";
            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";
            if (acr_values != null)
                relativeUri += $"&acr_values={string.Join(" ", acr_values.Select(p => $"{p.Key}:{p.Value}"))}";

            return relativeUri;
        }

        public virtual HttpClient BuildHttpClient(TokenResponse? token = null)
        {
            HttpClient client = HttpClientFactory.Create(GetHttpMessageHandler());

            if (token != null)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"{token.TokenType} {token.AccessToken}");
            }

            client.BaseAddress = new Uri(Uri);

            return client;
        }

        public virtual TService BuildRefitClient<TService>(TokenResponse? token = null)
        {
            return RestService.For<TService>(BuildHttpClient(token), new RefitSettings
            {
                ContentSerializer = new BitRefitJsonContentSerializer()
            });
        }
    }
}
