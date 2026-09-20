namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// One signed-in session against one deployed API: the scope it lives in, its <see cref="HttpClient"/>, and the typed
/// controllers resolved beside them. <see cref="DbContextFactory"/> and <see cref="McpClient"/> belong to the global
/// client alone, so they are null on one a test made for itself.
/// </summary>
public sealed class DeployedApiClient(
    AsyncServiceScope scope,
    HttpClient httpClient,
    IDbContextFactory<AppDbContext>? dbContextFactory = null,
    McpClient? mcpClient = null) : IAsyncDisposable
{
    /// <summary>
    /// The signed-in scope, so a typed controller resolved from it calls as this identity. Another identity or another
    /// API wants its own <see cref="DeployedApiClientProvider.CreateApiClientFor"/>.
    /// </summary>
    public IServiceProvider Services { get; } = scope.ServiceProvider;

    /// <summary>Passed in, not resolved: HttpClient is transient, so resolving would mint a second one.</summary>
    public HttpClient HttpClient { get; } = httpClient;

    /// <summary>The deployment's own database. Null unless this is the global client.</summary>
    public IDbContextFactory<AppDbContext>? DbContextFactory { get; } = dbContextFactory;

    /// <summary>Null unless this is the global client: /dev-mcp needs a global admin on a two-factor session.</summary>
    public McpClient? McpClient { get; } = mcpClient;

    /// <summary>MCP first: its transport does not own the HttpClient, which the scope disposes.</summary>
    public async ValueTask DisposeAsync()
    {
        if (McpClient is not null)
        {
            await McpClient.DisposeAsync();
        }

        await scope.DisposeAsync();
    }
}
