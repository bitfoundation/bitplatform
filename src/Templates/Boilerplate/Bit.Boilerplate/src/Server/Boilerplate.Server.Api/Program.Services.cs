//+:cnd:noEmit
using System.Net;
using System.Net.Mail;
using ImageMagick;
using Boilerplate.Server.Api.Features.Identity;
using Boilerplate.Server.Api.Features.Attachments;
using Boilerplate.Server.Api.Features.PersonalData;
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif
//#if (sample == true || offlineDb == true)
using Boilerplate.Server.Api.Features.Todo;
//#endif
//#if (signalR == true)
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Boilerplate.Shared.Features.Chatbot;
//#endif
//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
using System.ClientModel.Primitives;
//#endif
//#if (database == "PostgreSQL")
using Npgsql;
//#endif
//#if (database == "Sqlite")
using Microsoft.Data.Sqlite;
//#endif
using Microsoft.OpenApi;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.OData;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Twilio;
using Ganss.Xss;
using Fido2NetLib;
using PhoneNumbers;
using FluentStorage;
using FluentEmail.Core;
using FluentStorage.Storage;
using Hangfire.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
//#if (redis == true)
using StackExchange.Redis;
using Hangfire.Redis.StackExchange;
//#endif
//#if (notification == true)
using AdsPush;
using AdsPush.Abstraction;
//#endif
//#if (filesStorage == "AzureBlobStorage")
using Azure.Storage.Blobs;
//#endif
using Medallion.Threading;
//#if (offlineDb == true)
using CommunityToolkit.Datasync.Server;
//#endif
using Boilerplate.Server.Api.Features.Statistics;
using Boilerplate.Shared.Infrastructure.Resources;
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif
//#if (module == "Sales" || module == "Admin")
using Boilerplate.Server.Api.Features.Products;
//#endif

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

        builder.AddServerApiHealthChecks();

        ServerApiSettings appSettings = new();
        configuration.Bind(appSettings);

        ConfigureImageMagickResourceLimits();

        services.AddScoped<IdentityEmailService>();
        services.AddScoped<EmailServiceJobsRunner>();
        services.AddScoped<PhoneService>();
        services.AddScoped<PhoneServiceJobsRunner>();
        services.AddScoped<UserErasureService>();
        services.AddScoped<UserSessionsRetentionJobRunner>();
        services.AddScoped<UnconfirmedUsersRetentionJobRunner>();

        services.AddPersonalDataServices();
        //#if (signalR == true)
        services.AddScoped<Features.Attachments.AiChatImagesRetentionJobRunner>();
        services.AddScoped<Infrastructure.SignalR.AppChatbot>();
        //#endif
        services.AddDevMcp()
        //#if (signalR == true)
            .WithToolsFromAssembly() // Chatbot tools, served on /mcp only (See DevMcpServiceCollectionExtensions).
        //#endif
            ;
        //#if (module == "Sales" || module == "Admin")
        //#if (database == "PostgreSQL" || database == "SqlServer")
        services.AddScoped<ProductEmbeddingService>();
        //#endif
        //#endif
        if (appSettings.Sms?.Configured is true)
        {
            TwilioClient.Init(appSettings.Sms.TwilioAccountSid, appSettings.Sms.TwilioAutoToken);
        }

        services.AddSingleton(_ => PhoneNumberUtil.GetInstance());
        services.AddSingleton<IStore>(sp =>
        {
            //#if (filesStorage == "Local")
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
            var appDataDirPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data");
            Directory.CreateDirectory(appDataDirPath);
            return StorageFactory.Disk(appDataDirPath);
            //#elif (filesStorage == "AzureBlobStorage")
            var azureBlobStorageConnectionString = configuration.GetRequiredConnectionString("azureblobstorage")!;
            var blobServiceClient = new BlobServiceClient(azureBlobStorageConnectionString);
            string accountName = blobServiceClient.AccountName;
            string accountKey = azureBlobStorageConnectionString is "UseDevelopmentStorage=true" ? "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==" // https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage#well-known-storage-account-and-key
                : GetConnectionStringValue(azureBlobStorageConnectionString, "AccountKey");
            return AzureBlobStorage.FromSharedKey(accountName, accountKey, blobServiceClient.Uri);
            //#elif (filesStorage == "S3")
            // Run through docker using `docker run -d -p 9000:9000 -p 9001:9001 -e "MINIO_ROOT_USER=minioadmin" -e "MINIO_ROOT_PASSWORD=minioadmin" quay.io/minio/minio server /data --console-address ":9001"`
            // Open MinIO console at http://127.0.0.1:9001/browser
            var s3ConnectionString = configuration.GetRequiredConnectionString("s3")!;
            var clientConfig = new Amazon.S3.AmazonS3Config
            {
                AuthenticationRegion = GetConnectionStringValue(s3ConnectionString, "Region", defaultValue: "us-east-1"),
                ServiceURL = GetConnectionStringValue(s3ConnectionString, "Endpoint"),
                ForcePathStyle = true,
                HttpClientFactory = sp.GetRequiredService<S3HttpClientFactory>()
            };
            return AwsS3Storage.FromThirdPartyCredentials(accessKeyId: GetConnectionStringValue(s3ConnectionString, "AccessKey"),
                secretAccessKey: GetConnectionStringValue(s3ConnectionString, "SecretKey"),
                sessionToken: null!,
                bucketName: GetConnectionStringValue(s3ConnectionString, "BucketName", defaultValue: "files"),
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
        services.AddHttpClient("APNS"); // Apple Push Notification Service
        services.AddHttpClient("Vapid"); // Web Push
        services.AddSingleton(sp =>
        {
            var adsPushSenderBuilder = new AdsPushSenderBuilder();

            if (string.IsNullOrWhiteSpace(appSettings.AdsPushAPNS?.P8PrivateKey) is false)
            {
                adsPushSenderBuilder = adsPushSenderBuilder.ConfigureApns(appSettings.AdsPushAPNS, sp.GetRequiredService<IHttpClientFactory>().CreateClient("APNS"));
            }

            if (string.IsNullOrWhiteSpace(appSettings.AdsPushFirebase?.PrivateKey) is false)
            {
                appSettings.AdsPushFirebase.PrivateKey = appSettings.AdsPushFirebase.PrivateKey.Replace(@"\n", string.Empty);

                adsPushSenderBuilder = adsPushSenderBuilder.ConfigureFirebase(appSettings.AdsPushFirebase, AdsPushTarget.Android);
            }

            if (string.IsNullOrWhiteSpace(appSettings.AdsPushVapid?.PrivateKey) is false)
            {
                if (string.IsNullOrWhiteSpace(appSettings.AdsPushVapid.PublicKey))
                    throw new InvalidOperationException("VAPID public key is required");
                if (string.IsNullOrWhiteSpace(appSettings.AdsPushVapid.Subject))
                    throw new InvalidOperationException("VAPID subject is required"); // While it would work on Android, Windows, Linux, Apple requires subject, so we enforce it for all platforms to avoid confusion and potential issues.

                adsPushSenderBuilder = adsPushSenderBuilder.ConfigureVapid(appSettings.AdsPushVapid, sp.GetRequiredService<IHttpClientFactory>().CreateClient("Vapid"));
            }

            return adsPushSenderBuilder
                .BuildSender();
        });
        services.AddScoped<PushNotificationService>();
        services.AddScoped<PushNotificationJobRunner>();
        services.AddScoped<PushSubscriptionsRetentionJobRunner>();
        //#endif

        // Register distributed lock factory
        //#if (redis == true)
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        services.AddTransient(sp => new DistributedLockFactory((string lockKey) =>
        {
            return new Medallion.Threading.Redis.RedisDistributedLock(lockKey, sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis-persistent").GetDatabase());
        }));
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#else
        services.AddTransient(sp => new DistributedLockFactory((string lockKey) =>
        {
            return new Medallion.Threading.FileSystem.FileDistributedLock(new(Path.Combine(Path.GetTempPath(), $"Boilerplate-{lockKey}.lock")));
        }));
        //#endif

        services.AddSingleton<ApiServerExceptionHandler>();
        services.AddSingleton<SharedExceptionHandler>(sp => sp.GetRequiredService<ApiServerExceptionHandler>());
        services.AddSingleton(sp => (IProblemDetailsWriter)sp.GetRequiredService<ApiServerExceptionHandler>());
        services.AddProblemDetails();

        services.AddCors(builder =>
        {
            CorsPolicyBuilder ApplyPolicyDefaults(CorsPolicyBuilder policy)
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
                      .WithExposedHeaders(HeaderNames.RequestId,
                            HeaderNames.Age, "App-Cache-Response", "X-App-Platform", "X-App-Version", "X-Origin");

                return policy;
            }

            builder.AddDefaultPolicy(policy =>
            {
                ApplyPolicyDefaults(policy);
            });

            // Required for Cookies.Delete & Cookies.Append to work.
            builder.AddPolicy("CorsWithCredentials", policy =>
            {
                ApplyPolicyDefaults(policy)
                    .AllowCredentials();
            });
        });

        services.AddRateLimiter(options => options.AddAppRateLimitPolicies());

        services.AddSingleton(sp =>
        {
            JsonSerializerOptions options = new JsonSerializerOptions(AppJsonContext.Default.Options);

            options.TypeInfoResolverChain.Add(IdentityJsonContext.Default);
            options.TypeInfoResolverChain.Add(ServerJsonContext.Default);

            return options;
        });

        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.ApplyDefaultOptions());

        services.AddSingleton<HtmlSanitizer>();

        services
            .AddControllers(options => options.Filters.Add<AutoCsrfProtectionFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.ApplyDefaultOptions())
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

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1);
        })
        .AddMvc() // For API Controllers
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        });

        //#if (signalR == true)
        var signalRBuilder = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = env.IsDevelopment();
            configuration.GetRequiredSection("HubOptions").Bind(options);
        }).AddJsonProtocol(options => options.PayloadSerializerOptions.ApplyDefaultOptions());

        if (string.IsNullOrWhiteSpace(configuration["Azure:SignalR:ConnectionString"]) is false)
        {
            signalRBuilder.AddAzureSignalR(options =>
            {
                configuration.GetRequiredSection("Azure:SignalR").Bind(options);
            });
        }
        //#if (redis == true)
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        else
        {
            // Use Redis as SignalR backplane for scaling out across multiple server instances
            signalRBuilder.AddStackExchangeRedis(options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("Boilerplate:SignalR:");
            });
        }
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
        //#endif

        //#if (database == "PostgreSQL")
        services.AddSingleton(_ =>
        {
            var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(configuration.GetRequiredConnectionString("postgresdb"));
            dataSourceBuilder.UseVector();
            dataSourceBuilder.EnableDynamicJson();
            return dataSourceBuilder.Build();
        });
        //#endif

        services.AddDbContextPool<AppDbContext>(AddDbContext);
        services.AddPooledDbContextFactory<AppDbContext>(AddDbContext);

        void AddDbContext(IServiceProvider sp, DbContextOptionsBuilder options)
        {
            options.EnableSensitiveDataLogging(env.IsDevelopment())
                .EnableDetailedErrors(env.IsDevelopment());

            //#if (database == "Sqlite")
            var connectionStringBuilder = new SqliteConnectionStringBuilder(configuration.GetRequiredConnectionString("sqlite"));
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
            options.UseSqlServer(configuration.GetRequiredConnectionString("mssqldb"), dbOptions =>
            {
                dbOptions.UseCompatibilityLevel(170); // SQL Server 2025
                // dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                dbOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            //#elif (database == "PostgreSQL")
            options.UseNpgsql(sp.GetRequiredService<Npgsql.NpgsqlDataSource>(), dbOptions =>
            {
                dbOptions.UseVector();
                dbOptions.SetPostgresVersion(18, 0);
                // dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                dbOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
            //#elif (database == "MySql")
            options.UseMySql(configuration.GetRequiredConnectionString("mysqldb"), ServerVersion.AutoDetect(configuration.GetRequiredConnectionString("mysqldb")), dbOptions =>
            {
                // dbOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                dbOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            //#elif (database == "Other")
            throw new NotImplementedException("Install and configure any database supported by ef core (https://learn.microsoft.com/en-us/ef/core/providers)");
            //#endif
        }

        //#if (offlineDb == true)
        // Register CommunityToolkit.Datasync services and repositories
        services.AddDatasyncServices();
        services.AddScoped<Features.Todo.TodoItemTableRepository>();
        //#endif

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

        services.AddOpenApi(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;

            options.AddOperationTransformer(async (operation, context, cancellationToken) =>
            {
                var isAuthorizedAction = context.Description.ActionDescriptor.EndpointMetadata.Any(em => em is AuthorizeAttribute);
                var isODataEnabledAction = context.Description.ActionDescriptor.FilterDescriptors.Any(f => f.Filter is EnableQueryAttribute);

                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter()
                {
                    In = ParameterLocation.Header,
                    Name = HeaderNames.Authorization,
                    Example = "Bearer XXX.YYY...",
                    Description = "Get your JWT token by signin-in through Identity/SignIn endpoint",
                    Required = isAuthorizedAction
                });

                if (isODataEnabledAction)
                {
                    operation.Parameters.AddRange([

                        new OpenApiParameter() { In = ParameterLocation.Query, Name = "$filter", Description = "Filters the results, based on a Boolean condition. (ex. Age gt 25)" },
                        new OpenApiParameter() { In = ParameterLocation.Query, Name = "$select", Description = "Returns only the selected properties. (ex. FirstName, LastName)" },
                        new OpenApiParameter() { In = ParameterLocation.Query, Name = "$expand", Description = "Include only the selected objects. (ex. Orders, Locations)" },
                        new OpenApiParameter() { In = ParameterLocation.Query, Name = "$search", Description = "Finds resources that match a search criteria. (ex. \"search term\")" },
                        new OpenApiParameter() { In = ParameterLocation.Query, Name = "$top", Description = "Returns only the first n items from a collection. (ex. 10)" },
                        new OpenApiParameter() { In = ParameterLocation.Query, Name = "$skip", Description = "Skips the first n items from a collection. (ex. 10)" },
                        new OpenApiParameter() { In = ParameterLocation.Query, Name = "$orderby", Description = "Orders the results of a query by one or more properties. (ex. Name desc)" }
                    ]);
                }
            });
        });

        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .ProtectKeysWithCertificate(AppCertificateService.GetActiveAppCertificate(configuration))
            .UnprotectKeysWithAnyCertificate(AppCertificateService.GetAllAppCertificates(configuration));

        AddIdentity(builder);

        var emailSettings = appSettings.Email ?? throw new InvalidOperationException("Email settings are required.");
        var fluentEmailServiceBuilder = services.AddFluentEmail(emailSettings.DefaultFromEmail);
        fluentEmailServiceBuilder.AddSmtpSender(() =>
        {
            var smtpConnectionString = configuration.GetRequiredConnectionString("smtp")!;
            var endpoint = new Uri(GetConnectionStringValue(smtpConnectionString, "Endpoint", "smtp://localhost:25"));
            var host = endpoint.Host;
            var port = endpoint.Port is -1 ? 25 : endpoint.Port;
            var userName = GetConnectionStringValue(smtpConnectionString, "UserName", string.Empty);
            var password = GetConnectionStringValue(smtpConnectionString, "Password", string.Empty);
            var enableSsl = GetConnectionStringValue(smtpConnectionString, "EnableSsl", port == 465 || port == 587 ? "true" : "false").Equals("false", StringComparison.OrdinalIgnoreCase) is false;

            SmtpClient smtpClient = new(host, port)
            {
                EnableSsl = enableSsl
            };

            if (string.IsNullOrEmpty(userName) is false
                && string.IsNullOrEmpty(password) is false)
            {
                smtpClient.Credentials = new NetworkCredential(userName.ToString(), password.ToString());
            }

            return smtpClient;
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
            c.Timeout = TimeSpan.FromSeconds(20);
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

        services.AddHttpClient("Keycloak", c =>
        {
            c.BaseAddress = new Uri(configuration["KEYCLOAK_HTTP"]
                ?? configuration["Authentication:Keycloak:KeycloakUrl"]
                ?? throw new InvalidOperationException("KEYCLOAK_HTTP configuration is required"));
            c.DefaultRequestVersion = HttpVersion.Version11;
        });

        services.AddFido2(options =>
        {

        });

        // ServerDomain (the WebAuthn RP ID) is resolved PER REQUEST from GetWebAppUrl(), which honours a
        // caller-supplied origin. See ".docs/24 - Security note" for what that means for Blazor Hybrid passkeys.
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

        if (string.IsNullOrWhiteSpace(appSettings.AI?.OpenAI?.ChatApiKey) is false)
        {
            // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.OpenAI#microsoftextensionsaiopenai
            services.AddChatClient(sp => new OpenAI.Chat.ChatClient(model: appSettings.AI.OpenAI.ChatModel, credential: new(appSettings.AI.OpenAI.ChatApiKey), options: new()
            {
                Endpoint = appSettings.AI.OpenAI.ChatEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIChatClient())
            .UseLogging()
            .UseFunctionInvocation()
            .UseOpenTelemetry(configure: c => c.EnableSensitiveData = env.IsDevelopment());
            // .UseDistributedCache()

            //#if (signalR == true)
            builder.AddAppAIAgents();
            //#endif
        }

        if (string.IsNullOrWhiteSpace(appSettings.AI?.OpenAI?.EmbeddingApiKey) is false)
        {
            services.AddEmbeddingGenerator(sp => new OpenAI.Embeddings.EmbeddingClient(model: appSettings.AI.OpenAI.EmbeddingModel, credential: new(appSettings.AI.OpenAI.EmbeddingApiKey), options: new()
            {
                Endpoint = appSettings.AI.OpenAI.EmbeddingEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIEmbeddingGenerator())
            .UseLogging()
            .UseOpenTelemetry(configure: c => c.EnableSensitiveData = env.IsDevelopment());
            // .UseDistributedCache()
        }
        else if (string.IsNullOrWhiteSpace(appSettings.AI?.HuggingFace?.EmbeddingEndpoint) is false)
        {
            services.AddEmbeddingGenerator(sp => new Microsoft.SemanticKernel.Connectors.HuggingFace.HuggingFaceEmbeddingGenerator(
                  new Uri(appSettings.AI.HuggingFace.EmbeddingEndpoint),
                  apiKey: appSettings.AI.HuggingFace.EmbeddingApiKey,
                  httpClient: sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"), loggerFactory: sp.GetRequiredService<ILoggerFactory>()))
            .UseLogging()
            .UseOpenTelemetry(configure: c => c.EnableSensitiveData = env.IsDevelopment());
            // .UseDistributedCache()
        }

        //#if (signalR == true)
        // Speech in and speech out for the AI chat panel (See ChatbotController.TranscribeSpeech / SynthesizeSpeech).
        // They are Microsoft.Extensions.AI abstractions rather than the browser's Web Speech api, so a phone's web
        // view, a home-screen pwa and the MAUI app all behave the same - Web Speech is missing or crippled in most
        // of them. Each one is optional on its own: with no key the corresponding button is never offered.
#pragma warning disable MEAI001 // ISpeechToTextClient and ITextToSpeechClient are still experimental.
        if (string.IsNullOrWhiteSpace(appSettings.AI?.OpenAI?.SpeechToTextApiKey) is false)
        {
            services.AddSpeechToTextClient(sp => new OpenAI.Audio.AudioClient(model: appSettings.AI.OpenAI.SpeechToTextModel, credential: new(appSettings.AI.OpenAI.SpeechToTextApiKey), options: new()
            {
                Endpoint = appSettings.AI.OpenAI.SpeechToTextEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsISpeechToTextClient())
            .UseLogging()
            .UseOpenTelemetry(configure: c => c.EnableSensitiveData = env.IsDevelopment());
        }

        if (string.IsNullOrWhiteSpace(appSettings.AI?.OpenAI?.TextToSpeechApiKey) is false)
        {
            services.AddTextToSpeechClient(sp => new OpenAI.Audio.AudioClient(model: appSettings.AI.OpenAI.TextToSpeechModel, credential: new(appSettings.AI.OpenAI.TextToSpeechApiKey), options: new()
            {
                Endpoint = appSettings.AI.OpenAI.TextToSpeechEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsITextToSpeechClient())
            .UseLogging()
            .UseOpenTelemetry(configure: c => c.EnableSensitiveData = env.IsDevelopment());
        }
#pragma warning restore MEAI001
        //#endif
        //#endif

        var hangfireOptions = appSettings.Hangfire ?? throw new InvalidOperationException($"The {nameof(ServerApiSettings.Hangfire)} configuration section is required.");

        // Configure Hangfire to use Redis for persistent background job storage
        services.AddHangfire((sp, hangfireConfiguration) =>
        {
            if (hangfireOptions.UseIsolatedStorage is not true)
            {
                //#if (redis == true)
                //#if (IsInsideProjectTemplate == true)
                /*
                //#endif
                hangfireConfiguration.UseRedisStorage(sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis-persistent"), new RedisStorageOptions
                {
                    Prefix = "Boilerplate:Hangfire:",
                    Db = 1, // Use a dedicated Redis database for Hangfire
                }).WithJobExpirationTimeout(hangfireOptions.JobExpiration);
                //#if (IsInsideProjectTemplate == true)
                */
                //#endif
                //#else
                hangfireConfiguration.UseEFCoreStorage(optionsBuilder => AddDbContext(sp, optionsBuilder), new()
                {
                    Schema = "jobs",
                    QueuePollInterval = new TimeSpan(0, 0, 1)
                }).WithJobExpirationTimeout(hangfireOptions.JobExpiration);
                //#endif
            }
            else
            {
                var isRunningInsideDocker = Directory.Exists("/container_volume"); // It's supposed to be a mounted volume named /container_volume
                var appDataDirPath = Path.Combine(isRunningInsideDocker ? "/container_volume" : Directory.GetCurrentDirectory(), "App_Data");
                Directory.CreateDirectory(appDataDirPath);
                hangfireConfiguration.UseEFCoreStorage(optionsBuilder =>
                {
                    optionsBuilder.UseSqlite($"Data Source={Path.Combine(appDataDirPath, "BoilerplateJobDb.db")};");
                }, new()
                {
                    Schema = "jobs",
                    QueuePollInterval = new TimeSpan(0, 0, 1)
                })
                .UseDatabaseCreator()
                .WithJobExpirationTimeout(hangfireOptions.JobExpiration);
            }

            hangfireConfiguration.UseRecommendedSerializerSettings();
            hangfireConfiguration.UseSimpleAssemblyNameTypeSerializer();
            hangfireConfiguration.UseIgnoredAssemblyVersionTypeResolver();
            hangfireConfiguration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
        });

        services.AddHangfireServer(options =>
        {
            options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
            configuration.Bind("Hangfire", options);
        });
    }

    //#if (signalR == true)
    private static void AddAppAIAgents(this WebApplicationBuilder builder)
    {
        static string GetSystemPrompt(PromptKind promptKind, IServiceProvider sp)
        {
            var cache = sp.GetRequiredService<IFusionCache>();
            var dbContext = sp.GetRequiredService<AppDbContext>();
            //#if (multitenant == true)
            var tenantId = sp.GetRequiredService<TenantProvider>().GetCurrentTenantId();
            var cacheKey = $"SystemPrompt_{tenantId}_{promptKind}";
            //#endif
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
            //#if (multitenant != true)
            var cacheKey = $"SystemPrompt_{promptKind}";
            //#endif
            //#if (IsInsideProjectTemplate == true)
            */
            //#endif
            var result = cache.GetOrSet(
                cacheKey, _ =>
                {
                    var prompt = dbContext.SystemPrompts.FirstOrDefault(p => p.PromptKind == promptKind);
                    return prompt?.Markdown ?? throw new ResourceNotFoundException().WithData("Reason", $"System prompt for '{promptKind}' not found.");
                },
                options => options.SetDuration(TimeSpan.FromHours(1)).SetPriority(CacheItemPriority.High));
            return result;
        }

        //#if (module == "Sales" || module == "Admin")
        builder.AddAIAgent("AnalyzeProductImageAgent", (sp, _) => sp.GetRequiredService<IChatClient>().AsAIAgent(instructions: GetSystemPrompt(PromptKind.AnalyzeProductImage, sp),
                    name: "AnalyzeProductImageAgent",
                    description: "Analyzes product images to ensure they meet catalog standards for car products"), lifetime: ServiceLifetime.Scoped);
        //#endif

        builder.AddAIAgent("SupportAgent", (sp, _) =>
        {
            var aiFunctions = sp.GetRequiredService<AppChatbot>().GetAIFunctions();

            return sp.GetRequiredService<IChatClient>().AsAIAgent(instructions: GetSystemPrompt(PromptKind.Support, sp),
                    name: "SupportAgent",
                    description: "Provides support and assistance to users", tools: [.. aiFunctions]);
        }, lifetime: ServiceLifetime.Scoped);
    }
    //#endif

    private static void AddIdentity(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;
        ServerApiSettings appSettings = new();
        configuration.Bind(appSettings);

        services.AddIdentity<User, Features.Identity.Models.Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<AppIdentityErrorDescriber>()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddApiEndpoints();

        services.AddScoped<UserClaimsService>();
        //#if (multitenant == true)
        services.AddSingleton<TenantProvider>();
        // Replaces the default RoleValidator to scope the role name uniqueness by the role's TenantId.
        services.Replace(ServiceDescriptor.Scoped<IRoleValidator<Features.Identity.Models.Role>, AppRoleValidator>());
        //#endif
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

        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) is false)
        {
            authenticationBuilder.AddGoogle(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.AdditionalAuthorizationParameters["prompt"] = "select_account";
                configuration.GetRequiredSection("Authentication:Google").Bind(options);
            });
        }

        if (string.IsNullOrWhiteSpace(configuration["Authentication:GitHub:ClientId"]) is false)
        {
            authenticationBuilder.AddGitHub(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                configuration.GetRequiredSection("Authentication:GitHub").Bind(options);
            });
        }

        if (string.IsNullOrWhiteSpace(configuration["Authentication:Twitter:ConsumerKey"]) is false)
        {
            authenticationBuilder.AddTwitter(options =>
            {
                options.RetrieveUserDetails = true;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                configuration.GetRequiredSection("Authentication:Twitter").Bind(options);
            });
        }

        if (string.IsNullOrWhiteSpace(configuration["Authentication:Apple:ClientId"]) is false)
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

        if (string.IsNullOrWhiteSpace(configuration["Authentication:AzureAD:ClientId"]) is false)
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

        if (string.IsNullOrWhiteSpace(configuration["Authentication:Facebook:AppId"]) is false)
        {
            authenticationBuilder.AddFacebook(options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                configuration.GetRequiredSection("Authentication:Facebook").Bind(options);
            });
        }

        var keycloakBaseUrl = configuration["KEYCLOAK_HTTP"]
            ?? configuration["Authentication:Keycloak:KeycloakUrl"];

        if (string.IsNullOrWhiteSpace(keycloakBaseUrl) is false)
        {
            // In order to have better understanding of Keycloak integration, checkout .docs/07- ASP.NET Core Identity - Authentication & Authorization.md
            authenticationBuilder.AddOpenIdConnect("Keycloak", options =>
            {
                configuration.GetRequiredSection("Authentication:Keycloak").Bind(options);

                var realm = configuration["Authentication:Keycloak:Realm"] ?? throw new InvalidOperationException("Authentication:Keycloak:Realm configuration is required");

                options.Authority = $"{keycloakBaseUrl.TrimEnd('/')}/realms/{realm}";

                options.ResponseType = "code";
                options.ResponseMode = "query";

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("offline_access"); // To get refresh tokens

                options.MapInboundClaims = true;
                options.SaveTokens = true;

                options.Prompt = "login"; // Force login every time

                if (env.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                }
            });
        }

        services.ConfigureHttpClientFactoryForExternalIdentityProviders();
    }

    /// <summary>
    /// Reads a single `key=value` entry out of a `;`-separated connection string.
    /// Trimming and the case-insensitive comparison are required, not cosmetic: connection strings are commonly
    /// written with a space after the separator, and a plain ordinal StartsWith on an untrimmed segment makes
    /// every key after the first invisible - which silently drops SMTP credentials rather than failing loudly.
    /// Only the FIRST '=' is consumed, so values containing '=' (e.g. base64 padding) survive intact.
    /// </summary>
    private static string GetConnectionStringValue(string connectionString, string key, string? defaultValue = null)
    {
        var prefix = $"{key}=";
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (trimmedPart.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return trimmedPart[prefix.Length..];
        }
        return defaultValue ?? throw new ArgumentException($"Invalid connection string: '{key}' not found.");
    }

    /// <summary>
    /// A feature that holds personal data registers its source here: what is missing from this list is missing from
    /// every export and every erasure. See <see cref="Features.PersonalData.IPersonalDataSource"/>.
    /// </summary>
    private static IServiceCollection AddPersonalDataServices(this IServiceCollection services)
    {
        services.AddScoped<PersonalDataExportService>();

        services.AddScoped<IPersonalDataSource, IdentityPersonalDataSource>();
        services.AddScoped<IPersonalDataSource, UserSessionsPersonalDataSource>();
        services.AddScoped<IPersonalDataSource, AttachmentsPersonalDataSource>();
        //#if (notification == true)
        services.AddScoped<IPersonalDataSource, PushNotificationsPersonalDataSource>();
        //#endif
        //#if (multitenant == true)
        services.AddScoped<IPersonalDataSource, TenantsPersonalDataSource>();
        //#endif
        //#if (sample == true || offlineDb == true)
        services.AddScoped<IPersonalDataSource, TodoItemsPersonalDataSource>();
        //#endif

        return services;
    }

    private static WebApplicationBuilder AddServerApiHealthChecks(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        ServerApiSettings appSettings = new();
        configuration.Bind(appSettings);

        var healthChecksBuilder = builder.AddDefaultHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddHangfire(setup => setup.MinimumAvailableServers = 1)
            // These two reach a remote dependency, so they are bounded and they report Degraded rather than Unhealthy.
            // `/health` is the readiness contract (See MapAppHealthChecks), and Degraded keeps it at 200: an object
            // storage hiccup or an SMS provider outage must not pull every otherwise healthy instance out of the load
            // balancer rotation. The status is still visible in `/healthz`.
            .AddCheck<UserProfileImagesStorageHealthCheck>("userProfileImages", failureStatus: HealthStatus.Degraded, timeout: TimeSpan.FromSeconds(5))
            .AddCheck<TwilioHealthCheck>("sms", failureStatus: HealthStatus.Degraded, timeout: TimeSpan.FromSeconds(5));

        //#if (cloudflare == true)
        // Cloudflare Cache Purge API
        if (appSettings.Cloudflare?.Configured is true)
        {
            var cloudflareApiToken = appSettings.Cloudflare.ApiToken;
            healthChecksBuilder.AddUrlGroup(
                appSettings.Cloudflare.ZoneIds.Select(zoneId => new Uri($"https://api.cloudflare.com/client/v4/zones/{zoneId}")),
                name: "cloudflare",
                tags: [],
                configureClient: (_, client) =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {cloudflareApiToken}");
                });
        }
        //#endif

        var keycloakBaseUrl = configuration["KEYCLOAK_HTTP"] ?? configuration["Authentication:Keycloak:KeycloakUrl"];
        if (string.IsNullOrWhiteSpace(keycloakBaseUrl) is false)
        {
            var realm = configuration["Authentication:Keycloak:Realm"] ?? "dev";
            healthChecksBuilder.AddUrlGroup(
                new Uri($"{keycloakBaseUrl.TrimEnd('/')}/realms/{realm}/.well-known/openid-configuration"),
                name: "keycloakIdentity",
                tags: [],
                configureClient: (_, client) => client.Timeout = TimeSpan.FromSeconds(10));
        }

        return builder;
    }

    /// <summary>
    /// Configures global ImageMagick resource limits to prevent denial-of-service (DoS) attacks from untrusted image uploads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ImageMagick defaults to machine-derived resource caps (e.g., using physical RAM, 2x memory map, 4x memory area, and unlimited disk space).
    /// Malicious or unusually large uploads (e.g., a small 6MB PNG declaring 30,000x30,000 pixel dimensions) can force Magick.NET to 
    /// allocate gigabytes of pixel data in memory or exhaust temporary disk space.
    /// </para>
    /// <para>
    /// Because Magick.NET-Q16 allocates 2 bytes per channel, explicit bounds are applied to width, height, area, memory, and disk usage.
    /// </para>
    /// </remarks>
    private static void ConfigureImageMagickResourceLimits()
    {
        ResourceLimits.Memory = 256 * 1024 * 1024;    // 256 MB pixel cache before spilling
        ResourceLimits.Disk = 1024 * 1024 * 1024;     // 1 GB, instead of MagickResourceInfinity
        ResourceLimits.Width = 16384;
        ResourceLimits.Height = 16384;
        ResourceLimits.Area = 16384 * 16384;
    }
}
