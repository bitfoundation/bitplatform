using OtpNet;
using Npgsql;
using Microsoft.JSInterop;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using System.ClientModel.Primitives;
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
    /// The model <see cref="AiAnswerJudge"/> reads a chatbot answer with, or null when this project's user secrets
    /// name no <c>OpenAIChatApiKey</c> (See <see cref="AddAnswerJudge"/>).
    /// </summary>
    public static IChatClient? GetAnswerJudge() => host.Value.Services.GetService<IChatClient>();

    /// <summary>
    /// A client of the caller's own, talking to <paramref name="apiAddress"/> - signed out, with no Dev MCP and no
    /// database. <see cref="DeployedApps.ApiOf"/> maps an <see cref="App"/> to its API.
    /// </summary>
    public static DeployedApiClient CreateApiClientFor(string apiAddress)
    {
        var scope = host.Value.Services.CreateAsyncScope();
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

        var services = builder.Services;
        var configuration = builder.Configuration;

        // AddClientConfigurations reflects over these assemblies.
        _ = typeof(Client.Core.ClientCoreSettings).Assembly;
        _ = typeof(Client.Web.Program).Assembly;

        configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");
        // Development already implies both sources; explicit so a run with DOTNET_ENVIRONMENT set keeps the secrets.
        configuration.AddUserSecrets(typeof(DeployedApiClientProvider).Assembly, optional: true);
        configuration.AddEnvironmentVariables();
        // Required by ClientCoreSettings, and deliberately relative: an absolute value would pin
        // AbsoluteServerAddressProvider to one API for every scope instead of letting it follow the scope's HttpClient.
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServerAddress"] = "/"
        });

        var connectionString = configuration.GetRequiredConnectionString("postgresdb");

        services.AddClientCoreProjectServices(configuration);
        services.AddIntegrationApiOnlyTestsServices();
        services.AddSingleton<IJSRuntime, TestJsRuntime>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddScoped<IBitDeviceCoordinator, WebDeviceCoordinator>();
        services.AddScoped<ClientExceptionHandlerBase, TestClientExceptionHandler>();
        services.AddScoped<SharedExceptionHandler>(sp => sp.GetRequiredService<ClientExceptionHandlerBase>());
        services.AddScoped<DeployedApi>();

        services.AddScoped<HttpClient>(sp =>
        {
            var deployed = sp.GetRequiredService<DeployedApi>();
            var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
            var httpClient = new HttpClient(handlerFactory.Invoke())
            {
                BaseAddress = deployed.ApiAddress
            };

            httpClient.DefaultRequestHeaders.Add("X-App-Version", "400.0.0");
            httpClient.DefaultRequestHeaders.Add("X-App-Platform", AppPlatformType.Web.ToString());
            httpClient.DefaultRequestHeaders.Add("X-Origin", deployed.WebAppOrigin);

            return httpClient;
        });

        AddAnswerJudge(services, configuration);

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

    /// <summary>
    /// The model these tests read the deployed chatbot's answers with (See <see cref="AiAnswerJudge"/>).
    /// <para>
    /// The same registration Server.Api makes, minus what only the deployment needs: no agents, system prompts,
    /// embeddings or tools - this model only reads what the chatbot wrote. Its own key too (<c>OpenAIChatApiKey</c>
    /// in this project's user secrets), so a run costs the deployments nothing and a missing key skips those tests.
    /// </para>
    /// </summary>
    private static void AddAnswerJudge(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("AI");

        if (configuration["OpenAIChatApiKey"] is not { Length: > 0 } apiKey)
            return;

        services.AddChatClient(sp => new OpenAI.Chat.ChatClient(
            model: configuration["OpenAIChatModel"] ?? throw new InvalidOperationException("'OpenAIChatModel' is required alongside 'OpenAIChatApiKey'."),
            credential: new(apiKey),
            options: new()
            {
                Endpoint = configuration["OpenAIChatEndpoint"] is { Length: > 0 } endpoint ? new Uri(endpoint) : null,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIChatClient())
        .UseLogging();
    }

    private static async Task<DeployedApiClient> ConnectGlobalApiClient()
    {
        var scope = host.Value.Services.CreateAsyncScope();
        McpClient? mcp = null;
        try
        {
            var sp = scope.ServiceProvider;
            Apply(sp.GetRequiredService<DeployedApi>(), DeployedApps.AdminPanelApi);

            var configuration = sp.GetRequiredService<IConfiguration>();
            var email = configuration["GlobalAdminEmail"]!;
            var password = configuration["GlobalAdminPassword"]!;

            var dbContextFactory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();

            var authenticatorKey = configuration["GlobalAdminAuthenticatorKey"]!;

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
}
