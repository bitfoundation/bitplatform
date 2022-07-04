//-:cnd:noEmit
using System.IO.Compression;
using System.Net.Mail;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.ResponseCompression;
#if BlazorWebAssembly
using Microsoft.AspNetCore.Components;
using TodoTemplate.App.Services.Implementations;
#endif

namespace TodoTemplate.Api.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

        services.AddSharedServices();

#if BlazorWebAssembly
        services.AddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();
        services.AddAppServices();

        // In the Pre-Rendering mode, the configured HttpClient will use the access_token provided by the cookie in the request, so the pre-rendered content would be fitting for the current user.
        services.AddHttpClient("WebAssemblyPreRenderingHttpClient")
            .ConfigurePrimaryHttpMessageHandler<AppHttpClientHandler>()
            .ConfigureHttpClient((sp, httpClient) =>
            {
                NavigationManager navManager = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.RequestServices.GetRequiredService<NavigationManager>();
                httpClient.BaseAddress = new Uri($"{navManager.BaseUri}api/");
            });

        services.AddScoped(sp =>
        {
            IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return httpClientFactory.CreateClient("WebAssemblyPreRenderingHttpClient");
            // this is for pre rendering of blazor client/wasm
            // for other usages of http client, for example calling 3rd party apis, either use services.AddHttpClient("NamedHttpClient") or services.AddHttpClient<TypedHttpClient>();
        });
        services.AddRazorPages();
        services.AddMvcCore();
#endif

        services.AddCors();

        services
            .AddControllers()
            .AddOData(options => options.EnableQueryFeatures(maxTopValue: 20))
            .AddJsonOptions(options => options.JsonSerializerOptions.AddContext<AppJsonContext>());

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.ForwardedHostHeaderName = "X-Host";
        });
        
        services.AddResponseCaching();

        services.AddHttpContextAccessor();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" }).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.AddDbContext<AppDbContext>(options =>
        {
            options
            .UseSqlServer(configuration.GetConnectionString("SqlServerConnectionString"), sqlOpt =>
            {
                sqlOpt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.AddEndpointsApiExplorer();

        services.AddAutoMapper(typeof(Program).Assembly);

        services.AddSwaggerGen();

        services.AddIdentity(configuration);

        services.AddJwt(configuration);

        services.AddHealthChecks(env, configuration);

        var fluentEmailServiceBuilder = services.AddFluentEmail(appSettings.EmailSettings.DefaulFromEmail, appSettings.EmailSettings.DefaultFromName)
            .AddRazorRenderer();

        if (appSettings.EmailSettings.UseLocalFolderForEmails)
        {
            var sentEmailsFolderPath = Path.Combine(AppContext.BaseDirectory, "sent-emails");

            Directory.CreateDirectory(sentEmailsFolderPath);

            fluentEmailServiceBuilder.AddSmtpSender(() => new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = sentEmailsFolderPath
            });
        }
        else
        {
            if (appSettings.EmailSettings.HasCredential)
            {
                fluentEmailServiceBuilder.AddSmtpSender(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port, appSettings.EmailSettings.UserName, appSettings.EmailSettings.Password);
            }
            else
            {
                fluentEmailServiceBuilder.AddSmtpSender(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port);
            }
        }
    }
}
