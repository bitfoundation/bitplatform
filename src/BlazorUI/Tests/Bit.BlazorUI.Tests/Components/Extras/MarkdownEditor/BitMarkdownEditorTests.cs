using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.setConfig");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.setValue");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.run");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.insert");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.undo");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.redo");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.focus");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.blur");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.clearDraft");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.setSelection");
        Context.JSInterop.SetupVoid("BitBlazorUI.MarkdownEditor.dispose");
        Context.JSInterop.Setup<string>("BitBlazorUI.MarkdownEditor.getValue");
        // The value-returning find/replace/selection calls are left to the loose mode (or to
        // a per-test Setup), so a test can plan its own result for them.
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

    [TestMethod]
    public void BitMarkdownEditorShouldRenderTextAreaAttributes()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "write here");
            parameters.Add(p => p.SpellCheck, false);
            parameters.Add(p => p.MaxLength, 100);
        });

        var textArea = component.Find(".bit-mde-txa");

        Assert.AreEqual("write here", textArea.GetAttribute("placeholder"));
        Assert.AreEqual("false", textArea.GetAttribute("spellcheck"));
        Assert.AreEqual("100", textArea.GetAttribute("maxlength"));
        Assert.IsFalse(textArea.HasAttribute("readonly"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectReadOnly(bool readOnly)
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        Assert.AreEqual(readOnly, component.Find(".bit-mde-txa").HasAttribute("readonly"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldUseAriaLabelOnTheTextArea()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "the article body");
        });

        Assert.AreEqual("the article body", component.Find(".bit-mde-txa").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldFallBackToTheLocalizedEditorAriaLabel()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Texts, new BitMarkdownEditorTexts { EditorAriaLabel = "ویرایشگر" });
        });

        Assert.AreEqual("ویرایشگر", component.Find(".bit-mde-txa").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldMakeThePreviewPaneKeyboardReachable()
    {
        // The pane scrolls on its own, so it has to be focusable and named.
        var preview = RenderComponent<BitMarkdownEditor>().Find(".bit-mde-ppn");

        Assert.AreEqual("0", preview.GetAttribute("tabindex"));
        Assert.AreEqual("Markdown preview", preview.GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitMarkdownEditorShouldRespectResizable(bool resizable)
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Resizable, resizable);
        });

        Assert.AreEqual(resizable, component.Find(".bit-mde").ClassList.Contains("bit-mde-rsz"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRespectDir()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", component.Find(".bit-mde").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldApplyClassesAndStyles()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitMarkdownEditorClassStyles { Root = "root-class", Toolbar = "tlb-class", TextArea = "txa-class", Preview = "pvw-class", StatusBar = "sbr-class" });
            parameters.Add(p => p.Styles, new BitMarkdownEditorClassStyles { StatusBar = "color:tomato" });
        });

        Assert.IsTrue(component.Find(".bit-mde").ClassList.Contains("root-class"));
        Assert.IsTrue(component.Find(".bit-mde-tlb").ClassList.Contains("tlb-class"));
        Assert.IsTrue(component.Find(".bit-mde-txa").ClassList.Contains("txa-class"));
        Assert.IsTrue(component.Find(".bit-mde-ppn").ClassList.Contains("pvw-class"));
        Assert.IsTrue(component.Find(".bit-mde-sbr").ClassList.Contains("sbr-class"));
        Assert.IsTrue(component.Find(".bit-mde-sbr").GetAttribute("style")!.Contains("color:tomato"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldCountWordsAndCharacters()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceTime, 0);
        });

        // The emoji is a surrogate pair but a single grapheme cluster.
        await component.Instance._OnChange("hello brave world 👋");

        var statusBar = component.Find(".bit-mde-sbr").TextContent;

        Assert.Contains("4 words", statusBar);
        Assert.Contains("19 chars", statusBar);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldShowTheLimitInTheCounterWhenMaxLengthIsSet()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceTime, 0);
            parameters.Add(p => p.MaxLength, 5);
        });

        await component.Instance._OnChange("abc");
        Assert.Contains("3 / 5 chars", component.Find(".bit-mde-sbr").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-mde-lim").Count);

        await component.Instance._OnChange("abcde");
        Assert.AreEqual(1, component.FindAll(".bit-mde-lim").Count);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldShowTheReadingTime()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceTime, 0);
            parameters.Add(p => p.ShowReadingTime, true);
            parameters.Add(p => p.WordsPerMinute, 2);
        });

        await component.Instance._OnChange("one two three four five");

        Assert.Contains("3 min read", component.Find(".bit-mde-sbr").TextContent);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldLocalizeTheStatusBarAndToolbar()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Texts, new BitMarkdownEditorTexts
            {
                ModeSplit = "دوستونه",
                WordsFormat = "{0} واژه",
                ToolbarBold = "توپر"
            });
        });

        Assert.Contains("دوستونه", component.Find(".bit-mde-sbr").TextContent);
        Assert.Contains("واژه", component.Find(".bit-mde-sbr").TextContent);
        Assert.AreEqual("توپر", component.Find("[data-cmd=bold]").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldReportShortcutsToAssistiveTech()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        // "Ctrl" is not an aria-keyshortcuts modifier name; "Control" is.
        Assert.AreEqual("Control+B", component.Find("[data-cmd=bold]").GetAttribute("aria-keyshortcuts"));
        Assert.IsFalse(component.Find("[data-cmd=quote]").HasAttribute("aria-keyshortcuts"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRenderAnAccessibleDropdownMenu()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        var trigger = component.Find(".bit-mde-dd > .bit-mde-btn");
        Assert.AreEqual("true", trigger.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", trigger.GetAttribute("aria-expanded"));

        var menu = component.Find(".bit-mde-ddm");
        Assert.AreEqual("menu", menu.GetAttribute("role"));

        // Menu items are focused by the script, never by tabbing into them.
        foreach (var item in component.FindAll(".bit-mde-mi"))
        {
            Assert.AreEqual("menuitem", item.GetAttribute("role"));
            Assert.AreEqual("-1", item.GetAttribute("tabindex"));
        }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldDisableEditingToolbarItemsWhenReadOnly()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.IsTrue(component.Find("[data-cmd=bold]").HasAttribute("disabled"));
        // Chrome-only items keep working: they change nothing about the text.
        Assert.IsFalse(component.Find("[data-cmd=preview]").HasAttribute("disabled"));
        Assert.IsFalse(component.Find("[data-cmd=help]").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldKeepAlwaysEnabledItemsClickableWhenReadOnly()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Toolbar, new BitMarkdownEditorToolbarItem[]
            {
                new() { Name = "export", Title = "Export", Type = BitMarkdownEditorToolbarItemType.Custom, AlwaysEnabled = true, OnClick = _ => Task.CompletedTask },
                new() { Name = "save", Title = "Save", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = _ => Task.CompletedTask },
            });
        });

        Assert.IsFalse(component.Find("[data-cmd=export]").HasAttribute("disabled"));
        Assert.IsTrue(component.Find("[data-cmd=save]").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldInvokeCustomToolbarItemCallback()
    {
        var clicked = false;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, new BitMarkdownEditorToolbarItem[]
            {
                new()
                {
                    Name = "custom",
                    Title = "Custom",
                    Type = BitMarkdownEditorToolbarItemType.Custom,
                    OnClick = _ => { clicked = true; return Task.CompletedTask; }
                }
            });
        });

        component.Find("[data-cmd=custom]").Click();

        Assert.IsTrue(clicked);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldHighlightTheActiveFormatAtTheCaret()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        Assert.IsFalse(component.Find("[data-cmd=bold]").ClassList.Contains("bit-mde-act"));

        // The script sends the caret's own line with the offsets rebased on it.
        component.Instance._OnSelectionChanged(4, 4, "**bold**");

        Assert.IsTrue(component.Find("[data-cmd=bold]").ClassList.Contains("bit-mde-act"));
        Assert.AreEqual("true", component.Find("[data-cmd=bold]").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldCycleModesFromTheToolbarAndTheShortcut()
    {
        var mode = BitMarkdownEditorMode.Edit;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Bind(p => p.Mode, mode, v => mode = v);
        });

        component.Find("[data-cmd=preview]").Click();
        Assert.AreEqual(BitMarkdownEditorMode.Split, component.Instance.Mode);

        await component.Instance._OnShortcut("mode");
        Assert.AreEqual(BitMarkdownEditorMode.Preview, component.Instance.Mode);

        await component.Instance._OnShortcut("mode");
        Assert.AreEqual(BitMarkdownEditorMode.Edit, component.Instance.Mode);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldToggleFullScreenFromTheShortcut()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance._OnShortcut("fullscreen");

        Assert.IsTrue(component.Instance.FullScreen);
        Assert.IsTrue(component.Find(".bit-mde").ClassList.Contains("bit-mde-fsc"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldOpenTheFindPanelFromTheShortcut()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        Assert.AreEqual(0, component.FindAll(".bit-mde-fnd").Count);

        await component.Instance._OnShortcut("find");

        Assert.AreEqual(1, component.FindAll(".bit-mde-fnd").Count);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldToggleTheFindPanelFromTheToolbar()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=find]").Click();
        Assert.AreEqual(1, component.FindAll(".bit-mde-fnd").Count);
        Assert.AreEqual("true", component.Find("[data-cmd=find]").GetAttribute("aria-pressed"));

        component.Find("[data-cmd=find]").Click();
        Assert.AreEqual(0, component.FindAll(".bit-mde-fnd").Count);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldCloseTheFindPanelBeforeLeavingFullScreenOnEscape()
    {
        var fullScreen = true;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Bind(p => p.FullScreen, fullScreen, v => fullScreen = v);
        });

        await component.Instance._OnShortcut("find");

        await component.Instance._OnEscape();
        Assert.AreEqual(0, component.FindAll(".bit-mde-fnd").Count);
        Assert.IsTrue(component.Instance.FullScreen);

        await component.Instance._OnEscape();
        Assert.IsFalse(component.Instance.FullScreen);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldReportTheMatchCountInTheFindPanel()
    {
        Context.JSInterop.Setup<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.find", _ => true).SetResult(new(4, 2));

        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.Find(".bit-mde-fni").Input("md");
        component.Find(".bit-mde-fni").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Contains("2 of 4", component.Find(".bit-mde-fns").TextContent);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldReportNoMatchesInTheFindPanel()
    {
        Context.JSInterop.Setup<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.find", _ => true).SetResult(BitMarkdownEditorFindResult.None);

        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.Find(".bit-mde-fni").Input("nothing");
        component.Find(".bit-mde-fni").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Contains("No results", component.Find(".bit-mde-fns").TextContent);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldToggleMatchCaseInTheFindPanel()
    {
        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        var button = component.Find(".bit-mde-fnc");
        Assert.AreEqual("false", button.GetAttribute("aria-pressed"));

        button.Click();

        Assert.AreEqual("true", component.Find(".bit-mde-fnc").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldReplaceThroughTheFindPanel()
    {
        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.Find(".bit-mde-fni").Input("old");
        component.FindAll(".bit-mde-fni")[1].Input("new");

        var buttons = component.FindAll(".bit-mde-fnb");
        buttons[^2].Click(); // Replace
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.replaceOne");

        component.FindAll(".bit-mde-fnb")[^1].Click(); // All
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.replaceAll");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldListEveryShortcutInTheHelpPanel()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=help]").Click();

        var dialog = component.Find(".bit-mde-hcr");
        Assert.AreEqual("dialog", dialog.GetAttribute("role"));
        Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
        // Built from one list in C#, so a shortcut can never be documented in only one place.
        Assert.IsTrue(component.FindAll(".bit-mde-hcr dl > div").Count >= 20);
        Assert.Contains("Alt + ↑ / ↓", dialog.TextContent);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldCallTheJsApiOfEveryPublicMethod()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.AutoSaveId, "draft-key");
        });

        await component.Instance.Insert("text");
        await component.Instance.Replace("a", "b");
        await component.Instance.FindNext("a");
        await component.Instance.FindPrevious("a");
        await component.Instance.GetSelection();
        await component.Instance.SetSelection(1, 2);
        await component.Instance.ClearDraft();
        await component.Instance.Focus();
        await component.Instance.Blur();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.insert");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.replaceAll");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.find", 2);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.getSelection");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.setSelection");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.clearDraft");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.focus");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.blur");
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldNotMutateTheTextWhenReadOnly()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.AutoSaveId, "draft-key");
        });

        await component.Instance.Insert("text");
        await component.Instance.Run(BitMarkdownEditorCommand.Bold);
        await component.Instance.Undo();
        await component.Instance.Redo();
        var replaced = await component.Instance.Replace("a", "b");

        Assert.AreEqual(0, replaced);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.insert"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.run"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.undo"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.redo"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.replaceAll"].Count);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldNotClearADraftWithoutAnAutoSaveId()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        await component.Instance.ClearDraft();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.clearDraft"].Count);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldPushConfigChangesToTheScript()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        // The whole config only travels with init, so a later change must be pushed across.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.setConfig"].Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.SyncScroll, false);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.setConfig", 1);

        // An unrelated parameter leaves the config alone.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Placeholder, "hi");
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.setConfig", 1);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNotAskForSelectionReportsWithoutCommandButtons()
    {
        // Reporting the caret's formatting is a round trip per caret move; it is only worth
        // it while something on screen can light up because of it.
        RenderComponent<BitMarkdownEditor>();
        Assert.IsTrue(LastInitConfigFlag("ReportSelection"));

        RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, false);
        });
        Assert.IsFalse(LastInitConfigFlag("ReportSelection"));

        // A toolbar of nothing but chrome buttons has nothing to highlight either.
        RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, new BitMarkdownEditorToolbarItem[]
            {
                new() { Name = "help", Title = "Help", Type = BitMarkdownEditorToolbarItemType.Help }
            });
        });
        Assert.IsFalse(LastInitConfigFlag("ReportSelection"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldSendEveryConfiguredOptionToTheScript()
    {
        RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.AutoPair, false);
            parameters.Add(p => p.TabIndents, false);
            parameters.Add(p => p.MaxLength, 42);
        });

        Assert.IsTrue(LastInitConfigFlag("AutoFocus"));
        Assert.IsFalse(LastInitConfigFlag("AutoPair"));
        Assert.IsFalse(LastInitConfigFlag("TabIndents"));
        Assert.AreEqual(42, LastInitConfigValue("MaxLength"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldSendTheImageLimitsToTheScript()
    {
        RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.MaxImageSize, 2048L);
            parameters.Add(p => p.AcceptedImageTypes, "image/png,image/jpeg");
            parameters.Add(p => p.Texts, new BitMarkdownEditorTexts { UploadingText = "بارگذاری" });
        });

        Assert.AreEqual(2048L, LastInitConfigValue("MaxImageSize"));
        Assert.AreEqual("image/png,image/jpeg", LastInitConfigValue("ImageAccept"));
        Assert.AreEqual("بارگذاری", LastInitConfigValue("UploadingText"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldReportRejectedImages()
    {
        BitMarkdownEditorImageRejection? rejected = null;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnImageRejected, EventCallback.Factory.Create<BitMarkdownEditorImageRejection>(this, r => rejected = r));
        });

        await component.Instance._OnImageRejected("huge.png", "image/png", 9_999_999, "Size");

        Assert.IsNotNull(rejected);
        Assert.AreEqual("huge.png", rejected.Value.FileName);
        Assert.AreEqual(9_999_999, rejected.Value.Size);
        Assert.AreEqual(BitMarkdownEditorImageRejectionReason.Size, rejected.Value.Reason);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldFallBackToATypeRejectionForAnUnknownReason()
    {
        BitMarkdownEditorImageRejection? rejected = null;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnImageRejected, EventCallback.Factory.Create<BitMarkdownEditorImageRejection>(this, r => rejected = r));
        });

        await component.Instance._OnImageRejected("a.bmp", "image/bmp", 10, "something-else");

        Assert.IsNotNull(rejected);
        Assert.AreEqual(BitMarkdownEditorImageRejectionReason.Type, rejected.Value.Reason);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldCountCjkCharactersAsWords()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceTime, 0);
        });

        // Chinese carries no spaces, so splitting on whitespace would report one word.
        await component.Instance._OnChange("中文没有空格");
        Assert.Contains("6 words", component.Find(".bit-mde-sbr").TextContent);

        // Latin words still group, and the two scripts add up when they are mixed.
        await component.Instance._OnChange("hello 世界");
        Assert.Contains("3 words", component.Find(".bit-mde-sbr").TextContent);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldCountSurrogatePairsOnce()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceTime, 0);
        });

        await component.Instance._OnChange("👋 🌍");

        Assert.Contains("2 words", component.Find(".bit-mde-sbr").TextContent);
        Assert.Contains("3 chars", component.Find(".bit-mde-sbr").TextContent);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldOnlyReportAriaPressedForTogglingCommands()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        // Bold can be "on" at the caret; a table is inserted, never toggled.
        Assert.AreEqual("false", component.Find("[data-cmd=bold]").GetAttribute("aria-pressed"));
        Assert.AreEqual("false", component.Find("[data-cmd=quote]").GetAttribute("aria-pressed"));
        Assert.IsFalse(component.Find("[data-cmd=table]").HasAttribute("aria-pressed"));
        Assert.IsFalse(component.Find("[data-cmd=hr]").HasAttribute("aria-pressed"));
        Assert.IsFalse(component.Find("[data-cmd=link]").HasAttribute("aria-pressed"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldReturnTheFocusWhenTheFindPanelIsClosed()
    {
        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.Find(".bit-mde-fnd .bit-mde-hcl").Click();

        Assert.AreEqual(0, component.FindAll(".bit-mde-fnd").Count);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.focus");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldInvokeFocusAndBlurCallbacks()
    {
        var focused = false;
        var blurred = false;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnFocus, EventCallback.Factory.Create(this, () => focused = true));
            parameters.Add(p => p.OnBlur, EventCallback.Factory.Create(this, () => blurred = true));
        });

        component.Find(".bit-mde-txa").Focus();
        component.Find(".bit-mde-txa").Blur();

        Assert.IsTrue(focused);
        Assert.IsTrue(blurred);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldNotReportAReadingTimeForAnEmptyDocument()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceTime, 0);
            parameters.Add(p => p.ShowReadingTime, true);
        });

        Assert.Contains("0 min read", component.Find(".bit-mde-sbr").TextContent);

        await component.Instance._OnChange("one word");

        Assert.Contains("1 min read", component.Find(".bit-mde-sbr").TextContent);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldFallBackToTheDefaultIndentUnit()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.IndentUnit, string.Empty);
        });

        // An empty indent unit would otherwise report a change that changed nothing.
        var result = component.Instance._ApplyCommand("Indent", 0, 0, "text");

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("  text", result.Text);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldHoldThePageStillWhileFullScreen()
    {
        var fullScreen = false;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Bind(p => p.FullScreen, fullScreen, v => fullScreen = v);
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);

        component.Find("[data-cmd=fullscreen]").Click();
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.lockScroll", 1);

        component.Find("[data-cmd=fullscreen]").Click();
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.unlockScroll", 1);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldReleaseThePageOnDisposal()
    {
        var fullScreen = true;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Bind(p => p.FullScreen, fullScreen, v => fullScreen = v);
        });

        // A full-screen editor takes the hold on its first render.
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.lockScroll", 1);

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.unlockScroll", 1);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldDropAStaleMatchCountWhileTheTermIsRetyped()
    {
        Context.JSInterop.Setup<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.find", _ => true).SetResult(new(3, 1));

        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.Find(".bit-mde-fni").Input("md");
        component.Find(".bit-mde-fni").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Contains("1 of 3", component.Find(".bit-mde-fns").TextContent);

        // The count described the old term, so it must not stay on screen for the new one.
        component.Find(".bit-mde-fni").Input("mdx");

        Assert.AreEqual(string.Empty, component.Find(".bit-mde-fns").TextContent.Trim());
    }

    [TestMethod]
    public void BitMarkdownEditorShouldReturnTheFocusWhenTheHelpDialogIsClosed()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=help]").Click();
        Assert.AreEqual(1, component.FindAll(".bit-mde-hcr").Count);

        component.Find(".bit-mde-hcr .bit-mde-hcl").Click();

        Assert.AreEqual(0, component.FindAll(".bit-mde-hcr").Count);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.focus");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldPassTheTableSizeToTheCommands()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.TableColumns, 3);
            parameters.Add(p => p.TableRows, 2);
        });

        var result = component.Instance._ApplyCommand("Table", 0, 0, string.Empty);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual(4, result.Text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("| Column 3 |", result.Text);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNotRenderThePreviewInEditMode()
    {
        // Parsing markdown nobody can see would re-render the whole document per keystroke.
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Mode, BitMarkdownEditorMode.Edit);
            parameters.Add(p => p.DefaultValue, "# a heading");
        });

        Assert.AreEqual(0, component.FindAll(".bit-mde-ppn *").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Mode, BitMarkdownEditorMode.Split);
        });

        Assert.IsTrue(component.FindAll(".bit-mde-ppn *").Count > 0);
    }

    private bool LastInitConfigFlag(string property) => (bool)LastInitConfigValue(property)!;

    private object? LastInitConfigValue(string property)
    {
        var config = Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.init"].Last().Arguments[5];

        return config!.GetType().GetProperty(property)!.GetValue(config);
    }
}
