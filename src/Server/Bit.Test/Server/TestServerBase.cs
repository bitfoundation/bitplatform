using Bit.Core.Implementations;
using Bit.Signalr;
using Bit.Signalr.Implementations;
using Bit.Test.SignalR;
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
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Test.Server
{
    public abstract class TestServerBase : ITestServer
    {
        public virtual string Uri { get; protected set; }

        public virtual ODataBatch BuildODataBatchClient(Action<HttpRequestMessage> beforeRequest = null,
                  Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string odataRouteName = "Test")
        {
            return new ODataBatch(BuildODataClient(beforeRequest, afterResponse, token, odataRouteName));
        }

        public virtual IODataClient BuildODataClient(Action<HttpRequestMessage> beforeRequest = null,
            Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string odataRouteName = "Test")
        {
            ODataClient client = new ODataClient(new ODataClientSettings(new Uri($"{Uri}odata/{odataRouteName}/"))
            {
                BeforeRequest = message =>
                {
                    if (token != null)
                    {
                        message.Headers.Add("Authorization",
                            $"{token.TokenType} {token.AccessToken}");
                    }

                    beforeRequest?.Invoke(message);
                },
                AfterResponse = message =>
                {
                    afterResponse?.Invoke(message);
                },
                OnCreateMessageHandler = GetHttpMessageHandler,
                NameMatchResolver = ODataNameMatchResolver.AlpahumericCaseInsensitive
            });

            return client;
        }

        protected abstract HttpMessageHandler GetHttpMessageHandler();

        public virtual async Task<IHubProxy> BuildSignalRClient(TokenResponse token = null, Action<string, dynamic> onMessageReceived = null)
        {
            HubConnection hubConnection = new HubConnection(Uri);

            if (token != null)
                hubConnection.Headers.Add("Authorization", $"{token.TokenType} {token.AccessToken}");

            IHubProxy hubProxy = hubConnection.CreateHubProxy(nameof(MessagesHub));

            if (onMessageReceived != null)
            {
                hubProxy.On<string, string>("OnMessageReceived", (key, dataAsJson) =>
                {
                    onMessageReceived(key, new TestSignalRMessageContentFormatter().DeSerialize<dynamic>(dataAsJson));
                });
            }

            await hubConnection.Start(new ServerSentEventsTransport(new SignalRHttpClient(GetHttpMessageHandler())));

            return hubProxy;
        }

        public class TestSignalRMessageContentFormatter : SignalRMessageContentFormatter
        {
            public override T DeSerialize<T>(string objAsStr)
            {
                return JsonConvert.DeserializeObject<T>(objAsStr, GetSettings());
            }
        }

        public abstract void Dispose();

        public virtual RemoteWebDriver BuildWebDriver(RemoteWebDriverOptions options = null)
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

        public virtual Task<TokenResponse> Login(string userName, string password, string clientId, string secret = "secret", IDictionary<string,string> parameters = null)
        {
            HttpClient client = BuildHttpClient();

            return client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = "core/connect/token",
                ClientSecret = secret,
                ClientId = clientId,
                Scope = "openid profile user_info",
                UserName = userName,
                Password = password,
                Parameters = parameters ?? new Dictionary<string, string> { }
            });
        }

        public HttpClient BuildHttpClient(TokenResponse token = null)
        {
            HttpClient client = HttpClientFactory.Create(GetHttpMessageHandler());

            if (token != null)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"{token.TokenType} {token.AccessToken}");
            }

            client.BaseAddress = new Uri(Uri);

            return client;
        }

        public virtual TService BuildRefitClient<TService>(TokenResponse token = null)
        {
            return RestService.For<TService>(BuildHttpClient(token), new RefitSettings
            {
                ContentSerializer = new JsonContentSerializer(DefaultJsonContentFormatter.SerializeSettings())
            });
        }
    }
}
