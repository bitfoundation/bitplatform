//+:cnd:noEmit
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Tests.Features.PubSub;

/// <summary>
/// A culture change used to be applied by reloading the host, for one reason: the reload rebuilds the root component
/// tree, and a fresh tree re-reads <c>IStringLocalizer</c> and every culture dependent format. The same turned out to
/// be true of a time zone change and of a tenant switch, so the rebuild became a message of its own:
/// <c>ClientAppMessages.SOFT_RESTART</c>. <c>Routes</c> is its only subscriber - it keys the <c>AppErrorBoundary</c>
/// wrapping its <c>LayoutView</c> on a counter the message increments, which rebuilds the tree without restarting
/// the .NET or blazor webassembly runtime under it.
/// <para>
/// This test guards that mechanism, because losing it fails <b>silently</b>: the app keeps running, the culture (or
/// zone, or tenant) is stored and applied, and only the already-rendered UI keeps showing the previous one until
/// something else happens to re-render it. Nothing throws, and no test that merely checks <c>ChangeCulture</c> would
/// notice.
/// </para>
/// <para>
/// It asserts the mechanism (the subtree is torn down and rebuilt) rather than translated text, so it does not
/// depend on which strings happen to be localized. <c>Routes</c>' <c>Layout</c> parameter is the seam: a probe
/// layout counts how many times it is constructed.
/// </para>
/// <para>
/// Whether a restart is warranted at all belongs to the publisher, not here: <c>Routes</c> restarts on every message
/// it is given, and the services return early when the value picked is already in use.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), DoNotParallelize]
public class SoftRestartTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task SoftRestart_Should_RebuildTheComponentTreeWithoutReloadingTheHost()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        ProbeLayout.ResetInstantiations();

        var cut = ctx.Render<Routes>(parameters => parameters.Add(p => p.Layout, typeof(ProbeLayout)));

        Assert.AreEqual(1, ProbeLayout.Instantiations,
            "Routes did not render its Layout once on first render, so the rest of this test cannot mean anything.");

        var pubSubService = ctx.Services.GetRequiredService<PubSubService>();

        Task PublishSoftRestart() =>
            cut.InvokeAsync(() => pubSubService.Publish(ClientAppMessages.SOFT_RESTART)); // exactly what CultureService, TimeZoneService and AppMenu publish

        // Every expected count below is absolute, and every wait is on a value rather than a duration. Two earlier
        // versions of this test were wrong in exactly the ways this avoids: one hard-coded the target culture and so
        // depended on whichever culture happened to be ambient (the renderer does not initialize under the test
        // thread's culture), and one snapshotted a baseline after a fixed 100ms delay, which lost a race under load
        // and reported the fix as broken when it was not.

        await PublishSoftRestart();

        await cut.WaitForAssertionAsync(() => Assert.AreEqual(2, ProbeLayout.Instantiations,
            "Publishing SOFT_RESTART did not rebuild the layout subtree. Routes keys the AppErrorBoundary wrapping " +
            "its LayoutView on a counter this message increments, precisely so that a culture, time zone or tenant " +
            "change tears the tree down and builds a fresh one - that rebuild is the only thing the old forceLoad " +
            "reload was doing for localization. Without it the app keeps showing the previous language/zone/tenant " +
            "until an unrelated render happens to refresh it, and nothing reports an error. Check the @key on " +
            "AppErrorBoundary in Routes.razor and the SOFT_RESTART subscription in Routes.razor.cs."), timeout: TimeSpan.FromSeconds(10));

        // The key is a counter, not the value behind the restart, so a second restart rebuilds again. A key carrying
        // the value (as this one used to carry the culture name) does nothing when two changes produce the same key.
        await PublishSoftRestart();

        await cut.WaitForAssertionAsync(() => Assert.AreEqual(3, ProbeLayout.Instantiations,
            "A second SOFT_RESTART did not rebuild the layout subtree again. The @key must change on every message."), timeout: TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Stands in for <c>MainLayout</c>. Counting constructions is what makes "the subtree was rebuilt" observable -
    /// asserting on rendered markup instead would pass for a plain re-render, which is the thing this test has to
    /// tell apart from a genuine teardown.
    /// </summary>
    private sealed class ProbeLayout : LayoutComponentBase
    {
        private static int instantiations;

        public static int Instantiations => Volatile.Read(ref instantiations);

        public static void ResetInstantiations() => Volatile.Write(ref instantiations, 0);

        public ProbeLayout() => Interlocked.Increment(ref instantiations);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }
}
