//+:cnd:noEmit
using Microsoft.Net.Http.Headers;
//#if (api == "Integrated")
using Boilerplate.Server.Api;
//#endif
using Boilerplate.Client.Web;
using Microsoft.AspNetCore.Antiforgery;
using Boilerplate.Server.Web.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace Boilerplate.Server.Web;

public static partial class Program
{
    public static void AddServerWebProjectServices(this WebApplicationBuilder builder)
    {
        // Services being registered here can get injected in server project only.
        var services = builder.Services;
        var configuration = builder.Configuration;

        if (AppEnvironment.IsDevelopment())
        {
            builder.Logging.AddDiagnosticLogger();
        }

        services.AddClientWebProjectServices(configuration);

        //#if (api == "Integrated")
        builder.AddServerApiProjectServices();
        //#else
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        builder.AddServerSharedServices();
        builder.AddDefaultHealthChecks();
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = Microsoft.AspNetCore.Identity.IdentityConstants.BearerScheme;
        }).AddBearerToken(Microsoft.AspNetCore.Identity.IdentityConstants.BearerScheme, options =>
        {
            options.BearerTokenProtector = new SimpleJwtSecureDataFormat();
            options.RefreshTokenProtector = new SimpleJwtSecureDataFormat();

            options.Events = new()
            {
                OnMessageReceived = async context =>
                {
                    // The server accepts the accessToken from either the authorization header, the cookie, or the request URL query string
                    context.Token ??= context.Request.Query.ContainsKey("access_token") ? context.Request.Query["access_token"] : context.Request.Cookies["access_token"];
                }
            };
        });
        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#endif

        services.AddSingleton(sp =>
        {
            ServerWebSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });

        services.AddOptions<ServerWebSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddBlazor(builder);
    }

    private static void AddBlazor(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddTransient<IAntiforgery, NoOpAntiforgery>();
        services.AddTransient<IPrerenderStateService, WebServerPrerenderStateService>();
        services.AddScoped<IExceptionHandler, WebServerExceptionHandler>();
        services.AddScoped<IAuthTokenProvider, ServerSideAuthTokenProvider>();
        services.AddScoped<HttpClient>(sp =>
        {
            // This HTTP client is utilized during pre-rendering and within Blazor Auto/Server sessions for API calls. 
            // Key headers such as Authorization and AcceptLanguage headers are added in Client/Core/Services/HttpMessageHandlers. 
            // Additionally, forwarded headers are handled to ensure proper forwarding, if the backend is hosted behind a CDN. 
            // User agent and referrer headers are also included to provide the API with necessary request context. 

            var serverSettings = sp.GetRequiredService<ServerWebSettings>();
            var serverAddressString = string.IsNullOrEmpty(serverSettings.ServerSideHttpClientBaseAddress) is false ?
                serverSettings.ServerSideHttpClientBaseAddress : configuration.GetServerAddress();

            Uri.TryCreate(serverAddressString, UriKind.RelativeOrAbsolute, out var serverAddress);
            var currentRequest = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request;
            if (serverAddress!.IsAbsoluteUri is false)
            {
                serverAddress = new Uri(currentRequest.GetBaseUrl(), serverAddress);
            }

            var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
            var httpClient = new HttpClient(handlerFactory.Invoke())
            {
                BaseAddress = serverAddress
            };

            var forwardedHeadersOptions = sp.GetRequiredService<ServerWebSettings>().ForwardedHeaders;

            foreach (var xHeader in currentRequest.Headers.Where(h => h.Key.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase)))
            {
                httpClient.DefaultRequestHeaders.Add(xHeader.Key, string.Join(',', xHeader.Value.AsEnumerable()));
            }

            if (forwardedHeadersOptions is not null && httpClient.DefaultRequestHeaders.Contains(forwardedHeadersOptions.ForwardedForHeaderName) is false &&
                currentRequest.HttpContext.Connection.RemoteIpAddress is not null)
            {
                httpClient.DefaultRequestHeaders.Add(forwardedHeadersOptions.ForwardedForHeaderName,
                                                     currentRequest.HttpContext.Connection.RemoteIpAddress.ToString());
            }

            if (currentRequest.Headers.TryGetValue(HeaderNames.UserAgent, out var headerValues))
            {
                foreach (var ua in currentRequest.Headers.UserAgent)
                {
                    httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(ua);
                }
            }

            if (currentRequest.Headers.TryGetValue(HeaderNames.Referer, out headerValues))
            {
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Referer, string.Join(',', headerValues.AsEnumerable()));
            }

            httpClient.DefaultRequestHeaders.Add("X-Origin", currentRequest.GetBaseUrl().ToString());

            return httpClient;
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
    }
}
