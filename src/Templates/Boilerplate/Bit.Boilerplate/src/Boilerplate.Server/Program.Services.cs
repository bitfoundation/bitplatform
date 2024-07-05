//+:cnd:noEmit
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Boilerplate.Client.Web;
using Boilerplate.Server.Services;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using Boilerplate.Server.Models.Identity;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.DataProtection;
using FluentStorage;
using FluentStorage.Blobs;
using Twilio;

namespace Boilerplate.Server;

public static partial class Program
{
    private static void ConfigureServices(this WebApplicationBuilder builder)
    {
        // Services being registered here can get injected in server project only.

        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;

        services.AddExceptionHandler<ServerExceptionHandler>();

        services.AddOptions<ForwardedHeadersOptions>()
            .Bind(configuration.GetRequiredSection("ForwardedHeaders"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

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

        //#if (appInsights == true)
        services.AddApplicationInsightsTelemetry(configuration);
        //#endif

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

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

        services.AddDbContextPool<AppDbContext>(options =>
        {
            options.EnableSensitiveDataLogging(env.IsDevelopment())
                .EnableDetailedErrors(env.IsDevelopment());

            //#if (database == "Sqlite")
            options.UseSqlite(configuration.GetConnectionString("SqliteConnectionString"), dbOptions =>
            {

            });
            //#endif
            //#if (IsInsideProjectTemplate == true)
            return;
            //#endif
            //#if (database == "SqlServer")
            options.UseSqlServer(configuration.GetConnectionString("SqlServerConnectionString"), dbOptions =>
            {

            });
            //#elif (database == "PostgreSQL")
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQLConnectionString"), dbOptions =>
            {

            });
            //#else
            throw new NotImplementedException("Install and configure any database supported by ef core (https://learn.microsoft.com/en-us/ef/core/providers)");
            //#endif
        });

        services.AddOptions<AppSettings>()
            .Bind(configuration.GetRequiredSection(nameof(AppSettings)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<IdentityOptions>()
            .Bind(configuration.GetRequiredSection(nameof(AppSettings)).GetRequiredSection(nameof(AppSettings.Identity)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<AppSettings>>().Value);

        services.AddEndpointsApiExplorer();

        AddSwaggerGen(builder);

        AddIdentity(builder);

        services.TryAddTransient<IContentTypeProvider, FileExtensionContentTypeProvider>();

        var fluentEmailServiceBuilder = services.AddFluentEmail(appSettings.Email.DefaultFromEmail);

        if (appSettings.Email.UseLocalFolderForEmails)
        {
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
            var sentEmailsFolderPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data", "sent-emails");

            Directory.CreateDirectory(sentEmailsFolderPath);

            fluentEmailServiceBuilder.AddSmtpSender(() => new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = sentEmailsFolderPath
            });
        }
        else
        {
            if (appSettings.Email.HasCredential)
            {
                fluentEmailServiceBuilder.AddSmtpSender(() => new(appSettings.Email.Host, appSettings.Email.Port)
                {
                    Credentials = new NetworkCredential(appSettings.Email.UserName, appSettings.Email.Password),
                    EnableSsl = true
                });
            }
            else
            {
                fluentEmailServiceBuilder.AddSmtpSender(appSettings.Email.Host, appSettings.Email.Port);
            }
        }

        services.TryAddTransient<EmailService>();
        services.TryAddTransient<SmsService>();
        if (appSettings.Sms.Configured)
        {
            TwilioClient.Init(appSettings.Sms.TwilioAccountSid, appSettings.Sms.TwilioAutoToken);
        }

        //#if (filesStorage == "Local")
        services.TryAddSingleton(sp =>
        {
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
            var attachmentsDirPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data");
            Directory.CreateDirectory(attachmentsDirPath);
            var connectionString = $"disk://path={attachmentsDirPath}";
            return StorageFactory.Blobs.FromConnectionString(connectionString);
        });
        //#elif (filesStorage == "AzureBlobStorage")
        services.TryAddSingleton(sp =>
        {
            var azureBlobStorageSasUrl = configuration.GetConnectionString("AzureBlobStorageSasUrl");
            return (IBlobStorage)(azureBlobStorageSasUrl is "emulator"
                                 ? StorageFactory.Blobs.AzureBlobStorageWithLocalEmulator()
                                 : StorageFactory.Blobs.AzureBlobStorageWithSas(azureBlobStorageSasUrl));
        });
        //#else
        services.TryAddSingleton<IBlobStorage>(sp =>
        {
            // Note that FluentStorage.AWS can be used with any S3 compatible S3 implementation such as Digital Ocean's Spaces Object Storage.
            throw new NotImplementedException("Install and configure any storage supported by fluent storage (https://github.com/robinrodricks/FluentStorage/wiki/Blob-Storage)");
        });
        //#endif

        AddBlazor(builder);

        //#if (captcha == "reCaptcha")
        services.AddHttpClient<GoogleRecaptchaHttpClient>(c =>
        {
            c.BaseAddress = new Uri("https://www.google.com/recaptcha/");
        });
        //#endif
    }

    private static void AddBlazor(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.TryAddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.TryAddTransient(sp =>
        {
            Uri.TryCreate(configuration.GetServerAddress(), UriKind.RelativeOrAbsolute, out var serverAddress);

            if (serverAddress!.IsAbsoluteUri is false)
            {
                serverAddress = new Uri(sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.GetBaseUrl(), serverAddress);
            }

            return new HttpClient(sp.GetRequiredKeyedService<DelegatingHandler>("DefaultMessageHandler"))
            {
                BaseAddress = serverAddress
            };
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddMvc();

        services.AddClientWebProjectServices();
    }

    private static void AddIdentity(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;
        var identityOptions = appSettings.Identity;

        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionCertificate.pfx");
        var certificate = new X509Certificate2(certificatePath, configuration["DataProtectionCertificatePassword"], OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet);

        bool isTestCertificate = certificate.Thumbprint is "55140A8C935AB5202949071E5781E6946CD60606"; // The default test certificate is still in use
        if (isTestCertificate && env.IsDevelopment() is false)
        {
            throw new InvalidOperationException(@"The default test certificate is still in use. Please replace it with a new one by running the 'dotnet dev-certs https --export-path DataProtectionCertificate.pfx --password P@ssw0rdP@ssw0rd' command (or your preferred method for generating PFX files) in the server project's folder.");
        }

        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .ProtectKeysWithCertificate(certificate);

        services.AddTransient<IUserConfirmation<User>, AppUserConfirmation>();

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<AppIdentityErrorDescriber>()
            .AddApiEndpoints();

        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
            options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
            options.DefaultScheme = IdentityConstants.BearerScheme;
        })
        .AddBearerToken(IdentityConstants.BearerScheme, options =>
        {
            options.BearerTokenExpiration = identityOptions.BearerTokenExpiration;
            options.RefreshTokenExpiration = identityOptions.RefreshTokenExpiration;

            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new X509SecurityKey(certificate),

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateAudience = true,
                ValidAudience = identityOptions.Audience,

                ValidateIssuer = true,
                ValidIssuer = identityOptions.Issuer,

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

        if (string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]) is false)
        {
            authenticationBuilder.AddGoogle(options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
        }

        if (string.IsNullOrEmpty(configuration["Authentication:GitHub:ClientId"]) is false)
        {
            authenticationBuilder.AddGitHub(options =>
            {
                options.ClientId = configuration["Authentication:GitHub:ClientId"]!;
                options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"]!;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
        }

        if (string.IsNullOrEmpty(configuration["Authentication:Twitter:ConsumerKey"]) is false)
        {
            authenticationBuilder.AddTwitter(options =>
            {
                options.ConsumerKey = configuration["Authentication:Twitter:ConsumerKey"]!;
                options.ConsumerSecret = configuration["Authentication:Twitter:ConsumerSecret"]!;
                options.RetrieveUserDetails = true;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
        }

        services.AddAuthorization();
    }

    private static void AddSwaggerGen(WebApplicationBuilder builder)
    {
        var services = builder.Services;

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
}
