//+:cnd:noEmit
using System.Net;
using System.Net.Mail;
using System.IO.Compression;
//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
using System.ClientModel.Primitives;
//#endif
//#if (database == "Sqlite")
using Microsoft.Data.Sqlite;
//#endif
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OData;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Twilio;
using Ganss.Xss;
using System.Text;
using Fido2NetLib;
using PhoneNumbers;
using FluentStorage;
using FluentEmail.Core;
using FluentStorage.Blobs;
using Hangfire.EntityFrameworkCore;
//#if (notification == true)
using AdsPush;
using AdsPush.Abstraction;
//#endif
//#if (filesStorage == "AzureBlobStorage")
using Azure.Storage.Blobs;
//#endif
using Boilerplate.Server.Api.Services;
using Boilerplate.Server.Api.Controllers;
using Boilerplate.Server.Shared.Services;
using Boilerplate.Server.Api.Services.Jobs;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Services.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api;

public static partial class Program
{
    public static void AddServerApiProjectServices(this WebApplicationBuilder builder)
    {
        // Services being registered here can get injected in server project only.
        var env = builder.Environment;
        var services = builder.Services;
        var configuration = builder.Configuration;

        builder.AddServerSharedServices();

        builder.AddDefaultHealthChecks()
            .AddDbContextCheck<AppDbContext>(tags: ["live"])
            .AddHangfire(setup => setup.MinimumAvailableServers = 1, tags: ["live"])
            .AddCheck<FluentStorageHealthCheck>("storage", tags: ["live"]);
        // TODO: Sms, Email, Push notification, AI, Google reCaptcha, Cloudflare

        ServerApiSettings appSettings = new();
        configuration.Bind(appSettings);

        services.AddScoped<EmailService>();
        services.AddScoped<EmailServiceJobsRunner>();
        services.AddScoped<PhoneService>();
        services.AddScoped<PhoneServiceJobsRunner>();
        //#if (module == "Sales" || module == "Admin")
        //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
        services.AddScoped<ProductEmbeddingService>();
        //#endif
        //#endif
        if (appSettings.Sms?.Configured is true)
        {
            TwilioClient.Init(appSettings.Sms.TwilioAccountSid, appSettings.Sms.TwilioAutoToken);
        }

        services.AddSingleton(_ => PhoneNumberUtil.GetInstance());
        services.AddSingleton<IBlobStorage>(sp =>
        {
            //#if (filesStorage == "AzureBlobStorage" || filesStorage == "S3")
            string GetValue(string connectionString, string key, string? defaultValue = null)
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.StartsWith($"{key}="))
                        return part[$"{key}=".Length..];
                }
                return defaultValue ?? throw new ArgumentException($"Invalid connection string: '{key}' not found.");
            }
            //#endif

            //#if (filesStorage == "Local")
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
            var appDataDirPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data");
            Directory.CreateDirectory(appDataDirPath);
            return StorageFactory.Blobs.DirectoryFiles(appDataDirPath);
            //#elif (filesStorage == "AzureBlobStorage")
            var azureBlobStorageConnectionString = configuration.GetConnectionString("AzureBlobStorageConnectionString")!;
            var blobServiceClient = new BlobServiceClient(azureBlobStorageConnectionString);
            string accountName = blobServiceClient.AccountName;
            string accountKey = azureBlobStorageConnectionString is "UseDevelopmentStorage=true" ? "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==" // https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage#well-known-storage-account-and-key
                : GetValue(azureBlobStorageConnectionString, "AccountKey");
            return StorageFactory.Blobs.AzureBlobStorageWithSharedKey(accountName, accountKey, blobServiceClient.Uri);
            //#elif (filesStorage == "S3")
            // Run through docker using `docker run -d -p 9000:9000 -p 9001:9001 -e "MINIO_ROOT_USER=minioadmin" -e "MINIO_ROOT_PASSWORD=minioadmin" quay.io/minio/minio server /data --console-address ":9001"`
            // Open MinIO console at http://127.0.0.1:9001/browser
            var s3ConnectionString = configuration.GetConnectionString("S3ConnectionString")!;
            var clientConfig = new Amazon.S3.AmazonS3Config
            {
                AuthenticationRegion = GetValue(s3ConnectionString, "Region", defaultValue: "us-east-1"),
                ServiceURL = GetValue(s3ConnectionString, "Endpoint"),
                ForcePathStyle = true,
                HttpClientFactory = sp.GetRequiredService<S3HttpClientFactory>()
            };
            return StorageFactory.Blobs.AwsS3(accessKeyId: GetValue(s3ConnectionString, "AccessKey"),
                secretAccessKey: GetValue(s3ConnectionString, "SecretKey"),
                sessionToken: null!,
                bucketName: GetValue(s3ConnectionString, "BucketName", defaultValue: "files"),
                clientConfig);
            //#else
            throw new NotImplementedException("Install and configure any storage supported by fluent storage (https://github.com/robinrodricks/FluentStorage/wiki/Blob-Storage)");
            //#endif
        });

        //#if (filesStorage == "S3")
        services.AddSingleton<S3HttpClientFactory>();
        services.AddHttpClient("S3");
        //#endif

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
        services.AddScoped<PushNotificationJobRunner>();
        //#endif

        services.AddSingleton<ServerExceptionHandler>();
        services.AddSingleton(sp => (IProblemDetailsWriter)sp.GetRequiredService<ServerExceptionHandler>());
        services.AddProblemDetails();

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

                policy.SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && settings.IsTrustedOrigin(uri))
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders(HeaderNames.RequestId, "Age", "App-Cache-Response");
            });
        });

        services.AddSingleton(sp =>
        {
            JsonSerializerOptions options = new JsonSerializerOptions(AppJsonContext.Default.Options);

            options.TypeInfoResolverChain.Add(IdentityJsonContext.Default);
            options.TypeInfoResolverChain.Add(ServerJsonContext.Default);

            return options;
        });

        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.AddRange([AppJsonContext.Default, IdentityJsonContext.Default, ServerJsonContext.Default]));

        services.AddSingleton<HtmlSanitizer>();

        services
            .AddControllers(options =>
            {
                if (appSettings.SupportedAppVersions is not null)
                {
                    options.Filters.Add<ForceUpdateActionFilter>();
                }
            })
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
            var connectionStringBuilder = new SqliteConnectionStringBuilder(configuration.GetConnectionString("SqliteConnectionString"));
            connectionStringBuilder.DataSource = Environment.ExpandEnvironmentVariables(connectionStringBuilder.DataSource);
            if (connectionStringBuilder.Mode is not SqliteOpenMode.Memory)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(connectionStringBuilder.DataSource)!);
            }
            options.UseSqlite(connectionStringBuilder.ConnectionString, dbOptions =>
            {
                // dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            //#endif
            //#if (IsInsideProjectTemplate == true)
            return;
            //#endif
            //#if (database == "SqlServer")
            options.UseSqlServer(configuration.GetConnectionString("SqlServerConnectionString"), dbOptions =>
            {
                if (AppDbContext.IsEmbeddingEnabled)
                {
                    dbOptions.UseVectorSearch();
                }
                // dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            //#elif (database == "PostgreSQL")
            var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(configuration.GetConnectionString("PostgreSQLConnectionString"));
            dataSourceBuilder.EnableDynamicJson();
            options.UseNpgsql(dataSourceBuilder.Build(), dbOptions =>
            {
                dbOptions.UseVector();
                // dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            //#elif (database == "MySql")
            options.UseMySql(configuration.GetConnectionString("MySqlConnectionString"), ServerVersion.AutoDetect(configuration.GetConnectionString("MySqlConnectionString")), dbOptions =>
            {
                // dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            //#elif (database == "Other")
            throw new NotImplementedException("Install and configure any database supported by ef core (https://learn.microsoft.com/en-us/ef/core/providers)");
            //#endif
        }

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

        services.AddDataProtection()
           .PersistKeysToDbContext<AppDbContext>(); // It's advised to secure database-stored keys with a certificate by invoking ProtectKeysWithCertificate.

        AddIdentity(builder);

        var emailSettings = appSettings.Email ?? throw new InvalidOperationException("Email settings are required.");
        var fluentEmailServiceBuilder = services.AddFluentEmail(emailSettings.DefaultFromEmail);

        fluentEmailServiceBuilder.AddSmtpSender(() =>
        {
            if (emailSettings.UseLocalFolderForEmails)
            {
                var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
                var sentEmailsFolderPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data", "sent-emails");

                Directory.CreateDirectory(sentEmailsFolderPath);

                return new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                    PickupDirectoryLocation = sentEmailsFolderPath
                };
            }

            if (emailSettings.HasCredential)
            {
                return new(emailSettings.Host, emailSettings.Port)
                {
                    Credentials = new NetworkCredential(emailSettings.UserName, emailSettings.Password),
                    EnableSsl = true
                };
            }

            return new(emailSettings.Host, emailSettings.Port);
        });

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
            c.DefaultRequestVersion = HttpVersion.Version11;
        });

        services.AddHttpClient<ResponseCacheService>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
            //#if (cloudflare == true)
            c.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/zones/");
            //#endif
        });

        services.AddFido2(options =>
        {

        });

        services.AddScoped(sp =>
        {
            var webAppUrl = sp.GetRequiredService<IHttpContextAccessor>()
                .HttpContext!.Request.GetWebAppUrl();

            var options = new Fido2Configuration
            {
                ServerDomain = webAppUrl.Host,
                TimestampDriftTolerance = 1000,
                ServerName = "Boilerplate WebAuthn",
                Origins = new HashSet<string>([webAppUrl.AbsoluteUri]),
                ServerIcon = new Uri(webAppUrl, "images/icons/bit-logo.png").ToString()
            };

            return options;
        });

        //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
        services.AddHttpClient("AI");

        if (string.IsNullOrEmpty(appSettings.AI?.OpenAI?.ChatApiKey) is false)
        {
            // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.OpenAI#microsoftextensionsaiopenai
            services.AddChatClient(sp => new OpenAI.Chat.ChatClient(model: appSettings.AI.OpenAI.ChatModel, credential: new(appSettings.AI.OpenAI.ChatApiKey), options: new()
            {
                Endpoint = appSettings.AI.OpenAI.ChatEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIChatClient())
            .UseLogging()
            .UseFunctionInvocation()
            .UseOpenTelemetry();
            // .UseDistributedCache()
        }
        else if (string.IsNullOrEmpty(appSettings.AI?.AzureOpenAI?.ChatApiKey) is false)
        {
            // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.AzureAIInference#microsoftextensionsaiazureaiinference
            services.AddChatClient(sp => new Azure.AI.Inference.ChatCompletionsClient(endpoint: appSettings.AI.AzureOpenAI.ChatEndpoint,
                credential: new Azure.AzureKeyCredential(appSettings.AI.AzureOpenAI.ChatApiKey),
                options: new()
                {
                    Transport = new Azure.Core.Pipeline.HttpClientTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
                }).AsIChatClient(appSettings.AI.AzureOpenAI.ChatModel))
            .UseLogging()
            .UseFunctionInvocation()
            .UseOpenTelemetry();
            // .UseDistributedCache()
        }

        if (string.IsNullOrEmpty(appSettings.AI?.OpenAI?.EmbeddingApiKey) is false)
        {
            services.AddEmbeddingGenerator(sp => new OpenAI.Embeddings.EmbeddingClient(model: appSettings.AI.OpenAI.EmbeddingModel, credential: new(appSettings.AI.OpenAI.EmbeddingApiKey), options: new()
            {
                Endpoint = appSettings.AI.OpenAI.EmbeddingEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIEmbeddingGenerator())
            .UseLogging()
            .UseOpenTelemetry();
            // .UseDistributedCache()
        }
        else if (string.IsNullOrEmpty(appSettings.AI?.AzureOpenAI?.EmbeddingApiKey) is false)
        {
            services.AddEmbeddingGenerator(sp => new Azure.AI.Inference.EmbeddingsClient(endpoint: appSettings.AI.AzureOpenAI.EmbeddingEndpoint,
                credential: new Azure.AzureKeyCredential(appSettings.AI.AzureOpenAI.EmbeddingApiKey),
                options: new()
                {
                    Transport = new Azure.Core.Pipeline.HttpClientTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
                }).AsIEmbeddingGenerator(appSettings.AI.AzureOpenAI.EmbeddingModel))
            .UseLogging()
            .UseOpenTelemetry();
            // .UseDistributedCache()
        }
        //#endif

        builder.Services.AddHangfire(configuration =>
        {
            var efCoreStorage = configuration.UseEFCoreStorage(optionsBuilder =>
            {
                if (appSettings.Hangfire?.UseIsolatedStorage is true)
                {
                    var connectionString = "Data Source=BoilerplateJobs.db;Mode=Memory;Cache=Shared;";
                    var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                    connection.Open();
                    AppContext.SetData("ReferenceTheKeepTheInMemorySQLiteDatabaseAlive", connection);
                    optionsBuilder.UseSqlite(connectionString);
                }
                else
                {
                    AddDbContext(optionsBuilder);
                }
            }, new()
            {
                Schema = "jobs",
                QueuePollInterval = new TimeSpan(0, 0, 1)
            });

            if (appSettings.Hangfire?.UseIsolatedStorage is true)
            {
                efCoreStorage.UseDatabaseCreator();
            }

            configuration.UseRecommendedSerializerSettings();
            configuration.UseSimpleAssemblyNameTypeSerializer();
            configuration.UseIgnoredAssemblyVersionTypeResolver();
            configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
        });

        builder.Services.AddHangfireServer(options =>
        {
            options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
            configuration.Bind("Hangfire", options);
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

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<AppIdentityErrorDescriber>()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddApiEndpoints();

        services.AddScoped<UserClaimsService>();
        services.AddScoped<IUserConfirmation<User>, AppUserConfirmation>();
        services.AddScoped(sp => (IUserEmailStore<User>)sp.GetRequiredService<IUserStore<User>>());
        services.AddScoped(sp => (IUserPhoneNumberStore<User>)sp.GetRequiredService<IUserStore<User>>());
        services.AddScoped(sp => (AppUserClaimsPrincipalFactory)sp.GetRequiredService<IUserClaimsPrincipalFactory<User>>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenOptions>, AppBearerTokenOptionsConfigurator>());
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.BearerScheme;
            options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
        })
        .AddBearerToken(IdentityConstants.BearerScheme /*Checkout AppBearerTokenOptionsConfigurator*/ );

        services.AddAuthorization();

        if (string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]) is false)
        {
            authenticationBuilder.AddGoogle(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.AdditionalAuthorizationParameters["prompt"] = "select_account";
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

        if (string.IsNullOrEmpty(configuration["Authentication:AzureAD:ClientId"]) is false)
        {
            authenticationBuilder.AddMicrosoftIdentityWebApp(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.Events = new()
                {
                    OnTokenValidated = async context =>
                    {
                        var props = new AuthenticationProperties();
                        props.Items["LoginProvider"] = "AzureAD";
                        await context.HttpContext.SignInAsync(IdentityConstants.ExternalScheme, context.Principal!, props);
                    }
                };
                configuration.GetRequiredSection("Authentication:AzureAD").Bind(options);
            }, openIdConnectScheme: "AzureAD");
        }

        if (string.IsNullOrEmpty(configuration["Authentication:Facebook:AppId"]) is false)
        {
            authenticationBuilder.AddFacebook(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                configuration.GetRequiredSection("Authentication:Facebook").Bind(options);
            });
        }

        // While Google, GitHub, Twitter(X), Apple and AzureAD needs account creation in their corresponding developer portals,
        // and configuring the client ID and secret, the following OpenID Connect configuration is for Duende IdentityServer demo server,
        // which is a public server that allows you to test Social sign-in feature without needing to configure anything.
        // Note: The following demo server doesn't require licensing.
        if (builder.Environment.IsDevelopment())
        {
            authenticationBuilder.AddOpenIdConnect("IdentityServerDemo", options =>
            {
                options.Authority = "https://demo.duendesoftware.com";

                options.ClientId = "interactive.confidential";
                options.ClientSecret = "secret";
                options.ResponseType = "code";
                options.ResponseMode = "query";

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("api");
                options.Scope.Add("offline_access");
                options.Scope.Add("email");

                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;
                options.DisableTelemetry = true;

                options.Prompt = "login"; // Force login every time
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
