#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Mvc.Testing;

namespace ButilTests.Hosting.Infrastructure;

/// <summary>
/// The Blazor Web App hosting samples (Samples/Server, Samples/Wasm and Samples/Auto), in memory, one shared factory
/// each. They target net10.0 only, so this exists on that framework only.
/// </summary>
public static class SampleHostFactories
{
    public const string Server = "server";
    public const string WebAssembly = "wasm";
    public const string Auto = "auto";

    private static readonly Lazy<WebApplicationFactory<Bit.Butil.Samples.Server.Components.App>> _server = new(() => new());
    private static readonly Lazy<WebApplicationFactory<Bit.Butil.Samples.Wasm.Components.App>> _webAssembly = new(() => new());
    private static readonly Lazy<WebApplicationFactory<Bit.Butil.Samples.Auto.Components.App>> _auto = new(() => new());

    public static HttpClient CreateClient(string sample) => sample switch
    {
        Server => _server.Value.CreateClient(),
        WebAssembly => _webAssembly.Value.CreateClient(),
        Auto => _auto.Value.CreateClient(),
        _ => throw new ArgumentOutOfRangeException(nameof(sample), sample, null)
    };

    public static async Task DisposeAsync()
    {
        if (_server.IsValueCreated) await _server.Value.DisposeAsync();
        if (_webAssembly.IsValueCreated) await _webAssembly.Value.DisposeAsync();
        if (_auto.IsValueCreated) await _auto.Value.DisposeAsync();
    }
}
#endif
