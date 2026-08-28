using System.ClientModel.Primitives;
using System.IO.Compression;
using Bit.BlazorUI.Demo.Server.Services;
using Bit.BlazorUI.Demo.Server.Services.Mcp;
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

        // The MCP server - Controllers/McpController.cs for the tools, McpPrompts and McpResources
        // beside it, and Services/Mcp for the catalogs they all answer from.
        services.AddMcpServer(options =>
        {
            options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
            {
                Name = "bit-blazorui",
                Title = "bit BlazorUI - the Blazor component library",
                Version = BlazorUIAssemblies.Version,
                WebsiteUrl = BlazorUIMarkdown.SiteUrl
            };

            // The one field a server gets to write directly into the model's context, once, before
            // it has called anything: which tool to reach for first, and the handful of facts that
            // turn markup that compiles into markup that looks right. Deliberately short - it is
            // paid for on every request of every session.
            options.ServerInstructions = BlazorUIMcpInstructions.Text;
        })
            .WithHttpTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly()
            // Argument autocompletion for the prompts and the resource templates. Their arguments
            // are all drawn from closed sets this server already holds - the hosting models, the
            // component names, the type names - and without this a person picking a prompt in their
            // editor is asked to type one with nothing to type it from.
            .WithCompleteHandler((context, _) => ValueTask.FromResult(BlazorUICompletions.Complete(context.Params)));

        // Renders a page outside of a request's component hierarchy, so its content can be handed
        // to an MCP client as text. Scoped: a renderer belongs to the request that asked for it.
        services.AddScoped<HtmlRenderer>();

        // The "print every example's source instead of running it" view of a demo page. The MCP
        // server used to render pages through this and read the HTML back; it now answers from the
        // demo sources and the page types directly, so ?showallcodes is the only thing that turns
        // it on - the flag itself is still what DemoPage and DemoExample read.
        services.AddCascadingValue("RenderForMcpClient", sp =>
            sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request?.Query?.ContainsKey("showallcodes") is true);

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
