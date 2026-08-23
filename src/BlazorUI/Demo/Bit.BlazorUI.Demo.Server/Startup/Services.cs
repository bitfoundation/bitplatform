using System.ClientModel.Primitives;
using System.IO.Compression;
using Bit.BlazorUI.Demo.Server.Services;
using Bit.BlazorUI.Demo.Server.Controllers;
using Microsoft.AspNetCore.Components.Web;
using Bit.BlazorUI.Demo.Client.Core.Components;
using Bit.BlazorUI.Demo.Client.Core.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.AI;
using Azure.Monitor.OpenTelemetry.AspNetCore;

namespace Bit.BlazorUI.Demo.Server.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Server project.

        AppSettings appSettings = new();

        configuration.GetSection(nameof(AppSettings)).Bind(appSettings);

        services.AddHttpClient<TelegramBotApiClient>();
        services.AddScoped<TelegramBotService>();

        // Upstream client for the same-origin CesiumJS passthrough (see CesiumController).
        // No HttpClient.Timeout: it is a deadline for the whole exchange, the streamed body
        // included, and that body is drained at the pace of the browser downloading it - a client
        // on a slow link would have the multi-megabyte Cesium.js truncated mid-response. The
        // controller puts its own deadline on the part that is actually the server's to bound,
        // reaching the upstream and getting its headers back.
        services.AddHttpClient(nameof(CesiumController), client => client.Timeout = Timeout.InfiniteTimeSpan);

        // Upstream client for the same-origin demo-video passthrough (see VideosController), with
        // no Timeout for the same reason - all the more so there, where the body is a media stream
        // the browser holds open for as long as the video is playing.
        services.AddHttpClient(nameof(VideosController), client => client.Timeout = Timeout.InfiniteTimeSpan);

        services.AddExceptionHandler<ServerExceptionHandler>();

        services.AddBlazor(configuration);

        services.AddClientSharedServices();

        services.AddCors();

        services
            .AddControllers()
            .AddOData(options => options.EnableQueryFeatures())
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    throw new ResourceValidationException(context.ModelState.Select(ms => (ms.Key, ms.Value!.Errors.Select(e => new LocalizedString(e.ErrorMessage, e.ErrorMessage)).ToArray())).ToArray());
                };
            });

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = env.IsDevelopment();
        });

        services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        services.AddScoped<HtmlRenderer>();
        services.AddCascadingValue("RenderForMcpClient", sp =>
        {
            var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            return httpContext?.Items?.ContainsKey("RenderForMcpClient") is true
                || httpContext?.Request?.Query?.ContainsKey("showallcodes") is true;
        });

        // The theme the visitor picked, from the cookie the client mirrors it into (App.razor reads
        // the same cookie to paint the first frame). Prerendered chrome that reflects the current
        // theme - the AppHeader's design-system dropdown - reads it from here, because the JS
        // theme runtime is not reachable while prerendering. Null in the interactive circuits and
        // on WebAssembly, where the components ask the runtime instead.
        services.AddCascadingValue("PersistedTheme", sp =>
        {
            var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            return httpContext?.Request?.Cookies[BitThemeCookie.PreferenceCookieName];
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.ForwardedHostHeaderName = "X-Host";
        });

        if (string.IsNullOrEmpty(appSettings?.OpenAI?.ChatApiKey) is false)
        {
            // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.OpenAI#microsoftextensionsaiopenai
            services.AddChatClient(sp => new OpenAI.Chat.ChatClient(model: appSettings.OpenAI.ChatModel, credential: new(appSettings.OpenAI.ChatApiKey), options: new()
            {
                Endpoint = appSettings.OpenAI.ChatEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIChatClient())
            .UseLogging()
            .UseFunctionInvocation()
            .UseDistributedCache();
        }

        services.AddDistributedMemoryCache();

        services.AddResponseCaching();

        services.AddHttpContextAccessor();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<AppSettings>>().Value);

        // Add Azure Application Insights using OpenTelemetry if connection string is configured in appsettings.json
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrWhiteSpace(appInsightsConnectionString) is false)
        {
            services.AddOpenTelemetry().UseAzureMonitor(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            }).WithLogging(configureBuilder: null, configureOptions: options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            });
        }

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddHealthChecks(env, configuration);
    }
}
