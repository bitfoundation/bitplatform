using System.ClientModel.Primitives;
using System.IO.Compression;
using Bit.BlazorUI.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Bit.BlazorUI.Demo.Client.Core.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.AI;

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

        services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        services.AddScoped<HtmlRenderer>();
        services.AddCascadingValue(nameof(AppComponentBase.RenderForMcpClient), sp =>
        {
            var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            return httpContext?.Items?.ContainsKey(nameof(AppComponentBase.RenderForMcpClient)) is true
                || httpContext?.Request?.Query?.ContainsKey("showallcodes") is true;
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

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddHealthChecks(env, configuration);
    }
}
