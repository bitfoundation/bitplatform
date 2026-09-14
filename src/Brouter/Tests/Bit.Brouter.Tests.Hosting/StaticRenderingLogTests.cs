using Bit.Brouter.Tests.Hosting.Infrastructure;
using Microsoft.Extensions.Logging;
using Modes = Bit.Brouter.Tests.Harness.Web.HarnessRenderModes;

namespace Bit.Brouter.Tests.Hosting;

/// <summary>
/// What the server logs while rendering every harness route statically. Brouter logs the JS interop
/// failures it swallows at Debug, so a static render that tries interop - which never works there and
/// only costs an exception per call - shows up here even though the page renders fine.
/// </summary>
[TestClass]
public class StaticRenderingLogTests
{
    private static readonly string[] Routes =
    [
        "/", "/about", "/items/42", "/deep/a/b/c/leaf", "/data", "/guarded", "/denied", "/old-about", "/nope",
        "/broken", "/missing/0", "/long", "/other", "/preload", "/leave", "/history/1", "/keepalive", "/confirm",
    ];

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task Rendering_every_route_logs_no_errors_and_attempts_no_js_interop(string mode)
    {
        await using var factory = new HarnessHostFactory(mode);
        using var client = factory.CreateNonRedirectingClient();

        foreach (var route in Routes)
        {
            using var _ = await client.GetAsync(route);
        }

        var errors = factory.Logs.Entries.Where(e => e.Level >= LogLevel.Error).ToArray();
        Assert.AreEqual(0, errors.Length, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));

        var interop = factory.Logs.Entries.Where(e => e.Category.StartsWith("Bit.Brouter", StringComparison.Ordinal) && e.Message.Contains("interop", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.AreEqual(0, interop.Length, string.Join(Environment.NewLine, interop.Select(e => e.ToString())));
    }
}
