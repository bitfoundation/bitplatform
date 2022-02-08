#if BlazorWebAssembly
using TodoTemplate.App.Extensions;
using TodoTemplate.App.Implementations;
#endif

using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using TodoTemplate.Api.Extensions;

namespace TodoTemplate.Api.Startup;

public static class Services
{
    public static void Add(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

#if BlazorWebAssembly
        services.AddTodoTemplateServices();
        services.AddScoped(c =>
        {
            // this is for pre rendering of blazor client/wasm
            // Using this registration + registrations provided in Program.cs/Startup.cs of TodoTemplate.App project,
            // you can inject HttpClient and call TodoTemplate.Api api controllers in blazor pages.
            // for other usages of http client, for example calling 3rd party apis, please use services.AddHttpClient("NamedHttpClient"), then inject IHttpClientFactory and use its CreateClient("NamedHttpClient") method.
            return new HttpClient(new TodoTemplateHttpClientHandler()) { BaseAddress = new Uri($"{c.GetRequiredService<NavigationManager>().BaseUri}api/") };
        });
        services.AddRazorPages();
#endif

        services.AddCors();

        services.AddMvcCore();

        services.AddControllers();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Where(m => m != "text/html").Concat(new[] { "application/octet-stream" }).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.AddDbContext<TodoTemplateDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("SqlServerConnection"));
        });

        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.AddEndpointsApiExplorer();

        services.AddAutoMapper(typeof(Program).Assembly);

        services.AddCustomSwaggerGen();

        services.AddCustomIdentity(configuration);

        services.AddCustomJwt(configuration);
    }
}
