//+:cnd:noEmit
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Options;
//#if (api == "Integrated")
using Boilerplate.Server.Api;
//#else
using Microsoft.AspNetCore.Authentication.JwtBearer;
//#endif
using Boilerplate.Client.Web;
using Boilerplate.Server.Shared;
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
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = configuration.GetServerAddress();
            options.RequireHttpsMetadata = builder.Environment.IsDevelopment() is false;
            options.TokenValidationParameters = new()
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,

                RequireExpirationTime = true,

                ValidateAudience = true,
                ValidAudience = configuration["Identity:Audience"],

                ValidateIssuer = true,
                ValidIssuer = configuration["Identity:Issuer"]
            };

            options.Events = new()
            {
                OnMessageReceived = async context =>
                {
                    // The server accepts the accessToken from either the authorization header or the cookie.
                    context.Token ??= context.HttpContext.GetAccessToken();
                },
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal!;
                    var identity = (ClaimsIdentity)principal.Identity!;

                    if (principal.IsInRole(AppRoles.GlobalAdmin))
                    {
                        foreach (var feat in AppFeatures.GetGlobalAdminFeatures())
                        {
                            identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feat.Value));
                        }
                    }
                    //#if (multitenant == true)
                    else if (principal.IsInRole(AppRoles.TenantAdmin))
                    {
                        foreach (var feat in AppFeatures.GetTenantAdminFeatures())
                        {
                            identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feat.Value));
                        }
                    }
                    //#endif
                }
            };
        });
        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#endif

        services.AddOptions<ServerWebSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ServerWebSettings>>().Value);

        AddBlazor(builder);
    }

    private static void AddBlazor(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddScoped<IPrerenderStateService, WebServerPrerenderStateService>();
        services.AddScoped<ClientExceptionHandlerBase, WebServerExceptionHandler>();
        //#if (api == "Standalone")
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());
        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#endif

        services.AddScoped<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.AddSingleton(_ => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            SslOptions = new()
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            }
        });

        services.AddScoped<HttpClient>(sp =>
        {
            // This HTTP client is utilized during pre-rendering and within Blazor Auto/Server sessions for API calls. 
            // Key headers such as Authorization and AcceptLanguage headers are added in Client/Core/Services/HttpMessageHandlers. 
            // Additionally, forwarded headers are handled to ensure proper forwarding, if the backend is hosted behind a CDN. 
            // User agent and referrer headers are also included to provide the API with necessary request context. 

            var serverSettings = sp.GetRequiredService<ServerWebSettings>();
            var serverAddressString = string.IsNullOrEmpty(serverSettings.ServerSideHttpClientBaseAddress) is false ?
                serverSettings.ServerSideHttpClientBaseAddress : configuration.GetServerAddress();

            if (Uri.TryCreate(serverAddressString, UriKind.RelativeOrAbsolute, out var serverAddress) is false)
                throw new InvalidOperationException($"'{serverAddressString}' is not a valid address. Set ServerAddress (or ServerSideHttpClientBaseAddress) in appsettings.json.");

            var currentRequest = (sp.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException()).Request;

            if (serverAddress.IsAbsoluteUri is false)
            {
                serverAddress = new Uri(currentRequest.GetBaseUrl(), serverAddress);
            }

            var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
            var httpClient = new HttpClient(handlerFactory.Invoke(new NonDisposingHandler(sp.GetRequiredService<SocketsHttpHandler>())))
            {
                BaseAddress = serverAddress
            };

            var forwardedHeadersSection = configuration.GetSection("ForwardedHeaders");
            var forwardedHeadersOptions = forwardedHeadersSection.Exists() ? forwardedHeadersSection.DynamicBind<ForwardedHeadersOptions>() : null;

            // Headers that describe WHO the caller is are decided here, never forwarded from what the caller sent:
            // the api resolves the client ip (and therefore the identity rate limiter's partition and UserSession.IP)
            // from X-Forwarded-For, the request scheme from X-Forwarded-Proto, and the web app's origin from X-Origin.
            // The names are literals on purpose - deriving them from forwardedHeadersOptions would skip the exclusion
            // entirely whenever the ForwardedHeaders section is absent and that object is null.
            HashSet<string> clientControlledForwardingHeaders = new(StringComparer.OrdinalIgnoreCase)
            {
                "X-Forwarded-For", "X-Forwarded-Proto", "X-Forwarded-Host", "X-Forwarded-Prefix", "X-Origin"
            };
            if (forwardedHeadersOptions is not null)
            {
                clientControlledForwardingHeaders.Add(forwardedHeadersOptions.ForwardedForHeaderName);
                clientControlledForwardingHeaders.Add(forwardedHeadersOptions.ForwardedProtoHeaderName);
                clientControlledForwardingHeaders.Add(forwardedHeadersOptions.ForwardedHostHeaderName);
                clientControlledForwardingHeaders.Add(forwardedHeadersOptions.ForwardedPrefixHeaderName);
            }

            foreach (var xHeader in currentRequest.Headers.Where(h => h.Key.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase) &&
                                                                      clientControlledForwardingHeaders.Contains(h.Key) is false))
            {
                httpClient.DefaultRequestHeaders.Add(xHeader.Key, string.Join(',', xHeader.Value.AsEnumerable()));
            }

            if (currentRequest.HttpContext.Connection.RemoteIpAddress is not null)
            {
                httpClient.DefaultRequestHeaders.Add(forwardedHeadersOptions?.ForwardedForHeaderName ?? "X-Forwarded-For",
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

    /// <summary>
    /// The <see cref="SocketsHttpHandler"/> above is shared by every pre-rendering request and every Blazor Server
    /// session, so its connections are re-used instead of a new pool being opened each time. But each of those gets its
    /// own <see cref="HttpClient"/>, and an HttpClient disposes its whole handler chain - which would close the shared
    /// handler for everyone else. This wrapper sits in between and simply doesn't pass the dispose along.
    /// </summary>
    private sealed class NonDisposingHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        protected override void Dispose(bool disposing)
        {
            // Nothing to do on purpose: the shared handler is owned by the DI container, not by this client.
        }
    }
}
