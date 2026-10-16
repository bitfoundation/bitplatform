using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace Bit.BevTools.Tests;

/// <summary>
/// The BevTools UI is itself a Blazor application, and nothing else in the suite clicks its controls. A panel whose
/// buttons quietly do nothing looks exactly like a panel that works.
/// </summary>
[TestClass]
public class PanelInteractionTests : BunitContext
{
    public PanelInteractionTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private IRenderedComponent<BevToolsPanel> RenderPanel(string? prefs = null)
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true, Visibility = "visible" });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(prefs);
        module.SetupVoid("savePrefs", _ => true).SetVoidResult();
        module.Setup<BevStorageEntry[]>("getStorage", _ => true)
            .SetResult([new BevStorageEntry("demo:auth_token", 42, "«redacted»", true), new BevStorageEntry("theme", 4, "dark")]);
        module.Setup<BevBrowserSnapshot>("refresh", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        return Render<BevToolsPanel>();
    }

    [TestMethod]
    public void Every_built_in_panel_can_be_opened_from_its_tab()
    {
        var cut = RenderPanel();
        var tabs = cut.FindAll(".bev-tab").Select(t => t.TextContent.Trim().Split('\n')[0].Trim()).ToList();
        Assert.Contains("Components", tabs);

        foreach (var tab in tabs)
        {
            cut.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith(tab, StringComparison.Ordinal)).Click();
            Assert.IsNotEmpty(cut.FindAll(".bev-body"));
            Assert.DoesNotContain(e => e.TextContent.Contains("Unknown panel", StringComparison.Ordinal), cut.FindAll(".bev-empty"));
        }
    }

    [TestMethod]
    public void The_browser_panel_reads_storage_when_its_button_is_clicked()
    {
        var cut = RenderPanel();
        cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Browser", StringComparison.Ordinal)).Click();

        var button = cut.FindAll(".bev-body button").First(b => b.TextContent.Contains("localStorage", StringComparison.Ordinal));
        button.Click();

        Assert.IsNotNull(Session.Browser.LocalStorage);
        Assert.IsNull(Session.Browser.StorageError);
        Assert.Contains(e => e.Key == "demo:auth_token" && e.Redacted, Session.Browser.LocalStorage!);
        Assert.Contains("demo:auth_token", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("«redacted»", cut.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_panel_can_be_closed_and_reopened_from_the_badge()
    {
        var cut = RenderPanel();
        cut.FindAll(".bev-tools button").Last().Click();

        var badge = cut.Find(".bev-badge");
        Assert.IsEmpty(cut.FindAll(".bev-panel"));

        badge.Click();
        Assert.IsNotEmpty(cut.FindAll(".bev-panel"));
    }

    [TestMethod]
    public void An_unavailable_saved_panel_is_ignored()
    {
        var cut = RenderPanel("""{"panel":"circuit","open":true}""");

        Assert.AreEqual("components", Session.Ui.ActivePanelId);
        Assert.DoesNotContain("Unknown panel", cut.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Refresh_updates_the_open_tab_without_switching_panels()
    {
        var cut = RenderPanel();
        cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Network", StringComparison.Ordinal)).Click();
        Assert.Contains("No HTTP requests captured", cut.Markup, StringComparison.Ordinal);

        Session.Http.Add(new BevHttpRecord
        {
            Id = Session.Http.NextId(),
            StartedAt = DateTimeOffset.UtcNow,
            Method = "GET",
            Url = "https://example.test/live-refresh",
            StatusCode = 200,
        });
        Assert.DoesNotContain("live-refresh", cut.Markup, StringComparison.Ordinal);

        cut.Find("button[aria-label='Refresh captured data']").Click();
        Assert.Contains("live-refresh", cut.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Live_updates_can_be_turned_off_and_the_open_tab_stays_stale_until_refresh()
    {
        var cut = RenderPanel();
        cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Network", StringComparison.Ordinal)).Click();
        cut.Find("input[aria-label='Live updates']").Change(false);
        Assert.IsFalse(Session.Ui.LiveUpdates);

        Session.Http.Add(new BevHttpRecord
        {
            Id = Session.Http.NextId(),
            StartedAt = DateTimeOffset.UtcNow,
            Method = "GET",
            Url = "https://example.test/live-off",
            StatusCode = 204,
        });
        cut.Render();
        Assert.DoesNotContain("live-off", cut.Markup, StringComparison.Ordinal);

        cut.Find("button[aria-label='Refresh captured data']").Click();
        Assert.Contains("live-off", cut.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Live_preference_is_restored()
    {
        var cut = RenderPanel("""{"open":true,"live":false}""");
        Assert.IsFalse(Session.Ui.LiveUpdates);
        Assert.IsFalse(cut.Find("input[aria-label='Live updates']").HasAttribute("checked"));
    }

    /// <summary>
    /// The panels render framework components an application renders too — <c>Virtualize</c>, <c>CascadingValue</c>,
    /// <c>DynamicComponent</c> — so nothing about their type says they are BevTools'. Before ownership was inherited
    /// from the parent, opening a tab wrote its own renders, render batches and <c>Virtualize.init</c> interop calls
    /// into the very panels the developer was reading.
    /// </summary>
    [TestMethod]
    public void BevTools_own_ui_never_appears_as_application_activity()
    {
        var cut = RenderPanel();
        Session.Http.Add(new BevHttpRecord
        {
            Id = Session.Http.NextId(),
            StartedAt = DateTimeOffset.UtcNow,
            Method = "GET",
            Url = "https://example.test/thing",
            StatusCode = 200,
        });

        foreach (var tab in new[] { "Network", "Timeline", "Profiler", "Components" })
        {
            cut.FindAll(".bev-tab").First(t => t.TextContent.Contains(tab, StringComparison.Ordinal)).Click();
        }

        cut.Find("button[aria-label='Refresh captured data']").Click();

        foreach (var record in Session.Components.All)
        {
            Assert.IsTrue(record.IsBevTools, $"{record.TypeName} is rendered by the BevTools panel but is tracked as application activity.");
        }
        Assert.IsEmpty(Session.Timeline.Snapshot());
        Assert.IsEmpty(Session.Interop.Snapshot());
        Assert.IsEmpty(Session.Components.BuildTree(includeHidden: true));
    }

    /// <summary>The inverse: the propagation must not swallow the application's own use of the same framework components.</summary>
    [TestMethod]
    public void Application_components_under_a_cascading_value_are_still_application_activity()
    {
        RenderPanel();
        var cut = Render<AppHost>();

        var child = Assert.ContainsSingle(r => r.Type == typeof(ChildComponent), Session.Components.All);
        Assert.IsFalse(child.IsBevTools);
        Assert.Contains(e => e.ComponentName == child.DisplayName, Session.Timeline.Snapshot());
        Assert.IsNotNull(cut);
    }

    [TestMethod]
    public void Inspector_section_reacts_when_collapsed_parameter_changes()
    {
        var cut = Render<BevInspectorSection>(parameters => parameters
            .Add(p => p.Title, "Errors")
            .Add(p => p.Collapsed, true)
            .AddChildContent("details"));

        Assert.DoesNotContain("details", cut.Markup, StringComparison.Ordinal);
        cut.Render(parameters => parameters
            .Add(p => p.Title, "Errors")
            .Add(p => p.Collapsed, false)
            .AddChildContent("details"));

        Assert.Contains("details", cut.Markup, StringComparison.Ordinal);
        Assert.AreEqual("true", cut.Find(".bev-section-title").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void A_children_only_value_tree_shows_the_root_members_and_indents_nested_ones()
    {
        var cut = Render<BevValueTree>(parameters => parameters
            .Add(p => p.Root, new SnapshotSample())
            .Add(p => p.Name, "SnapshotSample")
            .Add(p => p.Inspector, Session.Inspector)
            .Add(p => p.ChildrenOnly, true));

        var rows = cut.FindAll(".bev-vt-row");
        Assert.Contains(r => r.TextContent.Contains("Theme:", StringComparison.Ordinal) && r.GetAttribute("style")!.Contains("padding-left:0px", StringComparison.Ordinal), rows);
        Assert.DoesNotContain(r => r.TextContent.Contains("SnapshotSample:", StringComparison.Ordinal), rows);

        rows.First(r => r.TextContent.Contains("Nested:", StringComparison.Ordinal)).QuerySelector(".bev-caret")!.Click();

        var depth = cut.FindAll(".bev-vt-row").First(r => r.TextContent.Contains("Depth:", StringComparison.Ordinal));
        Assert.Contains("padding-left:14px", depth.GetAttribute("style")!, StringComparison.Ordinal);
    }

    private sealed class SnapshotSample
    {
        public string Theme { get; } = "dark";

        public NestedSample Nested { get; } = new();
    }

    private sealed class NestedSample
    {
        public int Depth { get; } = 2;
    }

    [TestMethod]
    public void Selecting_a_component_through_the_host_stops_capturing_the_previous_one()
    {
        var cut = RenderPanel();
        var first = Session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false)!;
        var second = Session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false)!;

        cut.InvokeAsync(() => cut.Instance.SelectComponent(first.InstanceId));
        Assert.IsTrue(first.CaptureState);
        cut.InvokeAsync(() => cut.Instance.SelectComponent(second.InstanceId));

        Assert.IsFalse(first.CaptureState);
        Assert.IsTrue(second.CaptureState);
    }

    [TestMethod]
    public void Browser_bridge_can_be_disabled_without_preventing_the_panel_from_rendering()
    {
        using var context = new BunitContext();
        context.Services.AddBitBevToolsServices(options =>
        {
            options.Enabled = true;
            options.EnableBrowserBridge = false;
            options.Ui.OpenByDefault = true;
        });
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = context.JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<string?>("loadPrefs", _ => true).SetResult("""{"theme":"Dark"}""");
        context.UseRenderer("WebAssembly");

        var cut = context.Render<BevToolsPanel>();
        var session = context.Services.GetRequiredService<BevToolsSession>();

        Assert.IsNotEmpty(cut.FindAll(".bev-panel"));
        Assert.IsFalse(session.Browser.BridgeAttached);
        Assert.AreEqual(BevToolsTheme.Dark, session.Ui.Theme);
        Assert.DoesNotContain(invocation => invocation.Identifier == "attach", module.Invocations);
        cut.InvokeAsync(cut.Instance.Toggle);
        Assert.Contains(invocation => invocation.Identifier == "savePrefs", module.Invocations);
    }

    [TestMethod]
    public void Interop_filter_treats_colon_identifiers_as_search_text()
    {
        var cut = RenderPanel();
        var record = new BevJsInteropRecord
        {
            Id = Session.Interop.NextId(),
            At = DateTimeOffset.UtcNow,
            Direction = BevJsInteropDirection.DotNetToJs,
            Identifier = "module:invoke",
            ComponentName = "CartWidget",
        };
        Session.Interop.Add(record);
        Session.Interop.Complete(record, 1, succeeded: true, error: null);
        cut.FindAll(".bev-tab").Single(tab => tab.TextContent.Trim() == "JS Interop").Click();

        cut.Find("input[aria-label='Filter JS interop calls']").Input("module:invoke");

        Assert.Contains("module:invoke", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("No JS interop calls captured", cut.Markup, StringComparison.Ordinal);
        cut.Find("input[aria-label='Filter JS interop calls']").Input("CartWidget");
        Assert.Contains("module:invoke", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>An application component nested in the framework components the panels also use.</summary>
    private sealed class AppHost : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<int>>(0);
            builder.AddComponentParameter(1, nameof(CascadingValue<int>.Value), 7);
            builder.AddComponentParameter(2, nameof(CascadingValue<int>.ChildContent), (RenderFragment)(b =>
            {
                b.OpenComponent<ChildComponent>(0);
                b.AddComponentParameter(1, nameof(ChildComponent.Text), "app");
                b.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    [TestMethod]
    public void Programmatic_navigation_is_not_parented_to_an_old_ui_event()
    {
        RenderPanel();
        Session.LastUiEventId = Session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.UiEvent,
            Title = "old click",
        });

        Services.GetRequiredService<NavigationManager>().NavigateTo("/later");

        var navigation = Session.Timeline.Snapshot().Last(e => e.Kind == BevToolsEventKind.Navigation);
        Assert.IsNull(navigation.ParentEventId);
    }
}
