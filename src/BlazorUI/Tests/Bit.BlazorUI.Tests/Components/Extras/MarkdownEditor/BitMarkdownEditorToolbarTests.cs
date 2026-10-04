using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

/// <summary>
/// Covers what an app's own toolbar items can do: draw a built-in icon, spell a label out, bind a shortcut the
/// script handles (and the shortcuts panel lists), re-render the component that owns their handler, and show the
/// localized title of a default menu item rather than its English text.
/// </summary>
[TestClass]
public class BitMarkdownEditorToolbarTests : BunitTestContext
{
    private sealed class Owner : IHandleEvent
    {
        public int Handled { get; private set; }
        public int Clicked { get; private set; }

        public Task OnClick(BitMarkdownEditor editor)
        {
            Clicked++;
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(EventCallbackWorkItem item, object? arg)
        {
            Handled++;
            return item.InvokeAsync(arg);
        }
    }

    private static Dictionary<string, string>? ShortcutsOf(object? config) =>
        config?.GetType().GetProperty("Shortcuts")?.GetValue(config) as Dictionary<string, string>;

    [TestMethod]
    public void BitMarkdownEditorMenuItemShouldShowItsLocalizedTitle()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Texts, new BitMarkdownEditorTexts { ToolbarTableInsertRowAbove = "Insérer une ligne au-dessus" });
        });

        var item = component.Find("[data-cmd=trowabove]");

        // What is on screen is what is read out (WCAG 2.5.3).
        Assert.AreEqual("Insérer une ligne au-dessus", item.QuerySelector(".bit-mde-mit")!.TextContent);
        Assert.AreEqual("Insérer une ligne au-dessus", item.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownEditorToolbarItemShouldDrawItsIconNameAndText()
    {
        var owner = new Owner();
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, [new() { Name = "save", Text = "Save", IconName = "Save", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick }]);
        });

        var button = component.Find("[data-cmd=save]");
        var icon = button.QuerySelector("i.bit-mde-ico")!;

        Assert.IsTrue(icon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Save"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
        Assert.IsTrue(button.ClassList.Contains("bit-mde-btx"));
        Assert.AreEqual("Save", button.QuerySelector(".bit-mde-mit")!.TextContent);

        // With no Title, the text names the button.
        Assert.AreEqual("Save", button.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownEditorOwnIconMarkupShouldWinOverTheIconName()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, [new() { Name = "bold", Title = "Bold", Command = BitMarkdownEditorCommand.Bold, Icon = "<svg class=\"own\"></svg>", IconName = "Bold" }]);
        });

        var button = component.Find("[data-cmd=bold]");

        Assert.IsNotNull(button.QuerySelector("svg.own"));
        Assert.IsNull(button.QuerySelector("i.bit-mde-ico"));
        Assert.IsFalse(button.ClassList.Contains("bit-mde-btx"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldBindTheShortcutsOfItsToolbarItems()
    {
        var owner = new Owner();
        RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar,
            [
                new() { Name = "bold", Command = BitMarkdownEditorCommand.Bold, Shortcut = "Ctrl+B" },
                new() { Name = "quote", Command = BitMarkdownEditorCommand.Quote, Shortcut = "Cmd+Shift+." },
                new() { Name = "save", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "Ctrl+S" },
                new() { Name = "rename", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "F2" },
                new()
                {
                    Name = "more", Type = BitMarkdownEditorToolbarItemType.Dropdown,
                    Children = [new() { Name = "export", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "Option + E" }]
                },
                // Plain typing is never taken over, and the first item to claim a key keeps it.
                new() { Name = "typing", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "S" },
                new() { Name = "again", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "ctrl+s" },
                // Only a command or a custom item binds one.
                new() { Name = "find", Type = BitMarkdownEditorToolbarItemType.Find, Shortcut = "Ctrl+Shift+F" },
            ]);
        });

        var shortcuts = ShortcutsOf(Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.init").Arguments[^1])!;

        Assert.AreEqual("cmd:Bold", shortcuts["ctrl+b"]);
        Assert.AreEqual("cmd:Quote", shortcuts["ctrl+shift+."]);
        Assert.AreEqual("item:save", shortcuts["ctrl+s"]);
        Assert.AreEqual("item:rename", shortcuts["f2"]);
        Assert.AreEqual("item:export", shortcuts["alt+e"]);
        Assert.AreEqual(5, shortcuts.Count);
    }

    [TestMethod]
    public async Task BitMarkdownEditorItemShortcutShouldRunTheItemThroughItsOwner()
    {
        var owner = new Owner();
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, [new() { Name = "save", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "Ctrl+S" }]);
        });

        await component.InvokeAsync(() => component.Instance._OnToolbarShortcut("save"));

        Assert.AreEqual(1, owner.Clicked);
        // Run as the owner's event handler, so the owner re-renders after it.
        Assert.AreEqual(1, owner.Handled);

        await component.InvokeAsync(() => component.Instance._OnToolbarShortcut("missing"));

        Assert.AreEqual(1, owner.Clicked);
    }

    [TestMethod]
    public async Task BitMarkdownEditorItemShortcutShouldHonourTheReadOnlyState()
    {
        var owner = new Owner();
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Toolbar,
            [
                new() { Name = "clear", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "Ctrl+Shift+X" },
                new() { Name = "copy", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "Ctrl+Shift+C", AlwaysEnabled = true },
            ]);
        });

        await component.InvokeAsync(() => component.Instance._OnToolbarShortcut("clear"));
        Assert.AreEqual(0, owner.Clicked);

        await component.InvokeAsync(() => component.Instance._OnToolbarShortcut("copy"));
        Assert.AreEqual(1, owner.Clicked);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShortcutsPanelShouldListTheAppsOwnShortcuts()
    {
        var owner = new Owner();
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar,
            [
                new() { Name = "save", Title = "Save draft", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = owner.OnClick, Shortcut = "Ctrl+S" },
                new() { Name = "section", Title = "Section", Command = BitMarkdownEditorCommand.Heading2, Shortcut = "Ctrl+Shift+H" },
                // A built-in shortcut is already listed under its own name.
                new() { Name = "bold", Title = "Bold", Command = BitMarkdownEditorCommand.Bold, Shortcut = "Ctrl+B" },
                new() { Name = "help", Type = BitMarkdownEditorToolbarItemType.Help },
            ]);
        });

        await component.InvokeAsync(() => component.Instance._OnShortcut("help"));

        var rows = component.FindAll("[role=dialog] dl > div");
        var text = component.Find("[role=dialog] dl").TextContent;

        StringAssert.Contains(text, "Save draft");
        StringAssert.Contains(text, "Ctrl+S");
        StringAssert.Contains(text, "Section");
        StringAssert.Contains(text, "Ctrl+Shift+H");
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(text, "Bold").Count);
        Assert.IsTrue(rows.Count > 2);
    }
}
