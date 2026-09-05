namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// The run-long AdminPanel session <see cref="TestHost"/> keeps until shutdown: the global admin's
/// <see cref="HttpClient"/> (the same handler-chain factory every test scope uses), that scope's
/// <see cref="AppDbContext"/>, and an <see cref="McpClient"/> aimed at <c>/dev-mcp</c>.
/// </summary>
public sealed class TestBackend(HttpClient httpClient, AppDbContext dbContext, McpClient mcp) : IAsyncDisposable
{
    public HttpClient HttpClient { get; } = httpClient;

    public AppDbContext DbContext { get; } = dbContext;

    public McpClient McpClient { get; } = mcp;

    public async ValueTask DisposeAsync() => await McpClient.DisposeAsync();
}
