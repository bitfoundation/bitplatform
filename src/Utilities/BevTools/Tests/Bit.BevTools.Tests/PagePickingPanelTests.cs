using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>The panel's side of components in the page: hovering, picking, Show in page and render flashes.</summary>
[TestClass]
public class PagePickingPanelTests : BunitContext
{
    private readonly List<(string Method, object?[] Arguments)> _calls = [];
    private DotNetObjectReference<BevJsBridge>? _bridge;
    private BevHighlightResult _highlightResult = new() { Ok = true, Visible = true };

    public PagePickingPanelTests()
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
        module.Setup<BevBrowserSnapshot>("attach", invocation =>
        {
            _bridge = (DotNetObjectReference<BevJsBridge>)invocation.Arguments[0]!;
            return true;
        }).SetResult(new BevBrowserSnapshot { Online = true, TargetId = "t1" });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(prefs);
        module.SetupVoid("savePrefs", Record).SetVoidResult();
        module.Setup<BevHighlightResult>("highlight", Record).SetResult(_highlightResult);
        module.SetupVoid("clearHighlight", Record).SetVoidResult();
        module.SetupVoid("flash", Record).SetVoidResult();
        module.SetupVoid("stopPick", Record).SetVoidResult();
        module.Setup<bool>("startPick", Record).SetResult(true);
        return Render<BevToolsPanel>();
    }

    private bool Record(JSRuntimeInvocation invocation)
    {
        _calls.Add((invocation.Identifier, invocation.Arguments.ToArray()));
        return true;
    }

    private IRenderedComponent<BevToolsPanel> RenderPanelWithApp()
    {
        var panel = RenderPanel();
        Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
        panel.InvokeAsync(panel.Instance.Refresh);
        return panel;
    }

    [TestMethod]
    public void Tree_rows_leave_hovering_to_the_page_hub_and_the_keyboard_moves_the_outline()
    {
        var panel = RenderPanelWithApp();
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        var row = panel.FindAll(".bev-tree-row").Single(r => r.TextContent.Contains("ChildComponent", StringComparison.Ordinal));

        // Hovering is the hub's: a Blazor event per hovered row would be a circuit message per mouse move.
        Assert.AreEqual(child.InstanceId.ToString(System.Globalization.CultureInfo.InvariantCulture), row.GetAttribute("data-bev-id"));
        Assert.ThrowsExactly<MissingEventHandlerException>(() => row.TriggerEvent("onmouseenter", new MouseEventArgs()));

        // The keyboard has no hover: moving to a row outlines its component.
        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        panel.InvokeAsync(() => panel.Instance.InspectComponent(parent.InstanceId));
        var events = Session.Timeline.Snapshot().Length;
        panel.Find(".bev-tree").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var (_, arguments) = _calls.Single(c => c.Method == "highlight");
        Assert.AreEqual("t1", arguments[0]);
        var location = (BevDomLocation)arguments[1]!;
        Assert.AreEqual(child.InstanceId, location.InstanceId);
        Assert.AreEqual("#c", location.Path[^1].Kind, "a path ends on the component's container comment");
        Assert.AreEqual(events, Session.Timeline.Snapshot().Length, "BevTools' own keyboard navigation was recorded as application activity");
    }

    [TestMethod]
    public void A_component_picked_in_the_page_is_selected_in_the_inspector()
    {
        var panel = RenderPanelWithApp();
        var pick = panel.Find("button[aria-label='Pick a component in the page']");
        pick.Click();

        var (_, arguments) = _calls.Single(c => c.Method == "startPick");
        Assert.AreEqual("t1", arguments[0]);
        Assert.AreEqual("true", panel.Find("button[aria-label='Pick a component in the page']").GetAttribute("aria-pressed"));

        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        _bridge!.Value.OnComponentPicked(child.InstanceId);

        panel.WaitForAssertion(() =>
        {
            Assert.AreEqual(child.InstanceId, Session.Ui.SelectedComponentId);
            Assert.AreEqual("ChildComponent", panel.Find(".bev-right strong.bev-mono").TextContent);
            Assert.AreEqual("false", panel.Find("button[aria-label='Pick a component in the page']").GetAttribute("aria-pressed"));
        });
    }

    [TestMethod]
    public void A_pick_that_ends_without_a_component_says_why()
    {
        var panel = RenderPanelWithApp();
        panel.Find("button[aria-label='Pick a component in the page']").Click();

        _bridge!.Value.OnPickEnded("This part of the page is not inside an interactive component.");

        panel.WaitForAssertion(() =>
        {
            Assert.Contains("not inside an interactive component", panel.Find(".bev-notification").TextContent, StringComparison.Ordinal);
            Assert.AreEqual("false", panel.Find("button[aria-label='Pick a component in the page']").GetAttribute("aria-pressed"));
        });
    }

    [TestMethod]
    public void Show_in_page_scrolls_to_the_component_and_says_when_it_cannot_be_shown()
    {
        var panel = RenderPanelWithApp();
        var child = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));
        panel.InvokeAsync(() => panel.Instance.SelectComponent(child.InstanceId));
        _highlightResult.Ok = false;
        _highlightResult.Error = "The page does not match the component's latest render.";

        panel.FindAll(".bev-right button").Single(b => b.TextContent.Contains("Show in page", StringComparison.Ordinal)).Click();

        var (_, arguments) = _calls.Single(c => c.Method == "highlight");
        Assert.AreEqual(child.InstanceId, ((BevDomLocation)arguments[1]!).InstanceId);
        StringAssert.Contains(System.Text.Json.JsonSerializer.Serialize(arguments[2], new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)), "\"scroll\":true");
        panel.WaitForAssertion(() => Assert.Contains("does not match", panel.Find(".bev-notification").TextContent, StringComparison.Ordinal));
    }

    [TestMethod]
    public void Render_flashing_is_a_saved_preference_and_outlines_what_rendered()
    {
        var panel = RenderPanelWithApp();
        panel.Find("label[title^='Outline components in the page'] input").Change(true);

        Assert.IsTrue(Session.Ui.FlashRenders);
        Assert.Contains(c => c.Method == "savePrefs" && ((string)c.Arguments[0]!).Contains("\"flash\":true", StringComparison.Ordinal), _calls);

        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        panel.InvokeAsync(() => ((ParentComponent)parent.Component!).Increment());
        panel.WaitForAssertion(() => Assert.Contains(c => c.Method == "flash", _calls), TimeSpan.FromSeconds(5));
        // Serialized as Blazor serializes interop arguments: what the hub reads.
        var items = System.Text.Json.JsonSerializer.Serialize(_calls.First(c => c.Method == "flash").Arguments[1], new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        StringAssert.Contains(items, "\"name\":\"ParentComponent\"");

        // A panel rendered later reads the preference back.
        var restored = RenderPanel("{\"flash\":true}");
        Assert.IsTrue(Session.Ui.FlashRenders);
        Assert.IsNotNull(restored);
    }
}
