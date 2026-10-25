using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// BevTools' own UI must not be the inaccessible part of the page it is debugging: every control named, the tab list
/// keyboard-operable, focus kept where the developer is.
/// </summary>
[TestClass]
public class OwnAccessibilityTests : BunitContext
{
    private readonly List<(string Method, object?[] Arguments)> _calls = [];

    public OwnAccessibilityTests()
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

    private IRenderedComponent<BevToolsPanel> RenderPanel()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { TargetId = "t1" });
        module.SetupVoid("focusIn", invocation =>
        {
            _calls.Add((invocation.Identifier, invocation.Arguments.ToArray()));
            return true;
        }).SetVoidResult();
        var panel = Render<BevToolsPanel>();
        Render<ParentComponent>();
        return panel;
    }

    /// <summary>A name a screen reader announces: its aria-label, a label around it or pointing at it, or for a button its text with letters in it.</summary>
    private static bool HasName(IElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.GetAttribute("aria-label")) || element.Closest("label") is not null)
        {
            return true;
        }

        if (element.Id is { Length: > 0 } id && element.Owner!.QuerySelector("label[for='" + id + "']") is not null)
        {
            return true;
        }

        return element.LocalName == "button" && element.TextContent.Any(char.IsLetterOrDigit);
    }

    [TestMethod]
    public void Every_control_of_every_tab_has_a_name()
    {
        var panel = RenderPanel();
        // With a component inspected, so the inspector's sections and value trees are among the controls checked.
        var parent = Session.Components.All.Single(r => r.Type == typeof(ParentComponent));
        panel.InvokeAsync(() => panel.Instance.SelectComponent(parent.InstanceId));
        var tabs = panel.FindAll("[role=tab]").Select(t => t.Id!.Replace("bev-tab-", "", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(tabs.Count >= 14);
        var unnamed = new List<string>();
        foreach (var tab in tabs)
        {
            panel.InvokeAsync(() => panel.Instance.ShowPanel(tab));
            foreach (var control in panel.FindAll(".bev-panel input, .bev-panel select, .bev-panel textarea, .bev-panel button"))
            {
                if (control.GetAttribute("type") != "hidden" && !HasName(control))
                {
                    unnamed.Add(tab + ": " + control.OuterHtml[..Math.Min(120, control.OuterHtml.Length)]);
                }
            }
        }

        Assert.IsEmpty(unnamed.Distinct(), string.Join("\n", unnamed.Distinct()));
        Assert.AreEqual("region", panel.Find(".bev-panel").GetAttribute("role"));
        Assert.AreEqual("BevTools developer tools", panel.Find(".bev-panel").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void The_tab_list_moves_with_the_arrow_keys_and_focus_follows()
    {
        var panel = RenderPanel();
        Assert.AreEqual("components", Session.Ui.ActivePanelId);
        Assert.AreEqual("0", panel.Find("#bev-tab-components").GetAttribute("tabindex"));
        Assert.AreEqual("-1", panel.Find("#bev-tab-profiler").GetAttribute("tabindex"), "one tab stop for the whole list");

        panel.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual("profiler", Session.Ui.ActivePanelId);
        panel.WaitForAssertion(() => Assert.Contains(c => c.Method == "focusIn" && (string)c.Arguments[1]! == ".bev-tab.bev-active", _calls));

        panel.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.AreEqual("about", Session.Ui.ActivePanelId);
        panel.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual("components", Session.Ui.ActivePanelId, "the list wraps around");
        panel.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.AreEqual("about", Session.Ui.ActivePanelId);
    }

    [TestMethod]
    public void Escape_closes_the_panel_and_focus_returns_to_the_badge()
    {
        var panel = RenderPanel();
        panel.Find(".bev-root").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(Session.Ui.IsOpen);
        panel.WaitForAssertion(() => Assert.Contains(c => c.Method == "focusIn" && (string)c.Arguments[1]! == ".bev-badge", _calls));
        Assert.IsNotNull(panel.Find(".bev-badge"));
    }

    [TestMethod]
    public void The_command_palette_is_a_dialog_with_a_listbox_of_options()
    {
        var panel = RenderPanel();
        panel.Find("button[title^='Search or run a command']").Click();

        var dialog = panel.Find(".bev-palette");
        Assert.AreEqual("dialog", dialog.GetAttribute("role"));
        var input = panel.Find(".bev-palette input");
        Assert.AreEqual("combobox", input.GetAttribute("role"));
        Assert.AreEqual("bev-palette-item-0", input.GetAttribute("aria-activedescendant"));
        Assert.AreEqual("listbox", panel.Find("#bev-palette-list").GetAttribute("role"));
        var options = panel.FindAll("[role=option]");
        Assert.IsTrue(options.Count > 1);
        Assert.AreEqual("true", options[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void Escape_in_the_palette_closes_the_palette_and_not_the_panel_and_focus_returns_to_its_opener()
    {
        var panel = RenderPanel();
        panel.Find("button[title^='Search or run a command']").Click();
        Assert.HasCount(2, panel.FindAll(".bev-palette .bev-focus-sentinel"), "Tab stays inside the dialog");

        panel.Find(".bev-palette input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(Session.Ui.CommandPaletteOpen);
        Assert.IsTrue(Session.Ui.IsOpen, "the key was the palette's");
        panel.WaitForAssertion(() => Assert.Contains(c => c.Method == "focusIn" && (string)c.Arguments[1]! == "[data-bev-palette-opener]", _calls));
    }
}
