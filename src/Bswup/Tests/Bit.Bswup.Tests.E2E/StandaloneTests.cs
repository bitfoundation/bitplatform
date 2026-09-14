using Bit.Bswup.Tests.E2E.Infrastructure;

namespace Bit.Bswup.Tests.E2E;

/// <summary>What only a sub-path app has to get right.</summary>
[TestClass]
public class StandaloneTests : BswupTest
{
    protected override string Mode => "wasm";

    protected override string AppPath => "/standalone/";

    [TestMethod]
    public async Task A_refused_root_scope_falls_back_to_the_worker_folder()
    {
        // The default scope ("/") is wider than a worker in /standalone/ may control. The browser reports the refused
        // registration itself; Bswup's part is what happens next.
        AllowConsoleError("is not under the max scope allowed");
        await Session.SetOptionsAsync(new { scriptAttributes = new Dictionary<string, string?> { ["scope"] = null } });

        await InstallAsync();

        var scope = await Page.EvaluateAsync<string>("async () => (await navigator.serviceWorker.getRegistration()).scope");
        Assert.AreEqual($"{Session.Origin}/standalone/", scope);
        CollectionAssert.AreEqual(new[] { BucketName((await Session.ManifestAsync(AppPath)).Version) }, await BswupCacheNamesAsync());
    }

    [TestMethod]
    public async Task The_worker_script_and_manifest_resolve_relative_to_the_sub_path()
    {
        await InstallAsync();

        var requests = await Session.RequestsAsync();
        Assert.IsTrue(requests.Any(r => r.Path == "/standalone/service-worker-assets.js" && r.Status == 200), "The manifest was not requested next to the worker.");
        Assert.IsFalse(requests.Any(r => r.Path == "/service-worker.js" || r.Path == "/service-worker-assets.js"), "The sub-path app requested root worker files.");
    }
}
