//+:cnd:noEmit
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Tests.Features.Culture;

/// <summary>
/// A culture change used to be applied by reloading the host: <c>forceLoad: true</c> on every platform, plus an
/// <c>Application.Restart()</c> on the windows head. That reload existed for one reason - it rebuilds the root
/// component tree, and a fresh tree re-reads <c>IStringLocalizer</c> and every culture dependent format. On blazor
/// hybrid the rebuild can be done in place instead, so <c>Routes</c> keys its <c>LayoutView</c> on the current
/// culture and <c>CultureService</c> no longer forces a load there.
/// <para>
/// This test guards the replacement, because losing it fails <b>silently</b>: the app keeps running, the culture
/// is stored and <c>CultureInfo.CurrentUICulture</c> is updated, and only the already-rendered UI keeps the old
/// language until something else happens to re-render it. Nothing throws and no test that merely checks
/// <c>ChangeCulture</c> would notice.
/// </para>
/// <para>
/// It asserts the mechanism (the subtree is torn down and rebuilt) rather than translated text, so it does not
/// depend on which strings happen to be localized. <c>Routes</c>' <c>Layout</c> parameter is the seam: a probe
/// layout counts how many times it is constructed.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), DoNotParallelize]
public class CultureSwitchRebuildTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task CultureChange_Should_RebuildTheComponentTreeWithoutReloadingTheHost()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        ProbeLayout.ResetInstantiations();

        var cut = ctx.Render<Routes>(parameters => parameters.Add(p => p.Layout, typeof(ProbeLayout)));

        Assert.AreEqual(1, ProbeLayout.Instantiations,
            "Routes did not render its Layout once on first render, so the rest of this test cannot mean anything.");

        var pubSubService = ctx.Services.GetRequiredService<PubSubService>();

        Task PublishCulture(string culture) =>
            cut.InvokeAsync(() => pubSubService.Publish(ClientAppMessages.CULTURE_CHANGED, culture)); // exactly what CultureService.ChangeCulture publishes on the blazor hybrid branch

        // Every expected count below is absolute, and every wait is on a value rather than a duration. Two earlier
        // versions of this test were wrong in exactly the ways this avoids: one hard-coded the target culture and so
        // depended on whichever culture happened to be ambient (the renderer does not initialize under the test
        // thread's culture), and one snapshotted a baseline after a fixed 100ms delay, which lost a race under load
        // and reported the fix as broken when it was not.

        // Routes reads CultureInfo.CurrentUICulture on init, and nothing here controls what that is. Publishing a
        // value no culture can equal makes the first transition unconditional, so the key is known from here on.
        await PublishCulture("zz-Sentinel");

        await cut.WaitForAssertionAsync(() => Assert.AreEqual(2, ProbeLayout.Instantiations,
            "Publishing CULTURE_CHANGED did not rebuild the layout subtree. Routes keys its LayoutView on the " +
            "current culture precisely so a culture change tears the tree down and builds a fresh one - that " +
            "rebuild is the only thing the old forceLoad reload was doing for localization. Without it the app " +
            "keeps showing the previous language until an unrelated render happens to refresh it, and nothing " +
            "reports an error. Check the @key on LayoutView in Routes.razor and the CULTURE_CHANGED subscription " +
            "in Routes.razor.cs."), timeout: TimeSpan.FromSeconds(10));

        // Now the same transition with a real culture, from a known starting key.
        await PublishCulture("fa-IR");

        await cut.WaitForAssertionAsync(() => Assert.AreEqual(3, ProbeLayout.Instantiations,
            "Switching to a real culture (fa-IR) did not rebuild the layout subtree."), timeout: TimeSpan.FromSeconds(10));

        // The SAME culture again must not churn the tree: the key is the culture name, so an unchanged culture is an
        // unchanged key. Without this the settings page would rebuild the world on every save.
        await PublishCulture("fa-IR");

        await Task.Delay(200, TestContext.CancellationToken);

        Assert.AreEqual(3, ProbeLayout.Instantiations,
            "Publishing CULTURE_CHANGED with the culture that is already current rebuilt the tree again. The @key " +
            "is the culture name, so an unchanged culture must leave it alone.");
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
