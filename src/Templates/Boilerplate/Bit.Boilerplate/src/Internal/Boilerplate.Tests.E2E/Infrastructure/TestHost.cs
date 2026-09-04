using Npgsql;
using Boilerplate.Client.Core.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The generic host these tests reach a deployed app's backend through: configuration, logging, and a service provider
/// carrying <see cref="AppDbContext"/> - the very database a deployment runs on - next to the client app's own typed
/// http controllers. So a test calls the API the way the app does, then checks what it wrote.
/// <para>
/// The connection string lives in this project's user secrets locally (<c>dotnet user-secrets set
/// "ConnectionStrings:postgresdb" "..."</c> in this project's directory); on CI the environment variable
/// (<c>ConnectionStrings__postgresdb</c>) overrides it.
/// </para>
/// <para>One host per run - it owns an Npgsql data source and a built EF model - and one scope per unit of work.</para>
/// </summary>
public static class TestHost
{
    private static readonly Lazy<IHost> host = new(Build, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IServiceProvider Services => host.Value.Services;

    /// <summary>
    /// A scope whose <see cref="HttpClient"/> - and with it every typed controller resolved from that scope - talks to
    /// <paramref name="apiAddress"/>. The registration leaves BaseAddress unset because which API to reach is the
    /// test's choice; <see cref="DeployedApps.ApiOf"/> maps an <see cref="App"/> to its own.
    /// </summary>
    public static AsyncServiceScope CreateScope(string apiAddress)
    {
        var scope = Services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<HttpClient>().BaseAddress = new Uri(apiAddress);

        return scope;
    }

    /// <summary>Releases the pooled connections to the deployment's database; called from MSTestSettings.</summary>
    public static void Shutdown()
    {
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

        // Development already implies both sources; explicit so a run with DOTNET_ENVIRONMENT set keeps the secrets.
        builder.Configuration.AddUserSecrets(typeof(TestHost).Assembly, optional: true);
        builder.Configuration.AddEnvironmentVariables();

        var connectionString = builder.Configuration.GetConnectionString("postgresdb")
            ?? throw new InvalidOperationException("Connection string 'postgresdb' was found in neither this project's user secrets nor the environment variables.");

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

        // No BaseAddress: each app in DeployedApps has its own API, and CreateScope aims the client at one of them.
        builder.Services.AddScoped(_ => new HttpClient(new ThrowOnApiErrorHandler
        {
            InnerHandler = new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All }
        }));

        // What the generated typed controllers ask for. The app's own handler chain (auth, retry, caching, exception
        // translation) is left out - it needs half the client container - so a test that calls an authorized endpoint
        // sets the Authorization header itself.
        builder.Services.TryAddSingleton(_ =>
        {
            JsonSerializerOptions options = new(AppJsonContext.Default.Options);

            options.TypeInfoResolverChain.Add(IdentityJsonContext.Default);

            return options;
        });
        builder.Services.AddTransient<IPrerenderStateService, NoOpPrerenderStateService>();
        builder.Services.AddTypedHttpClients();

        return builder.Build();
    }
}
