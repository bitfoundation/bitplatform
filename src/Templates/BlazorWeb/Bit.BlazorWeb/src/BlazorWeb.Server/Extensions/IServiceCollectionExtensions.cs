using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using BlazorWeb.Server;
using BlazorWeb.Server.Models.Identity;
using BlazorWeb.Client.Services;
using Microsoft.JSInterop;
using BlazorWeb.Client.Services.HttpMessageHandlers;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography.X509Certificates;
using BlazorWeb.Shared.Dtos.Identity;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddBlazor(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.AddScoped(sp =>
        {
            IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return httpClientFactory.CreateClient("BlazorHttpClient");
            // This registers HttpClient for pre rendering & blazor server only, so to use http client to call 3rd party apis and other use cases,
            // either use services.AddHttpClient("NamedHttpClient") or services.AddHttpClient<TypedHttpClient>();
        });

        // In the Pre-Rendering mode, the configured HttpClient will use the access_token provided by the cookie in the request, so the pre-rendered content would be fitting for the current user.
        services.AddHttpClient("BlazorHttpClient")
            .AddHttpMessageHandler(sp => new LocalizationDelegatingHandler())
            .AddHttpMessageHandler(sp => new AuthDelegatingHandler(sp.GetRequiredService<IAuthTokenProvider>(), sp.GetRequiredService<IJSRuntime>()))
            .AddHttpMessageHandler(sp => new RetryDelegatingHandler())
            .AddHttpMessageHandler(sp => new ExceptionDelegatingHandler())
            .ConfigurePrimaryHttpMessageHandler<HttpClientHandler>()
            .ConfigureHttpClient((sp, httpClient) =>
            {
                Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

                if (apiServerAddress!.IsAbsoluteUri is false)
                {
                    apiServerAddress = new Uri($"{sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.GetBaseUrl()}{apiServerAddress}");
                }

                httpClient.BaseAddress = apiServerAddress;
            });

        services.AddScoped<LazyAssemblyLoader>();

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
    }

    public static void AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;
        var settings = appSettings.IdentitySettings;

        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");

        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .ProtectKeysWithCertificate(new X509Certificate2(certificatePath, appSettings.IdentitySettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet));

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
            options.RefreshTokenExpiration = TimeSpan.FromSeconds(TokenResponseDto.RefreshTokenExpiresIn);

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
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "BlazorWeb.Server.xml"));
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "BlazorWeb.Shared.xml"));

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

    public static void AddHealthChecks(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks is false)
            return;

        services.AddHealthChecksUI(setupSettings: setup =>
        {
            setup.AddHealthCheckEndpoint("WebHealthChecks", env.IsDevelopment() ? "https://localhost:5051/healthz" : "/healthz");
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
    }
}
