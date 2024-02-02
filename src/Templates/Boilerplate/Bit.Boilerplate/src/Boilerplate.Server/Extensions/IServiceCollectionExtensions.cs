//-:cnd:noEmit
using System.IO.Compression;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Boilerplate.Server;
using Boilerplate.Server.Models.Identity;
using Boilerplate.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddServerServices(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Server project.

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        services.AddExceptionHandler<ServerExceptionHandler>();

        services.AddBlazor(configuration);

        //+:cnd:noEmit

        services.AddCors();

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

        services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<AppSettings>>().Value);

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddIdentity(configuration, env);

        services.AddHealthChecks(env, configuration);

        services.AddTransient<HtmlRenderer>();

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
                    Credentials = new NetworkCredential(appSettings.EmailSettings.UserName, appSettings.EmailSettings.Password),
                    EnableSsl = true
                });
            }
            else
            {
                fluentEmailServiceBuilder.AddSmtpSender(appSettings.EmailSettings.Host, appSettings.EmailSettings.Port);
            }
        }
    }

    public static void AddBlazor(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.AddTransient(sp =>
        {
            Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

            if (apiServerAddress!.IsAbsoluteUri is false)
            {
                apiServerAddress = new Uri(sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.GetBaseUrl(), apiServerAddress);
            }

            return new HttpClient(sp.GetRequiredKeyedService<HttpMessageHandler>("DefaultMessageHandler"))
            {
                BaseAddress = apiServerAddress
            };
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddMvc();

        services.AddClientWebServices();
    }

    public static void AddIdentity(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment hostEnv)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;
        var settings = appSettings.IdentitySettings;

        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");
        var certificate = new X509Certificate2(certificatePath, appSettings.IdentitySettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet);

        bool isTestCertificate = certificate.Thumbprint is "55140A8C935AB5202949071E5781E6946CD60606"; // The default test certificate is still in use
        if (isTestCertificate && hostEnv.IsDevelopment() is false)
        {
            throw new InvalidOperationException(@"The default test certificate is still in use. Please replace it with a new one by running the 'dotnet dev-certs https --export-path IdentityCertificate.pfx --password P@ssw0rdP@ssw0rd' command (or your preferred method for generating PFX files) in the server project's folder.");
        }

        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .ProtectKeysWithCertificate(certificate);

        services.AddIdentity<User, Role>(options =>
        {
            options.User.RequireUniqueEmail = settings.RequireUniqueEmail;
            options.SignIn.RequireConfirmedEmail = true;
            options.Password.RequireDigit = settings.PasswordRequireDigit;
            options.Password.RequireLowercase = settings.PasswordRequireLowercase;
            options.Password.RequireUppercase = settings.PasswordRequireUppercase;
            options.Password.RequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric;
            options.Password.RequiredLength = settings.PasswordRequiredLength;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<AppIdentityErrorDescriber>()
            .AddApiEndpoints();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
            options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
            options.DefaultScheme = IdentityConstants.BearerScheme;
        })
        .AddBearerToken(IdentityConstants.BearerScheme, options =>
        {
            options.BearerTokenExpiration = settings.BearerTokenExpiration;
            options.RefreshTokenExpiration = settings.RefreshTokenExpiration;

            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new X509SecurityKey(certificate),

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateAudience = true,
                ValidAudience = settings.Audience,

                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,

                AuthenticationType = IdentityConstants.BearerScheme
            };

            options.BearerTokenProtector = new AppSecureJwtDataFormat(appSettings, validationParameters);

            options.Events = new()
            {
                OnMessageReceived = async context =>
                {
                    // The server accepts the access_token from either the authorization header, the cookie, or the request URL query string
                    context.Token ??= context.Request.Cookies["access_token"] ?? context.Request.Query["access_token"];
                }
            };
        });

        services.AddAuthorization();
    }

    public static void AddSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Boilerplate.Server.xml"));
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Boilerplate.Shared.xml"));

            options.OperationFilter<ODataOperationFilter>();

            options.AddSecurityDefinition("bearerAuth", new()
            {
                Name = "Authorization",
                Description = "Enter the Bearer Authorization string as following: `Bearer Generated-Bearer-Token`",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                        Reference = new OpenApiReference
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    []
                }
            });
        });
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks is false)
            return services;

        services.AddHealthChecksUI(setupSettings: setup =>
        {
            setup.AddHealthCheckEndpoint("BPHealthChecks", env.IsDevelopment() ? "http://localhost:5030/healthz" : "/healthz");
        }).AddInMemoryStorage();

        var healthChecksBuilder = services.AddHealthChecks()
            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 6 * 1024)
            .AddDiskStorageHealthCheck(opt =>
                opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 5 * 1024))
            .AddDbContextCheck<AppDbContext>();

        var emailSettings = appSettings.EmailSettings;

        if (emailSettings.UseLocalFolderForEmails is false)
        {
            healthChecksBuilder
                .AddSmtpHealthCheck(options =>
                {
                    options.Host = emailSettings.Host;
                    options.Port = emailSettings.Port;

                    if (emailSettings.HasCredential)
                    {
                        options.LoginWith(emailSettings.UserName, emailSettings.Password);
                    }
                });
        }

        return services;
    }
}
