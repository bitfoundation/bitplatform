using Bit.Belemetry;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools.Tests;

public sealed class SupportChild : ComponentBase
{
    [Inject] private ILogger<SupportChild> Logger { get; set; } = default!;

    protected override void OnInitialized()
    {
        try
        {
            Fail();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "The child failed");
        }
    }

    private static void Fail() => throw new InvalidOperationException("The child broke for order 10423");
}

public sealed class SupportParent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<SupportChild>(0);
        builder.CloseComponent();
    }
}

/// <summary>
/// bit Belemetry through BevTools: the Support panel, the jump from a failure to its component in the tree, and
/// BevTools' component tree in Belemetry's captures.
/// </summary>
[TestClass]
public class BelemetryIntegrationTests : BunitContext
{
    public BelemetryIntegrationTests()
    {
        Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
        Services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "bevtools-tests";
            o.ServiceVersion = "1.0.0";
            o.UseEnvironmentVariables = false;
            o.Consent.Default = BelemetryFeatures.All;
            o.Consent.Persist = false;
            o.Persistence.Enabled = false;
            o.Diagnostics.Enabled = true;
        });
        Services.AddBitBevToolsBelemetry(o => o.Enabled = true);
        this.UseRenderer("Server");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private IRenderedComponent<SupportParent> RenderFailingTree()
    {
        // The logger factory starts Belemetry's pipeline, as a host's does.
        Services.GetRequiredService<ILoggerFactory>();
        return Render<SupportParent>();
    }

    [TestMethod]
    public void The_support_panel_is_contributed()
    {
        var registry = Services.GetRequiredService<BevToolsRegistry>();
        Assert.Contains(p => p.Id == BevToolsBelemetryExtension.PanelId && p.ComponentType == typeof(BevBelemetryPanel), registry.Panels);
        Assert.IsEmpty(registry.ExtensionErrors);
    }

    [TestMethod]
    public async Task A_capture_gets_the_component_tree_BevTools_observed()
    {
        RenderFailingTree();

        var capture = await Services.GetRequiredService<IBelemetryDiagnostics>().CaptureAsync(new() { IncludeBrowserEvidence = false });

        var child = capture.Components.Single(c => c.Type == typeof(SupportChild).FullName);
        Assert.AreEqual("stack", child.Association, "Named by the exception's first application frame.");
        Assert.AreEqual("BevTools", child.Source);
        CollectionAssert.Contains(child.Ancestors!.ToArray(), typeof(SupportParent).FullName);
        Assert.IsGreaterThanOrEqualTo(1, child.RenderCount!.Value);
        Assert.IsFalse(capture.MissingEvidence.Any(m => m.Contains("hierarchy is unavailable")), "BevTools provided it.");
    }

    [TestMethod]
    public async Task Without_a_live_instance_the_provider_says_nothing()
    {
        var provider = Services.GetServices<IBelemetryComponentContextProvider>().OfType<BevBelemetryComponentContext>().Single();
        var described = await provider.DescribeAsync(new BelemetryComponentContextRequest { ComponentTypes = ["Not.A.Component"] }, CancellationToken.None);
        Assert.IsEmpty(described);
    }

    [TestMethod]
    public void A_failures_component_opens_the_component_tree_at_it()
    {
        RenderFailingTree();
        var cut = Render<BevToolsPanel>();
        cut.Find(".bev-badge").Click();
        cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Support", StringComparison.Ordinal)).Click();

        Assert.Contains("System.InvalidOperationException", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", cut.Markup, StringComparison.Ordinal);

        cut.Find(".bev-belemetry-show-component").Click();

        var child = Session.Components.All.Single(r => r.Type == typeof(SupportChild) && r.IsDisposed is false);
        Assert.AreEqual(child.InstanceId, Session.Ui.SelectedComponentId);
        Assert.AreEqual("components", Session.Ui.ActivePanelId);
    }

    [TestMethod]
    public void The_panel_captures_and_previews_a_bundle()
    {
        RenderFailingTree();
        var cut = Render<BevToolsPanel>();
        cut.Find(".bev-badge").Click();
        cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Support", StringComparison.Ordinal)).Click();

        cut.Find("#bev-belemetry-capture").Click();

        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll(".bit-belemetry-preview")));
        Assert.Contains("InvalidOperationException in SupportChild", cut.Find(".bit-belemetry-preview-title").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BevTools_own_UI_events_stay_off_Belemetrys_timeline()
    {
        RenderFailingTree();
        var cut = Render<BevToolsPanel>();
        cut.Find(".bev-badge").Click();
        cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Support", StringComparison.Ordinal)).Click();
        cut.Find("#bev-belemetry-capture").Click();
        cut.WaitForAssertion(() => Assert.HasCount(1, cut.FindAll(".bit-belemetry-preview")));

        var filter = Services.GetServices<IBelemetryDiagnosticComponentFilter>().Single();
        Assert.IsTrue(filter.IsExcluded(typeof(BevBelemetryPanel).FullName!));
        Assert.IsTrue(filter.IsExcluded(typeof(BevToolsPanel).FullName!));
        Assert.IsFalse(filter.IsExcluded(typeof(SupportChild).FullName!), "Exact namespace: an application under Bit.BevTools.* is the application.");
        Assert.IsFalse(Services.GetRequiredService<IBelemetryDiagnostics>().GetEvents().Any(e => e.ComponentType is { } type && filter.IsExcluded(type)));
    }
}

/// <summary>The Support panel under <see cref="BevValueAccess.None"/>: BevTools' own rule applies to Belemetry's values too.</summary>
[TestClass]
public class BelemetryIntegrationValueAccessTests : BunitContext
{
    public BelemetryIntegrationValueAccessTests()
    {
        Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
        Services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "bevtools-tests";
            o.UseEnvironmentVariables = false;
            o.Consent.Default = BelemetryFeatures.All;
            o.Consent.Persist = false;
            o.Persistence.Enabled = false;
            o.Diagnostics.Enabled = true;
        });
        Services.AddBitBevToolsBelemetry(o =>
        {
            o.Enabled = true;
            o.ValueAccess = BevValueAccess.None;
        });
        this.UseRenderer("Server");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void Under_value_access_None_the_panel_shows_no_message_and_captures_no_bundle()
    {
        // Belemetry sanitized the message; BevTools still keeps every application value out of its panels under None.
        Services.GetRequiredService<ILoggerFactory>();
        Render<SupportParent>();
        var cut = Render<BevToolsPanel>();
        cut.Find(".bev-badge").Click();
        cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Support", StringComparison.Ordinal)).Click();

        Assert.Contains("System.InvalidOperationException", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("The child broke", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(BevRedactor.HiddenValue, cut.Markup, StringComparison.Ordinal);
        Assert.IsTrue(cut.Find("#bev-belemetry-capture").HasAttribute("disabled"));
    }
}
