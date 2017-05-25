using System;
using System.Net.Http;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Chrome;
using Microsoft.AspNet.SignalR.Client;
using Foundation.Api.Middlewares.SignalR;
using Microsoft.AspNet.SignalR.Client.Transports;
using Foundation.Api.Middlewares.SignalR.Implementations;
using Foundation.Test.Api.Middlewares.SignalR;
using IdentityModel.Client;

namespace Foundation.Test.Server
{
    public abstract class TestServerBase : ITestServer
    {
        public virtual string Uri { get; protected set; }

        /*public virtual ODataBatch BuildODataBatchClient(Action<HttpRequestMessage> beforeRequest = null,
                  Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string route = null)
        {
            if (route == null)
                route = "Test";

            ODataBatch client = new ODataBatch(new ODataClientSettings($"{Uri}odata/{route}/")
            {
                BeforeRequest = message =>
                {
                    if (token != null)
                    {
                        message.Headers.Add("Authorization",
                            $"{token.token_type} {token.access_token}");
                    }

                    if (beforeRequest != null)
                        beforeRequest(message);
                },
                AfterResponse = message =>
                {
                    if (afterResponse != null)
                        afterResponse(message);
                },
                OnCreateMessageHandler = () => GetHttpMessageHandler()
            });

            return client;
        }*/

        /*public virtual ODataClient BuildODataClient(Action<HttpRequestMessage> beforeRequest = null,
            Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string route = null)
        {
            if (route == null)
                route = "Test";

            ODataClient client = new ODataClient(new ODataClientSettings($"{Uri}odata/{route}/")
            {
                BeforeRequest = message =>
                {
                    if (token != null)
                    {
                        message.Headers.Add("Authorization",
                            $"{token.token_type} {token.access_token}");
                    }

                    if (beforeRequest != null)
                        beforeRequest(message);
                },
                AfterResponse = message =>
                {
                    if (afterResponse != null)
                        afterResponse(message);
                },
                OnCreateMessageHandler = () => GetHttpMessageHandler()
            });

            return client;
        }*/

        protected abstract HttpMessageHandler GetHttpMessageHandler();

        public virtual IHubProxy BuildSignalRClient(TokenResponse token = null, Action<string, dynamic> onMessageReceived = null)
        {
            HubConnection hubConnection = new HubConnection(Uri);

            if (token != null)
                hubConnection.Headers.Add("Authorization", $"{token.TokenType} {token.AccessToken}");

            IHubProxy hubProxy = hubConnection.CreateHubProxy(nameof(MessagesHub));

            if (onMessageReceived != null)
            {
                hubProxy.On("OnMessageReceived", (dataAsJson) =>
                {
                    onMessageReceived("OnMessageReceived", new SignalRMessageContentFormatter().DeSerialize<dynamic>(dataAsJson));
                });
            }

            hubConnection.Start(new LongPollingTransport(new SignalRHttpClient(GetHttpMessageHandler()))).Wait();

            return hubProxy;
        }

        public abstract void Dispose();

        public virtual RemoteWebDriver GetWebDriver(RemoteWebDriverOptions options = null)
        {
            if (options == null)
                options = new RemoteWebDriverOptions();

            //FirefoxDriver driver = new FirefoxDriver();

            ChromeOptions chromeOptions = new ChromeOptions
            {

            };

            ChromeDriver driver = new ChromeDriver(chromeOptions);

            //InternetExplorerDriver driver = new InternetExplorerDriver();

            //DesiredCapabilities capabilities = new DesiredCapabilities();

            //capabilities.SetCapability("deviceName", "donatello");
            //capabilities.SetCapability("platformVersion", "4.2.2");
            //capabilities.SetCapability("uid", "192.168.21.80:5555");
            //capabilities.SetCapability("fullReset", "True");
            //capabilities.SetCapability(MobileCapabilityType.App, "Browser");
            //capabilities.SetCapability("platformName", "Android");

            //AndroidDriver<AndroidElement> driver = new AndroidDriver<AndroidElement>(new Uri("http://127.0.0.1:4723/wd/hub"), capabilities);

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

        public virtual TokenResponse Login(string userName, string password, string clientName, string secret = "secret")
        {
            TokenClient client = BuildTokenClient(clientName, secret);

            return client.RequestResourceOwnerPasswordAsync(userName, password, "openid profile user_info").Result;
        }

        public HttpClient GetHttpClient(TokenResponse token = null)
        {
            HttpClient client = HttpClientFactory.Create(GetHttpMessageHandler());

            if (token != null)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"{token.TokenType} {token.AccessToken}");
            }

            client.BaseAddress = new Uri(Uri);

            return client;
        }

        public virtual TokenClient BuildTokenClient(string clientName, string secret = "secret")
        {
            TokenClient tokenClient = new TokenClient($@"{Uri}core/connect/token", clientName, secret, innerHttpMessageHandler: GetHttpMessageHandler());

            return tokenClient;
        }
    }
}
