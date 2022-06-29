//-:cnd:noEmit
using System.IO.Compression;
using System.Net.Mail;
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

        services.AddTodoTemplateSharedServices();

#if BlazorWebAssembly
        services.AddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();
        services.AddTodoTemplateAppServices();

        services.AddHttpClient("WebAssemblyPreRenderingHttpClient")
            .ConfigurePrimaryHttpMessageHandler<TodoTemplateHttpClientHandler>()
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
            .AddJsonOptions(options => options.JsonSerializerOptions.AddContext<TodoTemplateJsonContext>());

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

        services.AddDbContext<TodoTemplateDbContext>(options =>
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

        services.AddTodoTemplateSwaggerGen();

        services.AddTodoTemplateIdentity(configuration);

        services.AddTodoTemplateJwt(configuration);

        services.AddHealthChecks(env, configuration);

        var fluentEmailServiceBuilder = services.AddFluentEmail(appSettings.EmailSettings.DefaulFromEmail, appSettings.EmailSettings.DefaultFromName)
            .AddRazorRenderer();

        if (appSettings.EmailSettings.Host is "LocalFolder")
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
