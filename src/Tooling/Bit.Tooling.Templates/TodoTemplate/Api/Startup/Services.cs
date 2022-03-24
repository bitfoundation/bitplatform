using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.OData;
using TodoTemplate.Api.Filters;

#if BlazorWebAssembly
using TodoTemplate.App.Services.Implementations;
using Microsoft.AspNetCore.Components;
#endif

namespace TodoTemplate.Api.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
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
            .AddControllers(options => options.Filters.Add<HttpResponseExceptionFilter>())
            .AddOData(options => options.EnableQueryFeatures(maxTopValue: 20))
            .AddJsonOptions(options => options.JsonSerializerOptions.AddContext<ToDoTemplateJsonContext>());

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
            .UseSqlServer(configuration.GetConnectionString("SqlServerConnection"), sqlOpt =>
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

        services.AddFluentEmail(appSettings.EmailSettings.DefaulFromEmail, appSettings.EmailSettings.DefaultFromName)
            .AddRazorRenderer()
            .AddSmtpSender(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port, appSettings.EmailSettings.Username, appSettings.EmailSettings.Password);

        // install Smtp4dev (fake smtp server) using following command:
        // dotnet tool install -g Rnwood.Smtp4dev
        // and run it using following command:
        // smtp4dev --urls=https://localhost:6001/
        // you'll be able to see sent emails by opening https://localhost:6001/
        // For production, either use production ready smtp server or use fluent email integration packages such as sendgrid, mailgun etc.
    }
}
