#if NET11_0_OR_GREATER
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bit.Brouter.Tests.Hosting;

internal static class SampleHostFactories
{
    public static readonly Lazy<WebApplicationFactory<Bit.Brouter.Samples.Server.Components.App>> Server = new(() => new());
    public static readonly Lazy<WebApplicationFactory<Bit.Brouter.Samples.Wasm.Components.App>> WebAssembly = new(() => new());
    public static readonly Lazy<WebApplicationFactory<Bit.Brouter.Samples.Auto.Components.App>> Auto = new(() => new());

    public static HttpClient CreateClient(string sample) => sample switch
    {
        "server" => Server.Value.CreateClient(),
        "wasm" => WebAssembly.Value.CreateClient(),
        "auto" => Auto.Value.CreateClient(),
        _ => throw new ArgumentOutOfRangeException(nameof(sample), sample, null)
    };

    public static async Task DisposeAsync()
    {
        if (Server.IsValueCreated) await Server.Value.DisposeAsync();
        if (WebAssembly.IsValueCreated) await WebAssembly.Value.DisposeAsync();
        if (Auto.IsValueCreated) await Auto.Value.DisposeAsync();
    }
}

/// <summary>
/// The hosting samples are what the README and the MCP setup guides hand to people as the way to wire
/// Brouter into each render mode, so they are booted as they ship - their own Program.cs and App.razor -
/// rather than trusted to keep matching the harness.
/// </summary>
[TestClass]
public class SamplesHostingTests
{
    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_home_page_is_prerendered(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "<h1>Brouter samples</h1>");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task A_typed_route_parameter_is_bound_in_the_prerendered_page(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/counter/1234");

        var html = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(html, "<h1>Counter</h1>");
        StringAssert.Contains(html, "<dd>1234</dd>");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task An_unmatched_url_ends_on_the_samples_not_found_route(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/definitely/not/here");

        Assert.AreEqual("/404", response.RequestMessage?.RequestUri?.AbsolutePath);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "Nothing matched this address.");
    }

    [TestMethod]
    [DataRow("server")]
    [DataRow("wasm")]
    [DataRow("auto")]
    public async Task The_script_module_is_served(string sample)
    {
        using var client = SampleHostFactories.CreateClient(sample);
        using var response = await client.GetAsync("/_content/Bit.Brouter/bit-brouter.js");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/javascript", response.Content.Headers.ContentType?.MediaType);
    }
}
#endif
