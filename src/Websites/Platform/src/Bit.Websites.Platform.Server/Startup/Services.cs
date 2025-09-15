using System.IO.Compression;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Bit.Websites.Platform.Server.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.AI;

namespace Bit.Websites.Platform.Server.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Server project.

        AppSettings appSettings = new();

        configuration.GetSection(nameof(AppSettings)).Bind(appSettings);

        services.AddTransient<IAntiforgery, NullAntiforgery>();
        services.AddHttpClient<TelegramBotApiClient>();
        services.AddScoped<TelegramBotService>();

        services.AddClientSharedServices();

        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddBlazor(configuration);

        services
            .AddControllers();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = env.IsDevelopment();
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.ForwardedHostHeaderName = "X-Host";
        });

        if (string.IsNullOrEmpty(appSettings?.AzureOpenAI?.ChatApiKey) is false)
        {
            // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.AzureAIInference#microsoftextensionsaiazureaiinference
            services.AddChatClient(sp => new Azure.AI.Inference.ChatCompletionsClient(endpoint: appSettings.AzureOpenAI.ChatEndpoint,
                credential: new Azure.AzureKeyCredential(appSettings.AzureOpenAI.ChatApiKey),
                options: new()
                {
                    Transport = new Azure.Core.Pipeline.HttpClientTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
                }).AsIChatClient(appSettings.AzureOpenAI.ChatModel))
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
