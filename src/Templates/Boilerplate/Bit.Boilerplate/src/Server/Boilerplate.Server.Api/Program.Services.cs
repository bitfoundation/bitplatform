//+:cnd:noEmit
using System.Net;
using System.Net.Mail;
using System.IO.Compression;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OData;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using System.Security.Cryptography.X509Certificates;
using Twilio;
using PhoneNumbers;
using FluentStorage;
using FluentStorage.Blobs;
using FluentEmail.Core;
//#if (notification == true)
using AdsPush;
using AdsPush.Abstraction;
//#endif
using Boilerplate.Server.Api.Services;
using Boilerplate.Server.Api.Controllers;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Services.Identity;

namespace Boilerplate.Server.Api;

public static partial class Program
{
    public static void AddServerApiProjectServices(this WebApplicationBuilder builder)
    {
        // Services being registered here can get injected in server project only.
        var env = builder.Environment;
        var services = builder.Services;
        var configuration = builder.Configuration;

        ServerApiSettings appSettings = new();
        configuration.Bind(appSettings);

        services.AddScoped<EmailService>();
        services.AddScoped<PhoneService>();
        if (appSettings.Sms?.Configured is true)
        {
            TwilioClient.Init(appSettings.Sms.TwilioAccountSid, appSettings.Sms.TwilioAutoToken);
        }

        services.AddSingleton(_ => PhoneNumberUtil.GetInstance());
        services.AddSingleton<IBlobStorage>(sp =>
        {
            //#if (filesStorage == "Local")
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
            var attachmentsDirPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data");
            Directory.CreateDirectory(attachmentsDirPath);
            return StorageFactory.Blobs.DirectoryFiles(attachmentsDirPath);
            //#elif (filesStorage == "AzureBlobStorage")
            var azureBlobStorageSasUrl = configuration.GetConnectionString("AzureBlobStorageSasUrl");
            return (IBlobStorage)(azureBlobStorageSasUrl is "emulator"
                                 ? StorageFactory.Blobs.AzureBlobStorageWithLocalEmulator()
                                 : StorageFactory.Blobs.AzureBlobStorageWithSas(azureBlobStorageSasUrl));
            //#else
            // Note that FluentStorage.AWS can be used with any S3 compatible S3 implementation such as Digital Ocean's Spaces Object Storage.
            throw new NotImplementedException("Install and configure any storage supported by fluent storage (https://github.com/robinrodricks/FluentStorage/wiki/Blob-Storage)");
            //#endif
        });
        //#if (notification == true)
        services.AddSingleton(_ =>
        {
            var adsPushSenderBuilder = new AdsPushSenderBuilder();

            if (string.IsNullOrEmpty(appSettings.AdsPushAPNS?.P8PrivateKey) is false)
            {
                adsPushSenderBuilder = adsPushSenderBuilder.ConfigureApns(appSettings.AdsPushAPNS, null);
            }

            if (string.IsNullOrEmpty(appSettings.AdsPushFirebase?.PrivateKey) is false)
            {
                appSettings.AdsPushFirebase.PrivateKey = appSettings.AdsPushFirebase.PrivateKey.Replace(@"\n", string.Empty);

                adsPushSenderBuilder = adsPushSenderBuilder.ConfigureFirebase(appSettings.AdsPushFirebase, AdsPushTarget.Android);
            }

            if (string.IsNullOrEmpty(appSettings.AdsPushVapid?.PrivateKey) is false)
            {
                adsPushSenderBuilder = adsPushSenderBuilder.ConfigureVapid(appSettings.AdsPushVapid, null);
            }

            return adsPushSenderBuilder
                .BuildSender();
        });
        services.AddScoped<PushNotificationService>();
        //#endif

        services.AddSingleton<ServerExceptionHandler>();
        services.AddSingleton(sp => (IProblemDetailsWriter)sp.GetRequiredService<ServerExceptionHandler>());
        services.AddProblemDetails();

        services.AddOutputCache(options =>
        {
            options.AddPolicy("AppResponseCachePolicy", policy =>
            {
                var builder = policy.AddPolicy<AppResponseCachePolicy>();
            }, excludeDefaultPolicy: true);
        });
        services.AddDistributedMemoryCache();

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
        services.AddApplicationInsightsTelemetry(options => configuration.GetRequiredSection("ApplicationInsights").Bind(options));
        //#endif

        services.AddCors(builder =>
        {
            builder.AddDefaultPolicy(policy =>
            {
                if (env.IsDevelopment() is false)
                {
                    policy.SetPreflightMaxAge(TimeSpan.FromDays(1)); // https://stackoverflow.com/a/74184331
                }

                ServerApiSettings settings = new();
                configuration.Bind(settings);

                policy.SetIsOriginAllowed(origin => settings.IsAllowedOrigin(new Uri(origin)))
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders(HeaderNames.RequestId, "Age", "App-Cache-Response");
            });
        });

        services.AddAntiforgery();

        services.AddSingleton(sp =>
        {
            JsonSerializerOptions options = new JsonSerializerOptions(AppJsonContext.Default.Options);

            options.TypeInfoResolverChain.Add(IdentityJsonContext.Default);
            options.TypeInfoResolverChain.Add(ServerJsonContext.Default);

            return options;
        });

        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.AddRange([AppJsonContext.Default, IdentityJsonContext.Default, ServerJsonContext.Default]));

        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.TypeInfoResolverChain.AddRange([AppJsonContext.Default, IdentityJsonContext.Default, ServerJsonContext.Default]);
            })
            //#if (api == "Integrated")
            .AddApplicationPart(typeof(AppControllerBase).Assembly)
            //#endif
            .AddOData(options => options.EnableQueryFeatures())
            .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = StringLocalizerProvider.ProvideLocalizer)
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    throw new ResourceValidationException(context.ModelState.Select(ms => (ms.Key, ms.Value!.Errors.Select(e => new LocalizedString(e.ErrorMessage, e.ErrorMessage)).ToArray())).ToArray());
                };
            });

        //#if (signalR == true)
        var signalRBuilder = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = env.IsDevelopment();
        });
        if (string.IsNullOrEmpty(configuration["Azure:SignalR:ConnectionString"]) is false)
        {
            signalRBuilder.AddAzureSignalR(options =>
            {
                configuration.GetRequiredSection("Azure:SignalR").Bind(options);
            });
        }
        //#endif

        services.AddPooledDbContextFactory<AppDbContext>(AddDbContext);
        services.AddDbContextPool<AppDbContext>(AddDbContext);

        void AddDbContext(DbContextOptionsBuilder options)
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
            //#elif (database == "MySql")
            options.UseMySql(configuration.GetConnectionString("MySqlSQLConnectionString"), ServerVersion.AutoDetect(configuration.GetConnectionString("MySqlSQLConnectionString")), dbOptions =>
            {

            });
            //#elif (database == "Other")
            throw new NotImplementedException("Install and configure any database supported by ef core (https://learn.microsoft.com/en-us/ef/core/providers)");
            //#endif
        };

        services.AddOptions<IdentityOptions>()
            .Bind(configuration.GetRequiredSection(nameof(ServerApiSettings.Identity)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ServerApiSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            ServerApiSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });

        services.AddEndpointsApiExplorer();

        AddSwaggerGen(builder);

        AddIdentity(builder);

        var emailSettings = appSettings.Email ?? throw new InvalidOperationException("Email settings are required.");
        var fluentEmailServiceBuilder = services.AddFluentEmail(emailSettings.DefaultFromEmail);

        if (emailSettings.UseLocalFolderForEmails)
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
            if (emailSettings.HasCredential)
            {
                fluentEmailServiceBuilder.AddSmtpSender(() => new(emailSettings.Host, emailSettings.Port)
                {
                    Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                    EnableSsl = true
                });
            }
            else
            {
                fluentEmailServiceBuilder.AddSmtpSender(emailSettings.Host, emailSettings.Port);
            }
        }

        //#if (captcha == "reCaptcha")
        services.AddHttpClient<GoogleRecaptchaService>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
            c.BaseAddress = new Uri("https://www.google.com/recaptcha/");
        });
        //#endif

        services.AddHttpClient<NugetStatisticsService>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(3);
            c.BaseAddress = new Uri("https://azuresearch-usnc.nuget.org");
        });

        services.AddHttpClient<ResponseCacheService>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
            c.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/zones/");
        });

        services.AddFido2(options =>
        {
            options.ServerIcon = "data:image/png;base64,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";
            options.ServerDomain = "localhost";
            options.ServerName = "Boilerplate WebAuthn";
            options.Origins = new HashSet<string> { "http://localhost:5030" };
            options.TimestampDriftTolerance = 1000;
        });
    }

    private static void AddIdentity(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;
        ServerApiSettings appSettings = new();
        configuration.Bind(appSettings);
        var identityOptions = appSettings.Identity;

        var certificatePath = Path.Combine(AppContext.BaseDirectory, "DataProtectionCertificate.pfx");
        var certificate = new X509Certificate2(certificatePath, appSettings.DataProtectionCertificatePassword, AppPlatform.IsWindows ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet);

        if (env.IsDevelopment() is false && (DateTimeOffset.UtcNow < certificate.NotBefore || DateTimeOffset.UtcNow > certificate.NotAfter))
            throw new InvalidOperationException($"The Data Protection certificate is invalid. Current UTC time: {DateTimeOffset.UtcNow}, Certificate valid from: {certificate.NotBefore.ToUniversalTime()}, Certificate valid until: {certificate.NotAfter.ToUniversalTime()}.");

        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .ProtectKeysWithCertificate(certificate);

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<AppIdentityErrorDescriber>()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddApiEndpoints();

        services.AddScoped<IUserConfirmation<User>, AppUserConfirmation>();
        services.AddScoped(sp => (IUserEmailStore<User>)sp.GetRequiredService<IUserStore<User>>());
        services.AddScoped(sp => (IUserPhoneNumberStore<User>)sp.GetRequiredService<IUserStore<User>>());
        services.AddScoped(sp => (AppUserClaimsPrincipalFactory)sp.GetRequiredService<IUserClaimsPrincipalFactory<User>>());

        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.BearerScheme;
            options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
        })
        .AddBearerToken(IdentityConstants.BearerScheme, options =>
        {
            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = env.IsDevelopment() is false,
                IssuerSigningKey = new X509SecurityKey(certificate),

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateAudience = true,
                ValidAudience = identityOptions.Audience,

                ValidateIssuer = true,
                ValidIssuer = identityOptions.Issuer,

                AuthenticationType = IdentityConstants.BearerScheme
            };

            options.BearerTokenProtector = new AppJwtSecureDataFormat(appSettings, validationParameters);
            options.RefreshTokenProtector = new AppJwtSecureDataFormat(appSettings, validationParameters);

            options.Events = new()
            {
                OnMessageReceived = async context =>
                {
                    // The server accepts the accessToken from either the authorization header, the cookie, or the request URL query string
                    context.Token ??= context.Request.Query.ContainsKey("access_token") ? context.Request.Query["access_token"] : context.Request.Cookies["access_token"];
                }
            };

            configuration.GetRequiredSection("Identity").Bind(options);
        });

        services.AddAuthorization();

        if (string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]) is false)
        {
            authenticationBuilder.AddGoogle(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                configuration.GetRequiredSection("Authentication:Google").Bind(options);
            });
        }

        if (string.IsNullOrEmpty(configuration["Authentication:GitHub:ClientId"]) is false)
        {
            authenticationBuilder.AddGitHub(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                configuration.GetRequiredSection("Authentication:GitHub").Bind(options);
            });
        }

        if (string.IsNullOrEmpty(configuration["Authentication:Twitter:ConsumerKey"]) is false)
        {
            authenticationBuilder.AddTwitter(options =>
            {
                options.RetrieveUserDetails = true;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                configuration.GetRequiredSection("Authentication:Twitter").Bind(options);
            });
        }

        if (string.IsNullOrEmpty(configuration["Authentication:Apple:ClientId"]) is false)
        {
            authenticationBuilder.AddApple(options =>
            {
                options.UsePrivateKey(keyId =>
                {
                    return env.ContentRootFileProvider.GetFileInfo("AppleAuthKey.p8");
                });
                configuration.GetRequiredSection("Authentication:Apple").Bind(options);
            });
        }
    }

    private static void AddSwaggerGen(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Boilerplate.Server.Api.xml"), includeControllerXmlComments: true);
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
