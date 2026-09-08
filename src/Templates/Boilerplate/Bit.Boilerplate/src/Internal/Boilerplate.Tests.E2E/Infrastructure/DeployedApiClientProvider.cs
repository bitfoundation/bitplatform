using OtpNet;
using Npgsql;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Boilerplate.Client.Web.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The host these tests reach the deployed apps' backends through. One scope is one identity aimed at one API
/// (<see cref="CreateApiClientFor"/>); <see cref="GetGlobalApiClient"/> is the one shared across the run.
/// </summary>
public static class DeployedApiClientProvider
{
    private static readonly Lazy<IHost> host = new(Build, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly SemaphoreSlim globalApiClientGate = new(1, 1);
    private static volatile DeployedApiClient? globalApiClient;

    public static IServiceProvider Services => host.Value.Services;

    // Where Identity stores the authenticator shared key (UserStore's InternalLoginProvider / AuthenticatorKeyTokenName).
    private const string AuthenticatorLoginProvider = "[AspNetUserStore]";
    private const string AuthenticatorKeyName = "AuthenticatorKey";

    /// <summary>
    /// The run-long global-admin session. The first caller signs it in; later callers share it, so it is not theirs
    /// to dispose.
    /// </summary>
    public static async Task<DeployedApiClient> GetGlobalApiClient(CancellationToken cancellationToken)
    {
        if (globalApiClient is not null)
            return globalApiClient;

        await globalApiClientGate.WaitAsync(cancellationToken);
        try
        {
            if (globalApiClient is not null)
                return globalApiClient;

            globalApiClient = await ConnectGlobalApiClient();
            return globalApiClient;
        }
        finally
        {
            globalApiClientGate.Release();
        }
    }

    /// <summary>
    /// A client of the caller's own, talking to <paramref name="apiAddress"/> - signed out, with no Dev MCP and no
    /// database. <see cref="DeployedApps.ApiOf"/> maps an <see cref="App"/> to its API.
    /// </summary>
    public static DeployedApiClient CreateApiClientFor(string apiAddress)
    {
        var scope = Services.CreateAsyncScope();
        Apply(scope.ServiceProvider.GetRequiredService<DeployedApi>(), apiAddress);
        return new DeployedApiClient(scope, scope.ServiceProvider.GetRequiredService<HttpClient>());
    }

    /// <summary>Releases the MCP session and the pooled connections to the deployment's database.</summary>
    public static async Task ShutdownAsync()
    {
        if (globalApiClient is not null)
        {
            await globalApiClient.DisposeAsync();
            globalApiClient = null;
        }

        if (host.IsValueCreated)
        {
            if (host.Value is IAsyncDisposable asyncDisposableHost)
            {
                await asyncDisposableHost.DisposeAsync();
            }
            else
            {
                host.Value.Dispose();
            }
        }
    }

    private static IHost Build()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(DeployedApiClientProvider).Assembly.GetName().Name
        });

        AppEnvironment.Set(Environments.Development);

        // AddClientConfigurations reflects over these assemblies.
        _ = typeof(Boilerplate.Client.Core.ClientCoreSettings).Assembly;
        _ = typeof(Boilerplate.Client.Web.Program).Assembly;

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");
        // Development already implies both sources; explicit so a run with DOTNET_ENVIRONMENT set keeps the secrets.
        builder.Configuration.AddUserSecrets(typeof(DeployedApiClientProvider).Assembly, optional: true);
        builder.Configuration.AddEnvironmentVariables();
        // Required by ClientCoreSettings, and deliberately relative: an absolute value would pin
        // AbsoluteServerAddressProvider to one API for every scope instead of letting it follow the scope's HttpClient.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServerAddress"] = "/"
        });

        var connectionString = builder.Configuration.GetConnectionString("postgresdb")
            ?? throw new InvalidOperationException("Connection string 'postgresdb' was found in neither this project's user secrets nor the environment variables.");

        builder.Services.AddClientCoreProjectServices(builder.Configuration);
        builder.Services.AddIntegrationApiOnlyTestsServices();
        builder.Services.AddSingleton<IJSRuntime, TestJsRuntime>();
        builder.Services.AddSingleton<NavigationManager, TestNavigationManager>();
        builder.Services.AddScoped<IBitDeviceCoordinator, WebDeviceCoordinator>();
        builder.Services.AddScoped<ClientExceptionHandlerBase, TestClientExceptionHandler>();
        builder.Services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());
        builder.Services.AddScoped<DeployedApi>();

        // Same shape as Boilerplate.Tests' AddTestProjectServices, except the base address is a deployed API chosen
        // per scope rather than the in-process test server.
        builder.Services.AddTransient(sp =>
        {
            var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
            var deployed = sp.GetRequiredService<DeployedApi>();
            var httpClient = new HttpClient(handlerFactory.Invoke(new SupportedClientVersionHandler
            {
                InnerHandler = new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(15)
                }
            }))
            {
                BaseAddress = deployed.ApiAddress
            };
            httpClient.DefaultRequestHeaders.Add("X-Origin", deployed.WebAppOrigin);
            return httpClient;
        });

        // Same shape as Server.Api's own registration, so what this queries is what the deployment writes.
        builder.Services.AddSingleton(_ =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();
            dataSourceBuilder.EnableDynamicJson();
            return dataSourceBuilder.Build();
        });

        // A factory, not a scoped context: nothing here serves a request, and a test may hold one open for a journey.
        builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            options.EnableSensitiveDataLogging()
                .EnableDetailedErrors();

            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>(), dbOptions =>
            {
                dbOptions.UseVector();
                dbOptions.SetPostgresVersion(18, 0);
            });
        });

        return builder.Build();
    }

    private static async Task<DeployedApiClient> ConnectGlobalApiClient()
    {
        var scope = Services.CreateAsyncScope();
        McpClient? mcp = null;
        try
        {
            var sp = scope.ServiceProvider;
            Apply(sp.GetRequiredService<DeployedApi>(), DeployedApps.AdminPanelApi);

            var configuration = sp.GetRequiredService<IConfiguration>();
            var email = configuration["GlobalAdminEmail"]!;
            var password = configuration["GlobalAdminPassword"]!;

            var dbContextFactory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var normalizedEmail = email.ToUpperInvariant();
            var admin = await db.Users.IgnoreQueryFilters()
                .SingleAsync(user => user.NormalizedEmail == normalizedEmail);

            var authenticatorKey = configuration["GlobalAdminAuthenticatorKey"]!;

            await EnsureUserCanSignIn(db, admin.Id, password, authenticatorKey, grantGlobalAdmin: true);

            var authManager = sp.GetRequiredService<AuthManager>();
            await authManager.SignIn(new() { Email = email, Password = password, RememberMe = true }, CancellationToken.None);

            await authManager.SignIn(new()
            {
                Email = email,
                Password = password,
                RememberMe = true,
                TwoFactorCode = new Totp(Base32Encoding.ToBytes(authenticatorKey)).ComputeTotp() // 2fa
            }, CancellationToken.None);

            var httpClient = sp.GetRequiredService<HttpClient>();
            mcp = await ConnectMcp(httpClient, sp.GetRequiredService<ILoggerFactory>());

            return new DeployedApiClient(scope, httpClient, dbContextFactory, mcp);
        }
        catch
        {
            if (mcp is not null)
                await mcp.DisposeAsync();
            await scope.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Aligns a live account with the secrets: confirmed, unlocked, matching password, and on two-factor with
    /// <paramref name="authenticatorKey"/> (null turns 2FA off). A no-op once it matches.
    /// </summary>
    /// <remarks>
    /// The key is why this exists: enrolling through the UI mints a random one, so nothing else can put the account on
    /// two-factor with OURS - and /dev-mcp needs a 2FA session.
    /// </remarks>
    public static async Task EnsureUserCanSignIn(AppDbContext db, Guid userId, string password, string? authenticatorKey = null, bool grantGlobalAdmin = false)
    {
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(item => item.Id == userId);

        user.EmailConfirmed = true;
        user.TwoFactorEnabled = authenticatorKey is not null;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

        var hasher = new PasswordHasher<User>();
        if (user.PasswordHash is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, password) is PasswordVerificationResult.Failed)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
        }

        if (authenticatorKey is not null)
        {
            // Where Identity keeps the shared key; written directly because this host has no UserManager.
            var token = await db.UserTokens.SingleOrDefaultAsync(item =>
                item.UserId == userId && item.LoginProvider == AuthenticatorLoginProvider && item.Name == AuthenticatorKeyName);

            if (token is null)
            {
                await db.UserTokens.AddAsync(new UserToken
                {
                    UserId = userId,
                    LoginProvider = AuthenticatorLoginProvider,
                    Name = AuthenticatorKeyName,
                    Value = authenticatorKey
                });
            }
            else if (token.Value != authenticatorKey)
            {
                token.Value = authenticatorKey;
            }
        }

        if (grantGlobalAdmin)
        {
            var globalAdminRoleId = await db.Roles
                .Where(role => role.Name == AppRoles.GlobalAdmin)
                .Select(role => role.Id)
                .SingleAsync();

            if (await db.UserRoles.AnyAsync(userRole => userRole.UserId == user.Id && userRole.RoleId == globalAdminRoleId) is false)
                await db.UserRoles.AddAsync(new UserRole { UserId = user.Id, RoleId = globalAdminRoleId });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<McpClient> ConnectMcp(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(httpClient.BaseAddress ?? throw new InvalidOperationException("HttpClient.BaseAddress is unset."), "dev-mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        }, httpClient, loggerFactory, ownsHttpClient: false);

        return await McpClient.CreateAsync(transport);
    }

    private static void Apply(DeployedApi deployed, string apiAddress)
    {
        var (address, origin) = DeployedApi.For(apiAddress);
        deployed.ApiAddress = address;
        deployed.WebAppOrigin = origin;
    }

    /// <summary>
    /// Innermost, so it strips what RequestHeadersDelegatingHandler just wrote: this host is not a shipped client, and
    /// ForceUpdate only applies when both headers are present.
    /// </summary>
    private sealed class SupportedClientVersionHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Remove("X-App-Version");
            request.Headers.Remove("X-App-Platform");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
