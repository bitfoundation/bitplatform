//+:cnd:noEmit
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorWeb.Server.Services;

namespace BlazorWeb.Server.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Api project.

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        services.AddClientSharedServices();

        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddBlazor(configuration);

        services
            .AddControllers()
            .AddOData(options => options.EnableQueryFeatures())
            .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = StringLocalizerProvider.ProvideLocalizer)
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    throw new ResourceValidationException(context.ModelState.Select(ms => (ms.Key, ms.Value!.Errors.Select(e => new LocalizedString(e.ErrorMessage, e.ErrorMessage)).ToArray())).ToArray());
                };
            });

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
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.AddDbContext<AppDbContext>(options =>
        {
            //#if (database == "SqlServer")
            options.UseSqlServer(configuration.GetConnectionString("SqlServerConnectionString"), dbOptions =>
            {
                dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            //#endif
            //#if (IsInsideProjectTemplate == true)
            return;
            //#endif
            //#if (database == "Sqlite")
            options.UseSqlite(configuration.GetConnectionString("SqliteConnectionString"), dbOptions =>
            {
                dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            //#endif
        });

        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.AddScoped(sp => sp.GetRequiredService<IOptionsSnapshot<AppSettings>>().Value);

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddIdentity(configuration);

        services.AddHealthChecks(env, configuration);

        services.AddScoped<HtmlRenderer>();

        var fluentEmailServiceBuilder = services.AddFluentEmail(appSettings.EmailSettings.DefaultFromEmail, appSettings.EmailSettings.DefaultFromName);

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
                fluentEmailServiceBuilder.AddSmtpSender(() => new(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port)
                {
                    Credentials = new NetworkCredential(appSettings.EmailSettings.UserName, appSettings.EmailSettings.Password)
                });
            }
            else
            {
                fluentEmailServiceBuilder.AddSmtpSender(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port);
            }
        }
    }
}
