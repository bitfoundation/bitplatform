using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

[TestClass]
public class BitMarkdownEditorTests : BunitTestContext
{
    // Runs after the base class Setup that creates the bUnit context.
    [TestInitialize]
    public void SetupJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.init");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.setValue");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.run");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.undo");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.redo");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.focus");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.dispose");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.getValue");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-mde");

        if (isEnabled)
        {
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldInitializeWithDefaultValue()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Id, "id");
            parameters.Add(p => p.DefaultValue, "hello");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.init");
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldInvokeOnChange()
    {
        string? changed = null;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create(this, (string? value) =>
            {
                changed = value;
                return Task.CompletedTask;
            }));
        });

        await component.Instance._OnChange("new value");

        Assert.AreEqual("new value", changed);
        Assert.AreEqual("new value", component.Instance.Value);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldSetValueAndCallJs()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Value, "initial");
        });

        // The initial value is seeded by init on first render, not by setValue.
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.MarkdownEditor.setValue");

        Assert.AreEqual("initial", component.Instance.Value);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, "updated");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.setValue", 1);

        Assert.AreEqual("updated", component.Instance.Value);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldRunCommand()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance.Run(BitMarkdownEditorCommand.Bold);

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.run");
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldUndoAndRedo()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance.Undo();
        await component.Instance.Redo();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.undo");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.redo");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldApplyCommands()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        var result = component.Instance._ApplyCommand("Bold", 0, 4, "test");

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("**test**", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
        Assert.AreEqual(6, result.SelectionEnd);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNotApplyCommandsWhenReadOnly()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        var result = component.Instance._ApplyCommand("Bold", 0, 4, "test");

        Assert.IsFalse(result.Handled);
        Assert.AreEqual("test", result.Text);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNotApplyUnknownCommands()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        var result = component.Instance._ApplyCommand("NotACommand", 0, 4, "test");

        Assert.IsFalse(result.Handled);
        Assert.AreEqual("test", result.Text);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldUpdateHistoryState()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        Assert.IsFalse(component.Instance.CanUndo);
        Assert.IsFalse(component.Instance.CanRedo);

        component.Instance._OnHistoryChanged(true, false);

        Assert.IsTrue(component.Instance.CanUndo);
        Assert.IsFalse(component.Instance.CanRedo);

        component.Instance._OnHistoryChanged(true, true);

        Assert.IsTrue(component.Instance.CanUndo);
        Assert.IsTrue(component.Instance.CanRedo);
    }

    [TestMethod,
        DataRow(BitMarkdownEditorMode.Edit),
        DataRow(BitMarkdownEditorMode.Split),
        DataRow(BitMarkdownEditorMode.Preview)]
    public void BitMarkdownEditorShouldRespectMode(BitMarkdownEditorMode mode)
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Mode, mode);
        });

        var body = component.Find(".bit-mde-bdy");

        Assert.IsTrue(body.ClassList.Contains($"bit-mde-{mode.ToString().ToLowerInvariant()}"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRenderDefaultToolbar()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        // Top-level toolbar buttons (dropdown triggers included, but not the buttons
        // rendered inside a dropdown menu).
        var buttons = component.FindAll(".bit-mde-btn:not(.bit-mde-mi)");

        Assert.AreEqual(BitMarkdownEditorToolbar.Default.Count(i => i.Type is not BitMarkdownEditorToolbarItemType.Separator), buttons.Count);

        // The default toolbar includes a heading dropdown with menu items.
        var menuItems = component.FindAll(".bit-mde-mi");
        Assert.IsTrue(menuItems.Count >= 6);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRenderCustomToolbar()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, new BitMarkdownEditorToolbarItem[]
            {
                new() { Name = "bold", Title = "Bold", Command = BitMarkdownEditorCommand.Bold, Icon = BitMarkdownEditorToolbar.Icons.Bold },
                BitMarkdownEditorToolbarItem.Separator,
                new() { Name = "italic", Title = "Italic", Command = BitMarkdownEditorCommand.Italic, Icon = BitMarkdownEditorToolbar.Icons.Italic },
            });
        });

        Assert.AreEqual(2, component.FindAll(".bit-mde-btn").Count);
        Assert.AreEqual(1, component.FindAll(".bit-mde-sep").Count);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectShowToolbar(bool showToolbar)
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, showToolbar);
        });

        Assert.AreEqual(showToolbar ? 1 : 0, component.FindAll(".bit-mde-tlb").Count);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectShowStatusBar(bool showStatusBar)
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ShowStatusBar, showStatusBar);
        });

        Assert.AreEqual(showStatusBar ? 1 : 0, component.FindAll(".bit-mde-sbr").Count);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectFullScreen(bool fullScreen)
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.FullScreen, fullScreen);
        });

        var root = component.Find(".bit-mde");

        Assert.AreEqual(fullScreen, root.ClassList.Contains("bit-mde-fsc"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRespectHeight()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Height, "10rem");
        });

        var root = component.Find(".bit-mde");

        Assert.IsTrue(root.GetAttribute("style")!.Contains("--bit-mde-height:10rem"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldDisposeJsInterop()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.dispose");
    }
}
