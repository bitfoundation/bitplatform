using Npgsql;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using ModelContextProtocol.Client;
using Boilerplate.Client.Web.Infrastructure.Services;
using Boilerplate.Shared.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The generic host these tests reach a deployed app's backend through. One host per run, two kinds of scope:
/// <list type="bullet">
/// <item>
/// A long-lived AdminPanel session, kept until <see cref="Shutdown"/>, that holds the global admin's
/// <see cref="HttpClient"/>, <see cref="AppDbContext"/> and <see cref="McpClient"/>. See <see cref="GetBackend"/>.
/// </item>
/// <item>
/// A per-test scope from <see cref="CreateScope"/>: the same <see cref="TestStorageService"/> / handler chain
/// Boilerplate.Tests uses, except the <see cref="HttpClient"/>'s base address is a <see cref="DeployedApps"/> API
/// rather than an in-process test server. Typed controllers such as <see cref="IIdentityController"/> resolve from it,
/// so a test can sign in as whoever it needs against whichever API it needs.
/// </item>
/// </list>
/// The connection string lives in this project's user secrets locally (<c>dotnet user-secrets set
/// "ConnectionStrings:postgresdb" "..."</c> in this project's directory); on CI the environment variable
/// (<c>ConnectionStrings__postgresdb</c>) overrides it.
/// </summary>
public static class TestHost
{
    private static readonly Lazy<IHost> host = new(Build, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly SemaphoreSlim backendGate = new(1, 1);
    private static AsyncServiceScope? backendScope;
    private static TestBackend? backend;

    public static IServiceProvider Services => host.Value.Services;

    /// <summary>
    /// The run-long AdminPanel session. First caller signs in as the configured global admin and keeps the resulting
    /// MCP client. Later callers share it.
    /// </summary>
    public static async Task<TestBackend> GetBackend(CancellationToken cancellationToken)
    {
        if (backend is not null)
            return backend;

        await backendGate.WaitAsync(cancellationToken);
        try
        {
            if (backend is not null)
                return backend;

            backend = await ConnectBackend();
            return backend;
        }
        finally
        {
            backendGate.Release();
        }
    }

    /// <summary>
    /// A scope whose <see cref="HttpClient"/> - and with it every typed controller resolved from that scope - talks to
    /// <paramref name="apiAddress"/>. <see cref="DeployedApps.ApiOf"/> maps an <see cref="App"/> to its own.
    /// </summary>
    public static AsyncServiceScope CreateScope(string apiAddress)
    {
        var scope = Services.CreateAsyncScope();
        Apply(scope.ServiceProvider.GetRequiredService<DeployedApi>(), apiAddress);
        return scope;
    }

    /// <summary>Releases the MCP session and the pooled connections to the deployment's database.</summary>
    public static void Shutdown()
    {
        if (backend is not null)
        {
            backend.DisposeAsync().AsTask().GetAwaiter().GetResult();
            backend = null;
        }

        if (backendScope is not null)
        {
            backendScope.Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
            backendScope = null;
        }

        if (host.IsValueCreated)
            host.Value.Dispose();
    }

    private static IHost Build()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(TestHost).Assembly.GetName().Name
        });

        AppEnvironment.Set(Environments.Development);

        // AddClientConfigurations reflects over these assemblies.
        _ = typeof(Boilerplate.Client.Core.ClientCoreSettings).Assembly;
        _ = typeof(Boilerplate.Client.Web.Program).Assembly;

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");
        // Development already implies both sources; explicit so a run with DOTNET_ENVIRONMENT set keeps the secrets.
        builder.Configuration.AddUserSecrets(typeof(TestHost).Assembly, optional: true);
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServerAddress"] = DeployedApps.AdminPanelApi
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
                BaseAddress = deployed.Address
            };
            httpClient.DefaultRequestHeaders.Add("X-Origin", deployed.Origin);
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

        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
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

    private static async Task<TestBackend> ConnectBackend()
    {
        var scope = Services.CreateAsyncScope();
        McpClient? mcp = null;
        try
        {
            var sp = scope.ServiceProvider;
            Apply(sp.GetRequiredService<DeployedApi>(), DeployedApps.AdminPanelApi);

            var configuration = sp.GetRequiredService<IConfiguration>();
            var email = configuration["GlobalAdminEmail"]
                ?? throw new InvalidOperationException("User secrets / env are missing GlobalAdminEmail.");
            var password = configuration["GlobalAdminPassword"]
                ?? throw new InvalidOperationException("User secrets / env are missing GlobalAdminPassword.");

            var db = sp.GetRequiredService<AppDbContext>();
            var admin = await db.Users.IgnoreQueryFilters()
                .SingleOrDefaultAsync(user => user.NormalizedEmail == email.ToUpperInvariant());
            if (admin is null)
                throw new InvalidOperationException($"GlobalAdminEmail '{email}' is not in the deployment's database.");

            // The live account has to match the secrets this host signs in with: confirmed, unlocked, password hash
            // equal to GlobalAdminPassword, and holding g-admin so /dev-mcp authorizes AppFeatures.System.DevMcp.
            await EnsureUserCanSignIn(db, admin.Id, password, grantGlobalAdmin: true);

            var authManager = sp.GetRequiredService<AuthManager>();
            var requiresTwoFactor = await authManager.SignIn(new() { Email = email, Password = password, RememberMe = true }, CancellationToken.None);
            if (requiresTwoFactor)
                throw new InvalidOperationException("GlobalAdminEmail is not expected to require two-factor authentication.");

            var httpClient = sp.GetRequiredService<HttpClient>();
            mcp = await ConnectMcp(httpClient, sp.GetRequiredService<ILoggerFactory>());

            backendScope = scope;
            return new TestBackend(httpClient, db, mcp);
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
    /// Aligns a live user with the password in secrets so a test can sign in: confirmed, unlocked, no 2FA, hash
    /// matching <paramref name="password"/>. Optionally grants <see cref="AppRoles.GlobalAdmin"/>.
    /// </summary>
    public static async Task EnsureUserCanSignIn(AppDbContext db, Guid userId, string password, bool grantGlobalAdmin = false)
    {
        // The backend DbContext lives for the whole run; a previous attempt or a live SignIn can have
        // changed ConcurrencyStamp, so never mutate a tracked instance from an earlier query.
        db.ChangeTracker.Clear();
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(item => item.Id == userId);

        user.EmailConfirmed = true;
        user.TwoFactorEnabled = false;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

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
        deployed.Address = address;
        deployed.Origin = origin;
    }

    /// <summary>
    /// Innermost so it runs after <c>RequestHeadersDelegatingHandler</c> wrote <c>X-App-Version</c>. This host is not
    /// a shipped client, so ForceUpdate must not apply: the middleware only runs when both version and platform headers
    /// are present.
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
