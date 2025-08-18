using Bit.Core.Exceptions;
using Bit.Core.Implementations;
using Bit.Http.Contracts;
using Bit.Http.Implementations;
using Bit.OData.Implementations;
using Bit.Signalr;
using Bit.Signalr.Implementations;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json;
using Refit;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Test.Server
{
    public abstract class TestServerBase : ITestServer
    {
        public virtual string Uri { get; protected set; } = default!;

        public virtual ODataBatch BuildODataBatchClient(Token? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null)
        {
            return new ODataBatch(BuildODataClient(token, odataRouteName, odataClientSettings));
        }

        public virtual IODataClient BuildODataClient(Token? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null)
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

        public virtual async Task<IHubProxy> BuildSignalRClient(Token? token = null, Action<string, dynamic>? onMessageReceived = null)
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

            await hubConnection.Start(new ServerSentEventsTransport(new SignalRHttpClient(GetHttpMessageHandler()))).ConfigureAwait(false);

            return hubProxy;
        }

        public class TestSignalRMessageContentFormatter : SignalRMessageContentFormatter
        {
            public override T Deserialize<T>(string objAsStr)
            {
                return JsonConvert.DeserializeObject<T>(objAsStr, GetSettings())!;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TestServerBase()
        {
            Dispose(false);
        }

        protected abstract void Dispose(bool disposing);

        public virtual void Initialize(string uri)
        {
            Uri = uri;
        }

        public virtual async Task<Token> LoginWithCredentials(string userName, string password, IDictionary<string, string?>? acr_values = null)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            HttpClient client = BuildHttpClient();

            Dictionary<string, string> loginData = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", userName },
                { "password", password }
            };

            if (acr_values != null)
            {
                loginData.Add("acr_values", string.Join(" ", acr_values.Select(p => $"{p.Key}:{System.Uri.EscapeDataString(p.Value ?? string.Empty)}")));
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "core/connect/token");

            request.Content = new FormUrlEncodedContent(loginData);

            using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

            await using Stream responseContent = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            Token token = await DefaultJsonContentFormatter.Current.DeserializeAsync<Token>(responseContent, default).ConfigureAwait(false);

            if (token.IsError)
            {
                if (token.Error == "invalid_grant" && !string.IsNullOrEmpty(token.ErrorDescription))
                {
                    throw new LoginFailureException(token.ErrorDescription, new Exception(token.Error));
                }
                else
                {
                    throw new Exception(token.Error);
                }
            }

            token.LoginDate = DateTimeOffset.UtcNow;

            return token;
        }

        public virtual string GetLoginUrl(string? client_id = null, Uri? redirect_uri = null, object? state = null, IDictionary<string, string?>? acr_values = null)
        {
            state ??= new { };

            string relativeUri = $"InvokeLogin?state={DefaultJsonContentFormatter.Current.Serialize(state)}";

            if (redirect_uri != null)
                relativeUri += $"&redirect_uri={redirect_uri}";
            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";
            if (acr_values != null)
                relativeUri += $"&acr_values={string.Join(" ", acr_values.Select(p => $"{p.Key}:{System.Uri.EscapeDataString(p.Value ?? string.Empty)}"))}";

            return relativeUri;
        }

        public virtual HttpClient BuildHttpClient(Token? token = null)
        {
            HttpClient client = HttpClientFactory.Create(GetHttpMessageHandler());

            if (token != null)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"{token.TokenType} {token.AccessToken}");
            }

            client.BaseAddress = new Uri(Uri);

            return client;
        }

        public virtual TService BuildRefitClient<TService>(Token? token = null)
        {
            return RestService.For<TService>(BuildHttpClient(token), new RefitSettings
            {
                ContentSerializer = new BitRefitJsonContentSerializer()
            });
        }
    }
}
