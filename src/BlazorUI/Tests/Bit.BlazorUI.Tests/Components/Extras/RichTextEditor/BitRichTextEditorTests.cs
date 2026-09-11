using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

[TestClass]
public class BitRichTextEditorTests : BunitTestContext
{
    private void SetupJsInterop()
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.initialize");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.updateOptions");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.enableToolbarRoving");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setHtml");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.exec");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.execBlock");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.focus");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.dispose");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.createLink");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.updateLink");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.insertImageUrl");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.updateImage");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.insertHtml");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.alignImage");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.insertText");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.insertTable");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.tableOp");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.clearFind");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.selectAll");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setBlockDirection");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.applyColor");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.clearColor");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.applyFont");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.applySlashCommand");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.applyMention");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.bindSlashKeys");
        Context.JSInterop.SetupVoid("BitBlazorUI.RichTextEditor.setFullScreen");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getHtml", _ => true).SetResult("<p>html</p>");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getText", _ => true).SetResult("text");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.getSelectedText", _ => true).SetResult("selected");
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.htmlToText", _ => true).SetResult("source text");
        Context.JSInterop.Setup<bool>("BitBlazorUI.RichTextEditor.insertMedia", _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>("BitBlazorUI.RichTextEditor.validateHtml", _ => true).SetResult(true);
        Context.JSInterop.Setup<int>("BitBlazorUI.RichTextEditor.find", _ => true).SetResult(3);
        Context.JSInterop.Setup<int>("BitBlazorUI.RichTextEditor.findStep", _ => true).SetResult(2);
        Context.JSInterop.Setup<int>("BitBlazorUI.RichTextEditor.replaceCurrent", _ => true).SetResult(2);
        Context.JSInterop.Setup<int>("BitBlazorUI.RichTextEditor.replaceAll", _ => true).SetResult(3);
    }

    // The setup payload is an internal type, so its values are read reflectively rather than by
    // referencing the type from the test assembly.
    private static object? SetupOption(object options, string name)
        => options.GetType().GetProperty(name)?.GetValue(options);

    private int InvokeCount(string identifier) => Context.JSInterop.Invocations[identifier].Count;

    private object LastSetupOptions()
        => Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.initialize"].Last().Arguments[2]!;

    private static IElement ButtonByLabel(IRenderedComponent<BitRichTextEditor> component, string ariaLabel)
        => component.Find($"button[aria-label='{ariaLabel}']");

    private static IElement ButtonByText(IRenderedComponent<BitRichTextEditor> component, string selector, string text)
        => component.FindAll(selector).First(b => b.TextContent.Trim() == text);


    // ---------------------------------------------------------------- rendering

    [TestMethod]
    public void BitRichTextEditorShouldRenderEditorAndToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        Assert.IsNotNull(component.Find(".bit-rte"));
        Assert.IsNotNull(component.Find(".bit-rte-edt"));
        Assert.IsNotNull(component.Find(".bit-rte-tlb"));

        // The editing surface must expose itself as a multiline textbox, or assistive technology
        // announces a plain group with no editing affordance.
        var editor = component.Find(".bit-rte-edt");
        Assert.AreEqual("textbox", editor.GetAttribute("role"));
        Assert.AreEqual("true", editor.GetAttribute("aria-multiline"));
        Assert.AreEqual("true", editor.GetAttribute("contenteditable"));
        Assert.AreEqual("toolbar", component.Find(".bit-rte-tlb").GetAttribute("role"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldHideToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, false);
        });

        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplyClassesAndReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitRichTextEditorClassStyles
            {
                Editor = "custom-editor",
                Toolbar = "custom-toolbar",
                Root = "custom-root",
                Group = "custom-group",
                Button = "custom-button",
                Count = "custom-count"
            });
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowCount, true);
        });

        var root = component.Find(".bit-rte");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-ro"));
        Assert.IsTrue(component.Find(".bit-rte-edt").ClassList.Contains("custom-editor"));
        Assert.IsTrue(component.Find(".bit-rte-tlb").ClassList.Contains("custom-toolbar"));
        Assert.IsTrue(component.Find(".bit-rte-grp").ClassList.Contains("custom-group"));
        Assert.IsTrue(component.Find(".bit-rte-btn").ClassList.Contains("custom-button"));
        Assert.IsTrue(component.Find(".bit-rte-cnt").ClassList.Contains("custom-count"));
        // Note: the Source hook only renders inside the HTML source-view textarea, which requires
        // toggling into source view (a JS-bridged action) and is covered separately.
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplyStyles()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Styles, new BitRichTextEditorClassStyles
            {
                Root = "border-color: red",
                Toolbar = "background: blue",
                Editor = "color: green"
            });
        });

        StringAssert.Contains(component.Find(".bit-rte").GetAttribute("style"), "border-color: red");
        StringAssert.Contains(component.Find(".bit-rte-tlb").GetAttribute("style"), "background: blue");
        StringAssert.Contains(component.Find(".bit-rte-edt").GetAttribute("style"), "color: green");
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderPlaceholder()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Type here");
        });

        Assert.AreEqual("Type here", component.Find(".bit-rte-edt").GetAttribute("data-placeholder"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplyHeightAndMaxHeight()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Height, "12rem");
            parameters.Add(p => p.MaxHeight, "20rem");
        });

        var style = component.Find(".bit-rte-edt").GetAttribute("style");
        StringAssert.Contains(style, "min-height:12rem");
        StringAssert.Contains(style, "max-height:20rem");
    }

    [TestMethod]
    public void BitRichTextEditorShouldOmitMaxHeightWhenNotSet()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        // An unset MaxHeight must not leak an empty/invalid declaration into the style attribute.
        Assert.IsFalse(component.Find(".bit-rte-edt").GetAttribute("style")!.Contains("max-height"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldApplySpellCheck()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();
        Assert.AreEqual("true", component.Find(".bit-rte-edt").GetAttribute("spellcheck"));

        component.Render(parameters => parameters.Add(p => p.SpellCheck, false));
        Assert.AreEqual("false", component.Find(".bit-rte-edt").GetAttribute("spellcheck"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldUseAriaLabelForTheEditingSurface()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Article body");
        });

        // AriaLabel has to name the editing surface (the thing focus lands on), not the wrapper.
        Assert.AreEqual("Article body", component.Find(".bit-rte-edt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldFallBackToTheDefaultEditorLabel()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        Assert.AreEqual("Rich text editor", component.Find(".bit-rte-edt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldDescribeTheSurfaceByTheCountFooter()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowCount, true);
        });

        var describedBy = component.Find(".bit-rte-edt").GetAttribute("aria-describedby");
        Assert.IsFalse(string.IsNullOrEmpty(describedBy));
        Assert.AreEqual(describedBy, component.Find(".bit-rte-cnt").GetAttribute("id"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldNotDescribeTheSurfaceWithoutACountFooter()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        Assert.IsNull(component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));
    }


    // ---------------------------------------------------------------- disabled / readonly

    [TestMethod]
    public void BitRichTextEditorShouldLockTheSurfaceWhenReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
        });

        var editor = component.Find(".bit-rte-edt");
        Assert.AreEqual("false", editor.GetAttribute("contenteditable"));
        Assert.AreEqual("true", editor.GetAttribute("aria-readonly"));
        Assert.IsTrue(component.FindAll(".bit-rte-tlb .bit-rte-btn").All(b => b.HasAttribute("disabled")));
    }

    [TestMethod]
    public void BitRichTextEditorShouldLockTheSurfaceWhenDisabled()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
        });

        // IsEnabled=false must lock editing exactly like ReadOnly; painting the disabled class
        // while leaving the surface editable would let a "disabled" editor be typed into.
        var root = component.Find(".bit-rte");
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("bit-rte-ro"));

        var editor = component.Find(".bit-rte-edt");
        Assert.AreEqual("false", editor.GetAttribute("contenteditable"));
        Assert.AreEqual("true", editor.GetAttribute("aria-readonly"));
        Assert.IsTrue(component.FindAll(".bit-rte-tlb .bit-rte-btn").All(b => b.HasAttribute("disabled")));
    }

    [TestMethod]
    public void BitRichTextEditorShouldTellTheBridgeAboutTheEffectiveReadOnlyState()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        // The bridge refuses paste/drop/typing on this flag, so it must reflect IsEnabled too and
        // not only the ReadOnly parameter.
        Assert.AreEqual(true, SetupOption(LastSetupOptions(), "ReadOnly"));
        Assert.AreEqual(0, component.FindAll(".bit-rte-src").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNotRunCommandsWhileReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync("bold"));

        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.exec"));
    }


    // ---------------------------------------------------------------- toolbar composition

    [TestMethod]
    public void BitRichTextEditorShouldRenderToolbarGroups()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
        });

        // The Inline group must render as a single toolbar group of exactly five toggle buttons -
        // bold, italic, underline, strikethrough, inline code (in that order) - with no other
        // groups or non-button controls (color/font selectors, url/find inputs) leaking in.
        Assert.AreEqual(1, component.FindAll(".bit-rte-tlb .bit-rte-grp").Count);

        var buttons = component.FindAll(".bit-rte-tlb .bit-rte-btn");
        Assert.AreEqual(5, buttons.Count);
        CollectionAssert.AreEqual(
            new[] { "Bold", "Italic", "Underline", "Strikethrough", "Inline code" },
            buttons.Select(b => b.GetAttribute("aria-label")).ToArray());
        foreach (var button in buttons)
        {
            Assert.AreEqual("button", button.GetAttribute("type"));
        }

        // No selectors, color pickers, or text inputs belong to the inline-only toolbar.
        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb input").Count);
        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb select").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderNoGroupsForToolbarNone()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.None);
        });

        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb .bit-rte-grp").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldOfferEveryHeadingLevelAndBlockFormat()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.BlockFormat);
        });

        var options = component.FindAll(".bit-rte-sel option").Select(o => o.GetAttribute("value")).ToArray();
        CollectionAssert.AreEqual(
            new[] { "p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre" },
            options);
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderJustifyInTheAlignmentGroup()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Alignment);
        });

        CollectionAssert.AreEqual(
            new[] { "Align left", "Align center", "Align right", "Justify" },
            component.FindAll(".bit-rte-btn").Select(b => b.GetAttribute("aria-label")).ToArray());
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderTaskListInTheListsGroup()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Lists);
        });

        CollectionAssert.AreEqual(
            new[] { "Bullet list", "Numbered list", "Task list" },
            component.FindAll(".bit-rte-btn").Select(b => b.GetAttribute("aria-label")).ToArray());
    }

    [TestMethod]
    public void BitRichTextEditorShouldDisableTableOperationsOutsideATable()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Table);
        });

        // Only "Insert table" works with the caret outside a table; the structural operations have
        // nothing to act on, so they must be disabled rather than silently no-op.
        Assert.IsFalse(ButtonByLabel(component, "Insert table").HasAttribute("disabled"));
        Assert.IsTrue(ButtonByLabel(component, "Delete row").HasAttribute("disabled"));
        Assert.IsTrue(ButtonByLabel(component, "Split cell").HasAttribute("disabled"));
        Assert.IsTrue(ButtonByLabel(component, "Delete table").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldEnableTableOperationsInsideATable()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Table);
        });

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { InTable = true })).Wait();

        Assert.IsFalse(ButtonByLabel(component, "Delete row").HasAttribute("disabled"));
        Assert.IsFalse(ButtonByLabel(component, "Merge cells").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldOrderToolbarEntries()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.History | BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists);
            parameters.Add(p => p.ToolbarConfig, new BitRichTextEditorToolbarConfig
            {
                // "nope" is unknown and must be skipped; History is omitted and must be appended.
                Order = [BitRichTextEditorToolbarConfig.GroupIds.Lists, "nope", BitRichTextEditorToolbarConfig.GroupIds.Inline]
            });
        });

        var firstButtonOfEachGroup = component.FindAll(".bit-rte-grp")
            .Select(g => g.QuerySelector("button")!.GetAttribute("aria-label"))
            .ToArray();

        CollectionAssert.AreEqual(new[] { "Bullet list", "Bold", "Undo" }, firstButtonOfEachGroup);
    }

    [TestMethod]
    public void BitRichTextEditorShouldRenderCustomToolbarItemsLast()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
            parameters.Add(p => p.ToolbarConfig, new BitRichTextEditorToolbarConfig
            {
                CustomItems =
                [
                    new() { Id = "today", Label = "Today", AriaLabel = "Insert today", OnActivate = _ => Task.CompletedTask }
                ]
            });
        });

        var last = component.FindAll(".bit-rte-tlb .bit-rte-btn").Last();
        Assert.AreEqual("Insert today", last.GetAttribute("aria-label"));
        Assert.AreEqual("Today", last.TextContent);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldReportAFailingCustomToolbarAction()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.None);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
            parameters.Add(p => p.ToolbarConfig, new BitRichTextEditorToolbarConfig
            {
                CustomItems =
                [
                    new() { Id = "boom", Label = "Boom", OnActivate = _ => throw new InvalidOperationException("nope") }
                ]
            });
        });

        await ButtonByLabel(component, "Boom").ClickAsync(new());

        // A host callback that throws must surface as a recoverable editor error, not tear the
        // component down or leak the exception detail into the message.
        Assert.IsNotNull(error);
        Assert.AreEqual("custom-action-failed", error!.Code);
        Assert.IsFalse(error.Message.Contains("nope"));
        Assert.IsNotNull(component.Find(".bit-rte-err"));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void BitRichTextEditorShouldRejectABlankCustomItemId(string id)
    {
        SetupJsInterop();

        Assert.ThrowsExactly<ArgumentException>(() => RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ToolbarConfig, new BitRichTextEditorToolbarConfig
            {
                CustomItems = [new() { Id = id, Label = "X", OnActivate = _ => Task.CompletedTask }]
            });
        }));
    }

    [TestMethod]
    public void BitRichTextEditorShouldRejectDuplicateCustomItemIds()
    {
        SetupJsInterop();

        Assert.ThrowsExactly<ArgumentException>(() => RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ToolbarConfig, new BitRichTextEditorToolbarConfig
            {
                CustomItems =
                [
                    new() { Id = "dup", Label = "A", OnActivate = _ => Task.CompletedTask },
                    new() { Id = "DUP", Label = "B", OnActivate = _ => Task.CompletedTask }
                ]
            });
        }));
    }

    [TestMethod]
    public void BitRichTextEditorShouldRejectACustomItemIdThatShadowsABuiltInGroup()
    {
        SetupJsInterop();

        Assert.ThrowsExactly<ArgumentException>(() => RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ToolbarConfig, new BitRichTextEditorToolbarConfig
            {
                CustomItems =
                [
                    new() { Id = BitRichTextEditorToolbarConfig.GroupIds.History, Label = "X", OnActivate = _ => Task.CompletedTask }
                ]
            });
        }));
    }

    [TestMethod]
    public void BitRichTextEditorShouldRejectACustomItemWithoutAName()
    {
        SetupJsInterop();

        Assert.ThrowsExactly<ArgumentException>(() => RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ToolbarConfig, new BitRichTextEditorToolbarConfig
            {
                CustomItems = [new() { Id = "x", OnActivate = _ => Task.CompletedTask }]
            });
        }));
    }


    // ---------------------------------------------------------------- selection state

    [TestMethod]
    public void BitRichTextEditorShouldReflectTheSelectionStateOnToggleButtons()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
        });

        Assert.AreEqual("false", ButtonByLabel(component, "Bold").GetAttribute("aria-pressed"));

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { Bold = true, InlineCode = true })).Wait();

        var bold = ButtonByLabel(component, "Bold");
        Assert.AreEqual("true", bold.GetAttribute("aria-pressed"));
        Assert.IsTrue(bold.ClassList.Contains("bit-rte-act"));
        Assert.IsTrue(ButtonByLabel(component, "Inline code").ClassList.Contains("bit-rte-act"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldShowTheActiveColorsInThePickers()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Color);
        });

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { ForeColor = "#123456", BackColor = "#abcdef" })).Wait();

        var pickers = component.FindAll("input[type=color]");
        Assert.AreEqual("#123456", pickers[0].GetAttribute("value"));
        Assert.AreEqual("#abcdef", pickers[1].GetAttribute("value"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldPreselectTheActiveFont()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Font);
            parameters.Add(p => p.FontFamilies, new[] { "Georgia", "Verdana" });
            parameters.Add(p => p.FontSizes, new[] { "12px", "16px" });
        });

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { FontName = "Georgia", FontSize = "16px" })).Wait();

        var selects = component.FindAll(".bit-rte-sel");
        Assert.AreEqual("Georgia", selects[0].GetAttribute("value"));
        Assert.AreEqual("16px", selects[1].GetAttribute("value"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldUseTheSuppliedFontOptions()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Font);
            parameters.Add(p => p.FontFamilies, new[] { "Georgia" });
            parameters.Add(p => p.FontSizes, new[] { "42px" });
        });

        var values = component.FindAll(".bit-rte-sel option").Select(o => o.GetAttribute("value")).ToArray();
        CollectionAssert.AreEqual(new[] { "", "Georgia", "", "42px" }, values);
    }


    // ---------------------------------------------------------------- links

    [TestMethod]
    public async Task BitRichTextEditorShouldCreateALinkFromTheLinkPanel()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = "https://example.com" });
        await component.Find(".bit-rte-bar input[type=text]").InputAsync(new() { Value = "Example" });
        await component.Find(".bit-rte-bar input[type=checkbox]").ChangeAsync(new() { Value = true });
        await ButtonByText(component, ".bit-rte-bar button", "Apply").ClickAsync(new());

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.createLink");
        Assert.AreEqual("https://example.com", invocation.Arguments[1]);
        Assert.AreEqual(true, invocation.Arguments[2]);
        Assert.AreEqual("Example", invocation.Arguments[3]);
        // The panel closes once the link is applied.
        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
    }

    [DataTestMethod]
    [DataRow("example.com", "https://example.com")]
    [DataRow("example.com/docs", "https://example.com/docs")]
    [DataRow("example.com:8443", "https://example.com:8443")]
    [DataRow("example.com:8443/docs", "https://example.com:8443/docs")]
    [DataRow("https://example.com", "https://example.com")]
    [DataRow("/relative/path", "/relative/path")]
    [DataRow("#anchor", "#anchor")]
    [DataRow("mailto:a@b.com", "mailto:a@b.com")]
    public async Task BitRichTextEditorShouldNormalizeAcceptedLinkUrls(string typed, string expected)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = typed });
        await ButtonByText(component, ".bit-rte-bar button", "Apply").ClickAsync(new());

        Assert.AreEqual(expected, Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.createLink").Arguments[1]);
    }

    [DataTestMethod]
    [DataRow("javascript:alert(1)")]
    [DataRow("//evil.example.com")]
    [DataRow("\\\\evil.example.com")]
    [DataRow("/\\evil.example.com")]
    [DataRow("ftp://example.com")]
    [DataRow("   ")]
    public async Task BitRichTextEditorShouldRejectUnsafeLinkUrls(string typed)
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = typed });
        await ButtonByText(component, ".bit-rte-bar button", "Apply").ClickAsync(new());

        Assert.IsNotNull(error);
        Assert.AreEqual("invalid-url", error!.Code);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.createLink"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldUpdateAnExistingLinkInsteadOfCreatingOne()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { InLink = true, LinkHref = "https://old.example.com" }));

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        // The panel prefills the existing href and offers Remove instead of a link-text field.
        Assert.AreEqual("https://old.example.com", component.Find(".bit-rte-bar input[type=url]").GetAttribute("value"));
        Assert.AreEqual(0, component.FindAll(".bit-rte-bar input[type=text]").Count);

        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = "https://new.example.com" });
        await ButtonByText(component, ".bit-rte-bar button", "Apply").ClickAsync(new());

        Assert.AreEqual("https://new.example.com",
            Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.updateLink").Arguments[1]);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.createLink"));
    }


    // ---------------------------------------------------------------- images

    [TestMethod]
    public async Task BitRichTextEditorShouldInsertAnImageWithItsAlternativeText()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
        });

        await ButtonByLabel(component, "Insert image").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = "https://example.com/a.png" });
        await component.Find(".bit-rte-bar input[type=text]").InputAsync(new() { Value = "A picture" });
        await ButtonByText(component, ".bit-rte-bar button", "Insert").ClickAsync(new());

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertImageUrl");
        Assert.AreEqual("https://example.com/a.png", invocation.Arguments[1]);
        Assert.AreEqual("A picture", invocation.Arguments[2]);
    }

    [DataTestMethod]
    [DataRow("javascript:alert(1)")]
    [DataRow("ftp://example.com/a.png")]
    [DataRow("data:text/html;base64,AAAA")]
    [DataRow("data:image/pngfoo,AAAA")]
    public async Task BitRichTextEditorShouldRejectUnsafeImageUrls(string typed)
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await ButtonByLabel(component, "Insert image").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = typed });
        await ButtonByText(component, ".bit-rte-bar button", "Insert").ClickAsync(new());

        Assert.IsNotNull(error);
        Assert.AreEqual("invalid-url", error!.Code);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.insertImageUrl"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldAcceptADataImageUrl()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
        });

        await ButtonByLabel(component, "Insert image").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = "data:image/png;base64,AAAA" });
        await ButtonByText(component, ".bit-rte-bar button", "Insert").ClickAsync(new());

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertImageUrl");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldEmbedADroppedImageAsADataUrlWithoutAnUploadCallback()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        var url = await component.InvokeAsync(() =>
            component.Instance._ResolveImageUrl("a.png", "image/png", "AAAA"));

        Assert.AreEqual("data:image/png;base64,AAAA", url);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNormalizeTheReportedImageMimeType()
    {
        SetupJsInterop();

        BitRichTextEditorImageUpload? received = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnImageUpload, new Func<BitRichTextEditorImageUpload, Task<string?>>(u =>
            {
                received = u;
                return Task.FromResult<string?>("https://cdn.example.com/a.png");
            }));
        });

        var url = await component.InvokeAsync(() =>
            component.Instance._ResolveImageUrl("a.png", "IMAGE/PNG; charset=utf-8", "AAAA"));

        // The client-reported content type is never handed on verbatim: it is matched against the
        // known set and passed through in its canonical form.
        Assert.AreEqual("https://cdn.example.com/a.png", url);
        Assert.IsNotNull(received);
        Assert.AreEqual("image/png", received!.ContentType);
        Assert.AreEqual("a.png", received.FileName);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldRejectAnUnsupportedImageMimeType()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        var url = await component.InvokeAsync(() =>
            component.Instance._ResolveImageUrl("a.exe", "application/octet-stream", "AAAA"));

        Assert.IsNull(url);
        Assert.IsNotNull(error);
        Assert.AreEqual("invalid-image", error!.Code);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldRejectAnOversizedImage()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        // Just past the 10 MB limit once the base64 padding is accounted for.
        var oversized = new string('A', 4 * (10 * 1024 * 1024 / 3 + 16));
        var url = await component.InvokeAsync(() =>
            component.Instance._ResolveImageUrl("big.png", "image/png", oversized));

        Assert.IsNull(url);
        Assert.IsNotNull(error);
        Assert.AreEqual("file-too-large", error!.Code);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldReportAnUploadThatReturnsNoUrl()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
            parameters.Add(p => p.OnImageUpload, new Func<BitRichTextEditorImageUpload, Task<string?>>(
                _ => Task.FromResult<string?>(null)));
        });

        var url = await component.InvokeAsync(() =>
            component.Instance._ResolveImageUrl("a.png", "image/png", "AAAA"));

        Assert.IsNull(url);
        Assert.IsNotNull(error);
        Assert.AreEqual("upload-failed", error!.Code);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldReportAThrowingUploadWithoutLeakingDetail()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
            parameters.Add(p => p.OnImageUpload, new Func<BitRichTextEditorImageUpload, Task<string?>>(
                _ => throw new InvalidOperationException("storage credentials")));
        });

        var url = await component.InvokeAsync(() =>
            component.Instance._ResolveImageUrl("a.png", "image/png", "AAAA"));

        Assert.IsNull(url);
        Assert.IsNotNull(error);
        Assert.AreEqual("upload-failed", error!.Code);
        Assert.IsFalse(error.Message.Contains("credentials"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldRefuseDataImageUrlsWhenThePolicyForbidsThem()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("");

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.SanitizationPolicy, new BitRichTextEditorSanitizationPolicy
            {
                AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "p" },
                AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase),
                AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https" },
                AllowDataImageUris = false
            });
        });

        var url = await component.InvokeAsync(() =>
            component.Instance._ResolveImageUrl("a.png", "image/png", "AAAA"));

        Assert.IsNull(url);
    }

    [TestMethod]
    public void BitRichTextEditorImageUploadShouldCopyItsContent()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var upload = new BitRichTextEditorImageUpload("a.png", "image/png", bytes);

        bytes[0] = 9;

        // The payload is defensively copied in both directions, so neither the caller's array nor
        // a consumer's copy can mutate what the upload carries.
        Assert.AreEqual(1, upload.Content[0]);
        upload.Content[0] = 8;
        Assert.AreEqual(1, upload.Content[0]);
    }

    [TestMethod]
    public void BitRichTextEditorImageUploadShouldRejectNullMembers()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BitRichTextEditorImageUpload(null!, "image/png", []));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BitRichTextEditorImageUpload("a.png", null!, []));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BitRichTextEditorImageUpload("a.png", "image/png", null!));
    }


    // ---------------------------------------------------------------- media embeds

    [DataTestMethod]
    [DataRow("https://www.youtube.com/watch?v=abc123", "youtube-nocookie.com/embed/abc123")]
    [DataRow("https://youtu.be/abc123", "youtube-nocookie.com/embed/abc123")]
    [DataRow("https://www.youtube.com/embed/abc123", "youtube-nocookie.com/embed/abc123")]
    [DataRow("https://vimeo.com/12345", "player.vimeo.com/video/12345")]
    [DataRow("https://cdn.example.com/clip.mp4", "<video")]
    [DataRow("https://cdn.example.com/song.mp3", "<audio")]
    public async Task BitRichTextEditorShouldBuildTheExpectedMediaEmbed(string url, string expected)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Media);
        });

        await ButtonByLabel(component, "Embed media").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = url });
        await ButtonByText(component, ".bit-rte-bar button", "Embed").ClickAsync(new());

        var html = (string)Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertMedia").Arguments[1]!;
        StringAssert.Contains(html, expected);
    }

    [DataTestMethod]
    [DataRow("https://youtube.com.evil.test/watch?v=abc123", "media-not-allowed")]
    [DataRow("https://example.com/page", "media-not-allowed")]
    [DataRow("ftp://example.com/clip.mp4", "invalid-url")]
    [DataRow("not a url", "invalid-url")]
    public async Task BitRichTextEditorShouldRejectAnUnsupportedMediaUrl(string url, string expectedCode)
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Media);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await ButtonByLabel(component, "Embed media").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = url });
        await ButtonByText(component, ".bit-rte-bar button", "Embed").ClickAsync(new());

        Assert.IsNotNull(error);
        Assert.AreEqual(expectedCode, error!.Code);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.insertMedia"));
    }


    // ---------------------------------------------------------------- tables

    [TestMethod]
    public async Task BitRichTextEditorShouldInsertATableOfTheChosenSize()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Table);
        });

        await ButtonByLabel(component, "Insert table").ClickAsync(new());
        await component.FindAll(".bit-rte-num")[0].InputAsync(new() { Value = "4" });
        await component.FindAll(".bit-rte-num")[1].InputAsync(new() { Value = "3" });
        await ButtonByText(component, ".bit-rte-bar button", "Insert").ClickAsync(new());

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertTable");
        Assert.AreEqual(4, invocation.Arguments[1]);
        Assert.AreEqual(3, invocation.Arguments[2]);
        Assert.AreEqual(true, invocation.Arguments[3]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldRejectAnOutOfRangeTableSize()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Table);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await ButtonByLabel(component, "Insert table").ClickAsync(new());
        await component.FindAll(".bit-rte-num")[0].InputAsync(new() { Value = "500" });
        await ButtonByText(component, ".bit-rte-bar button", "Insert").ClickAsync(new());

        Assert.IsNotNull(error);
        Assert.AreEqual("invalid-table", error!.Code);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.insertTable"));
    }

    [DataTestMethod]
    [DataRow("Insert row above", "addRowBefore")]
    [DataRow("Insert row below", "addRow")]
    [DataRow("Insert column before", "addColBefore")]
    [DataRow("Insert column after", "addCol")]
    [DataRow("Delete row", "delRow")]
    [DataRow("Delete column", "delCol")]
    [DataRow("Toggle header row", "headerRow")]
    [DataRow("Merge cells", "merge")]
    [DataRow("Split cell", "split")]
    [DataRow("Delete table", "delTable")]
    public async Task BitRichTextEditorShouldForwardTheTableOperation(string label, string op)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Table);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { InTable = true }));

        await ButtonByLabel(component, label).ClickAsync(new());

        Assert.AreEqual(op, Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.tableOp").Arguments[1]);
    }


    // ---------------------------------------------------------------- find & replace

    [TestMethod]
    public async Task BitRichTextEditorShouldReportTheMatchPositionWhileStepping()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find);
        });

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=text]").InputAsync(new() { Value = "fox" });

        // The first search lands on match 1 of 3 (the bridge is stubbed to find three).
        Assert.AreEqual("1 of 3", component.Find(".bit-rte-find-count").TextContent.Trim());

        await ButtonByLabel(component, "Next match").ClickAsync(new());
        Assert.AreEqual("2 of 3", component.Find(".bit-rte-find-count").TextContent.Trim());

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.findStep");
        Assert.AreEqual(1, invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldForwardTheFindOptions()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find);
        });

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=text]").InputAsync(new() { Value = "fox" });
        await component.Find("input[aria-label='Match case']").ChangeAsync(new() { Value = true });
        await component.Find("input[aria-label='Whole word']").ChangeAsync(new() { Value = true });

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.find"].Last();
        Assert.AreEqual("fox", invocation.Arguments[1]);
        Assert.AreEqual(true, invocation.Arguments[2]);
        Assert.AreEqual(true, invocation.Arguments[3]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldReplaceAllMatches()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find);
        });

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.FindAll(".bit-rte-bar input[type=text]")[0].InputAsync(new() { Value = "fox" });
        await component.FindAll(".bit-rte-bar input[type=text]")[1].InputAsync(new() { Value = "cat" });
        await ButtonByText(component, ".bit-rte-bar button", "All").ClickAsync(new());

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.replaceAll");
        Assert.AreEqual("fox", invocation.Arguments[1]);
        Assert.AreEqual("cat", invocation.Arguments[2]);
        Assert.AreEqual("3 replaced", component.Find(".bit-rte-find-count").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldRejectAnOverlongSearchTerm()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=text]").InputAsync(new() { Value = new string('x', 1001) });

        Assert.IsNotNull(error);
        Assert.AreEqual("invalid-find", error!.Code);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.find"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldClearTheFindHighlightsWhenThePanelCloses()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find);
        });

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=text]").InputAsync(new() { Value = "fox" });
        await ButtonByLabel(component, "Find and replace").ClickAsync(new());

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.clearFind");
        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
    }


    // ---------------------------------------------------------------- emoji

    [TestMethod]
    public async Task BitRichTextEditorShouldFilterTheEmojiPickerByKeyword()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Emoji);
        });

        await ButtonByLabel(component, "Insert emoji").ClickAsync(new());
        Assert.IsTrue(component.FindAll(".bit-rte-emoji-head").Count > 1);

        await component.Find(".bit-rte-emoji-panel input").InputAsync(new() { Value = "rocket" });

        var shown = component.FindAll(".bit-rte-emoji");
        Assert.AreEqual(1, shown.Count);
        Assert.AreEqual("🚀", shown[0].TextContent);

        await shown[0].ClickAsync(new());
        Assert.AreEqual("🚀", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertText").Arguments[1]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldSayWhenNoEmojiMatches()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Emoji);
        });

        await ButtonByLabel(component, "Insert emoji").ClickAsync(new());
        await component.Find(".bit-rte-emoji-panel input").InputAsync(new() { Value = "zzzzzz" });

        Assert.AreEqual(0, component.FindAll(".bit-rte-emoji").Count);
        Assert.IsNotNull(component.Find(".bit-rte-emoji-empty"));
    }


    // ---------------------------------------------------------------- slash menu

    [TestMethod]
    public async Task BitRichTextEditorShouldOpenAndApplyASlashCommand()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.InvokeAsync(() => component.Instance._OnSlashTrigger());

        Assert.IsNotNull(component.Find(".bit-rte-slash"));
        var items = component.FindAll(".bit-rte-slash-item");
        Assert.AreEqual(10, items.Count);
        Assert.AreEqual("Heading 1", items[0].TextContent);

        await items[0].ClickAsync(new());

        Assert.AreEqual("h1", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.applySlashCommand").Arguments[1]);
        Assert.AreEqual(0, component.FindAll(".bit-rte-slash").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldFilterTheSlashMenuByKeyword()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.InvokeAsync(() => component.Instance._OnSlashTrigger());
        // "todo" is not in any label; it only reaches the task list through the keyword table.
        await component.Find(".bit-rte-slash input").InputAsync(new() { Value = "todo" });

        var items = component.FindAll(".bit-rte-slash-item");
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("Task list", items[0].TextContent);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNotOpenTheSlashMenuWhileReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        await component.InvokeAsync(() => component.Instance._OnSlashTrigger());

        Assert.AreEqual(0, component.FindAll(".bit-rte-slash").Count);
    }


    // ---------------------------------------------------------------- shortcuts

    [DataTestMethod]
    [DataRow("b", false, "bold")]
    [DataRow("i", false, "italic")]
    [DataRow("u", false, "underline")]
    [DataRow("z", false, "undo")]
    [DataRow("y", false, "redo")]
    [DataRow("x", true, "strikeThrough")]
    [DataRow("e", false, "inlineCode")]
    [DataRow("7", true, "insertOrderedList")]
    [DataRow("8", true, "insertUnorderedList")]
    [DataRow("9", true, "insertTaskList")]
    public async Task BitRichTextEditorShouldRunTheBuiltInShortcuts(string key, bool shift, string command)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        var handled = await component.InvokeAsync(() => component.Instance._OnShortcut(key, true, shift, false));

        Assert.IsTrue(handled);
        Assert.AreEqual(command, Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.exec").Arguments[1]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldOpenTheLinkPanelFromItsShortcut()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        var handled = await component.InvokeAsync(() => component.Instance._OnShortcut("k", true, false, false));

        // "link" is the one shortcut command that opens UI instead of running an editing command.
        Assert.IsTrue(handled);
        Assert.IsNotNull(component.Find(".bit-rte-bar input[type=url]"));
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.exec"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldOpenTheLinkPanelWithoutAToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, false);
            parameters.Add(p => p.ShowQuickToolbar, true);
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        var handled = await component.InvokeAsync(() => component.Instance._OnShortcut("k", true, false, false));

        // A chrome-free surface still needs to be able to link: the panel is opened by the shortcut
        // and the floating selection toolbar, not only by a toolbar button.
        Assert.IsTrue(handled);
        Assert.IsNotNull(component.Find(".bit-rte-bar input[type=url]"));
        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldOfferTheLinkButtonOnTheQuickToolbarWithoutAToolbar()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, false);
            parameters.Add(p => p.ShowQuickToolbar, true);
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState
        {
            HasSelection = true,
            SelectionTop = 90,
            SelectionLeft = 20,
            SelectionHeight = 18
        }));

        var quick = component.Find(".bit-rte-quick");
        Assert.IsNotNull(quick.QuerySelector("button[aria-label='Insert or edit link']"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNotClaimTheLinkShortcutWithoutTheLinkGroup()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
        });

        var handled = await component.InvokeAsync(() => component.Instance._OnShortcut("k", true, false, false));

        // The panel lives in the toolbar, so with the link group off the keystroke must fall
        // through to the browser instead of being swallowed for a panel that cannot appear.
        Assert.IsFalse(handled);
        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldPreferACustomShortcutOverTheBuiltInOne()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string> { ["CTRL+B"] = "italic" });
        });

        await component.InvokeAsync(() => component.Instance._OnShortcut("b", true, false, false));

        Assert.AreEqual("italic", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.exec").Arguments[1]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldReportAnUnknownShortcutCommand()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string> { ["ctrl+j"] = "launchMissiles" });
        });

        var handled = await component.InvokeAsync(() => component.Instance._OnShortcut("j", true, false, false));

        Assert.IsFalse(handled);
        Assert.IsNotNull(error);
        Assert.AreEqual("unknown-shortcut", error!.Code);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNotHandleShortcutsWhileReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        var handled = await component.InvokeAsync(() => component.Instance._OnShortcut("b", true, false, false));

        // Reporting it as handled would suppress the browser default for a command that never runs.
        Assert.IsFalse(handled);
    }

    [TestMethod]
    public void BitRichTextEditorShouldAdvertiseItsOwnedShortcutCombos()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string>
            {
                ["ctrl+j"] = "justifyFull",
                ["ctrl+q"] = "notACommand"
            });
        });

        var combos = (string[])SetupOption(LastSetupOptions(), "ShortcutKeys")!;

        CollectionAssert.Contains(combos, "ctrl+b");
        CollectionAssert.Contains(combos, "ctrl+j");
        // A combo mapped to an unknown command must not be advertised as owned, or the bridge
        // would suppress a browser default for a keystroke nothing handles.
        CollectionAssert.DoesNotContain(combos, "ctrl+q");
        // The list is sorted so an unchanged configuration serializes identically each render.
        CollectionAssert.AreEqual(combos.OrderBy(c => c, StringComparer.Ordinal).ToArray(), combos);
    }


    // ---------------------------------------------------------------- bridge callbacks

    [TestMethod]
    public async Task BitRichTextEditorShouldRaiseChangeAndBindingOnContentChange()
    {
        SetupJsInterop();

        string? changed = null;
        string? bound = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<string?>(this, v => changed = v));
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v));
        });

        await component.InvokeAsync(() => component.Instance._OnContentChanged(
            "<p>typed</p>", new BitRichTextEditorContentFacts(true, false, 5, 1)));

        Assert.AreEqual("<p>typed</p>", changed);
        Assert.AreEqual("<p>typed</p>", bound);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldShowTheCountsFromTheContentFacts()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowCount, true);
            parameters.Add(p => p.MaxLength, 10);
        });

        await component.InvokeAsync(() => component.Instance._OnFactsChanged(
            new BitRichTextEditorContentFacts(true, false, 10, 2)));

        var footer = component.Find(".bit-rte-cnt").TextContent;
        StringAssert.Contains(footer, "2 words");
        StringAssert.Contains(footer, "10/10");
        // Reaching the cap is called out rather than silently swallowing further input.
        Assert.IsNotNull(component.Find(".bit-rte-cnt-over"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldCountOneWordInTheSingular()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowCount, true);
        });

        await component.InvokeAsync(() => component.Instance._OnFactsChanged(
            new BitRichTextEditorContentFacts(true, false, 1, 1)));

        // Each count is a full template rather than a number glued to a word, so one word does not
        // read "1 words" - and a translator keeps control of the plural.
        var footer = component.Find(".bit-rte-cnt").TextContent;
        StringAssert.Contains(footer, "1 word ");
        StringAssert.Contains(footer, "1 char");
        Assert.IsFalse(footer.Contains("1 words"));
        Assert.IsFalse(footer.Contains("1 chars"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldRaiseFocusAndBlur()
    {
        SetupJsInterop();

        var focused = false;
        var blurred = false;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnFocus, EventCallback.Factory.Create(this, () => focused = true));
            parameters.Add(p => p.OnBlur, EventCallback.Factory.Create(this, () => blurred = true));
        });

        await component.InvokeAsync(() => component.Instance._OnFocused());
        await component.InvokeAsync(() => component.Instance._OnBlurred());

        Assert.IsTrue(focused);
        Assert.IsTrue(blurred);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldSurfaceACommandFailureWithoutBridgeDetail()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await component.InvokeAsync(() => component.Instance._OnCommandError("bold", "NS_ERROR_FAILURE"));

        Assert.IsNotNull(error);
        Assert.AreEqual("command-failed", error!.Code);
        StringAssert.Contains(error.Message, "bold");
        Assert.IsFalse(error.Message.Contains("NS_ERROR_FAILURE"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldSurfaceClientErrors()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await component.InvokeAsync(() => component.Instance._OnClientError("file-too-large", "Too big."));

        Assert.IsNotNull(error);
        Assert.AreEqual("file-too-large", error!.Code);
        Assert.AreEqual("Too big.", component.Find(".bit-rte-err").TextContent);
        Assert.AreEqual("alert", component.Find(".bit-rte-err").GetAttribute("role"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldTrackAnExternalFullScreenExit()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.FullScreen);
        });

        await component.InvokeAsync(() => component.Instance._OnFullScreenChanged(true));
        Assert.IsTrue(component.Find(".bit-rte").ClassList.Contains("bit-rte-fsc"));

        await component.InvokeAsync(() => component.Instance._OnFullScreenChanged(false));
        Assert.IsFalse(component.Find(".bit-rte").ClassList.Contains("bit-rte-fsc"));
    }


    // ---------------------------------------------------------------- imperative API

    [TestMethod]
    public async Task BitRichTextEditorShouldGetHtml()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        var html = await component.Instance.GetHtmlAsync();

        Assert.AreEqual("<p>html</p>", html);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldGetText()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        var text = await component.Instance.GetTextAsync();

        Assert.AreEqual("text", text);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldGetTheSelectedText()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        Assert.AreEqual("selected", await component.Instance.GetSelectedTextAsync());
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldExecuteCommand()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.Instance.ExecuteCommandAsync("bold");

        // Assert the exact forwarded command (Arguments: [editor, command, value]) so a wrong
        // command string can't slip through - merely verifying the invoke happened wouldn't.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.exec");
        Assert.AreEqual("bold", invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldInsertTextAndHtml()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.InvokeAsync(() => component.Instance.InsertTextAsync("hello"));
        await component.InvokeAsync(() => component.Instance.InsertHtmlAsync("<b>hi</b>"));

        Assert.AreEqual("hello", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertText").Arguments[1]);
        Assert.AreEqual("<b>hi</b>", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertHtml").Arguments[1]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldUndoRedoAndSelectAll()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.InvokeAsync(() => component.Instance.UndoAsync());
        await component.InvokeAsync(() => component.Instance.RedoAsync());
        await component.InvokeAsync(() => component.Instance.SelectAllAsync().AsTask());

        var commands = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.exec"]
            .Select(i => i.Arguments[1]).ToArray();
        CollectionAssert.AreEqual(new object[] { "undo", "redo" }, commands);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.selectAll");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldSetAndClearHtmlThroughTheApi()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>clean</p>");

        string? bound = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v));
        });

        await component.InvokeAsync(() => component.Instance.SetHtmlAsync("<p>dirty</p>"));

        // SetHtmlAsync must route through the sanitizer and publish the cleaned markup, not the
        // markup it was handed.
        Assert.AreEqual("<p>clean</p>", bound);
        Assert.AreEqual("<p>clean</p>", Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.setHtml"].Last().Arguments[1]);

        await component.InvokeAsync(() => component.Instance.ClearAsync());
        Assert.AreEqual("", bound);
    }

    [TestMethod]
    public void BitRichTextEditorShouldSetupOnFirstRender()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.initialize");
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.enableToolbarRoving");
    }

    [TestMethod]
    public void BitRichTextEditorShouldFocusOnFirstRenderWhenAutoFocusIsSet()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.focus");
    }

    [TestMethod]
    public void BitRichTextEditorShouldNotFocusOnFirstRenderByDefault()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>();

        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.focus"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldDisposeJsInterop()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.dispose");
    }


    // ---------------------------------------------------------------- options plumbing

    [TestMethod]
    public void BitRichTextEditorShouldSendTheBridgeOptions()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceMs, 500);
            parameters.Add(p => p.MaxLength, 42);
            parameters.Add(p => p.PasteAsPlainText, true);
            parameters.Add(p => p.AutoLink, false);
        });

        var options = LastSetupOptions();
        Assert.AreEqual(500, SetupOption(options, "Debounce"));
        Assert.AreEqual(42, SetupOption(options, "MaxLength"));
        Assert.AreEqual(true, SetupOption(options, "PlainTextPaste"));
        Assert.AreEqual(false, SetupOption(options, "AutoLink"));
        // The selection rectangle is a layout read on every selection change, so the bridge is told
        // whether anything needs it.
        Assert.AreEqual(false, SetupOption(options, "QuickToolbar"));
        Assert.AreEqual(false, SetupOption(options, "HasUpload"));
        Assert.IsNull(SetupOption(options, "Policy"));
    }

    [DataTestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, 0)]
    [DataRow(250, 250)]
    public void BitRichTextEditorShouldClampANegativeDebounce(int given, int expected)
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceMs, given);
        });

        // A negative interval is meaningless to the bridge timer, so it is normalized rather than
        // being forwarded as-is.
        Assert.AreEqual(expected, SetupOption(LastSetupOptions(), "Debounce"));
    }

    [DataTestMethod]
    [DataRow(-5)]
    [DataRow(-1)]
    public void BitRichTextEditorShouldRejectANegativeMaxLength(int given)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.MaxLength, given);
            parameters.Add(p => p.ShowCount, true);
        });

        Assert.IsNull(component.Instance.MaxLength);
        Assert.IsNull(SetupOption(LastSetupOptions(), "MaxLength"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldPushChangedOptionsToTheBridge()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.DebounceMs, 200);
        });

        var before = InvokeCount("BitBlazorUI.RichTextEditor.updateOptions");

        // Re-rendering with the same values must not talk to the bridge again...
        component.Render(parameters => parameters.Add(p => p.DebounceMs, 200));
        var unchanged = InvokeCount("BitBlazorUI.RichTextEditor.updateOptions");
        Assert.AreEqual(before, unchanged);

        // ...but a real change must.
        component.Render(parameters => parameters.Add(p => p.DebounceMs, 800));
        Assert.IsTrue(Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.updateOptions"].Count > unchanged);
    }


    // ---------------------------------------------------------------- sanitization

    [TestMethod]
    public void BitRichTextEditorShouldInvokeSanitizeBridgeWhenPolicyIsSet()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>clean</p>");

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.SanitizationPolicy, BitRichTextEditorSanitizationPolicy.Default);
        });

        // Capture how many times the sanitize bridge was invoked during setup so the assertion
        // below proves the *update* path invokes it again, not an earlier render.
        var before = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;

        // A value change after initialization routes through the sanitization bridge.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, "<p><script>alert(1)</script>dirty</p>");
        });

        var after = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;
        Assert.IsTrue(after > before, "The Value update should route through the sanitize bridge.");
    }

    [TestMethod]
    public void BitRichTextEditorShouldInvokeSanitizeBridgeWhenPolicyIsNull()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>clean</p>");

        // No SanitizationPolicy is set, so the component relies on the bridge's secure default
        // allowlist; non-empty Value updates must still route through the sanitize bridge.
        var component = RenderComponent<BitRichTextEditor>();

        var before = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;

        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, "<p><script>alert(1)</script>dirty</p>");
        });

        var after = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;
        Assert.IsTrue(after > before, "The Value update should route through the sanitize bridge even without an explicit policy.");
    }

    [TestMethod]
    public void BitRichTextEditorShouldWriteTheSanitizedValueBackToTheBinding()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>clean</p>");

        string? bound = null;
        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Value, "<p><script>alert(1)</script>clean</p>");
            parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v));
        });

        // The bound value must never keep markup the editor refused to render.
        Assert.AreEqual("<p>clean</p>", bound);
    }

    [TestMethod]
    public void BitRichTextEditorShouldRecleanTheContentWhenThePolicyTightens()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>clean</p>");

        var component = RenderComponent<BitRichTextEditor>();
        var before = Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count;

        component.Render(parameters =>
        {
            parameters.Add(p => p.SanitizationPolicy, new BitRichTextEditorSanitizationPolicy
            {
                AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "p" },
                AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase),
                AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https" }
            });
        });

        // A tightened allowlist has to clean what is already loaded, not just future input.
        Assert.IsTrue(Context.JSInterop.Invocations["BitBlazorUI.RichTextEditor.sanitizeHtml"].Count > before);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.updateOptions");
    }

    [TestMethod]
    public void BitRichTextEditorShouldSendTheLowercasedPolicyToTheBridge()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("");

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.SanitizationPolicy, new BitRichTextEditorSanitizationPolicy
            {
                AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "P", "STRONG" },
                AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["A"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HREF" }
                },
                AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HTTPS" }
            });
        });

        // The bridge compares lowercase names, so the payload is normalized on the way out.
        var policy = SetupOption(LastSetupOptions(), "Policy")!;
        CollectionAssert.AreEqual(new[] { "p", "strong" }, (string[])SetupOption(policy, "AllowedTags")!);
        CollectionAssert.AreEqual(new[] { "https" }, (string[])SetupOption(policy, "AllowedUriSchemes")!);
        var attributes = (Dictionary<string, string[]>)SetupOption(policy, "AllowedAttributes")!;
        CollectionAssert.AreEqual(new[] { "href" }, attributes["a"]);
    }

    [TestMethod]
    public void BitRichTextEditorDefaultPolicyShouldKeepTheFormattingItProduces()
    {
        var policy = BitRichTextEditorSanitizationPolicy.Default;

        // Alignment, indentation, colors and font sizes are all emitted as style/align attributes
        // by the formatting commands; dropping them would show formatting the saved value lacks.
        Assert.IsTrue(policy.AllowedAttributes["*"].Contains("style"));
        Assert.IsTrue(policy.AllowedAttributes["*"].Contains("align"));
        // Column resizing writes a width attribute onto the cell.
        Assert.IsTrue(policy.AllowedAttributes["td"].Contains("width"));
        // Checklists are plain list items carrying their state in data-checked.
        Assert.IsTrue(policy.AllowedAttributes["li"].Contains("data-checked"));
        // Media embeds are on by default; the sanitize pass pairs the tag with a host allowlist,
        // so an iframe only survives when it points at an approved embed host over https.
        Assert.IsTrue(policy.AllowedTags.Contains("iframe"));
        Assert.IsTrue(policy.AllowedAttributes["iframe"].Contains("allowfullscreen"));
        Assert.IsNotNull(policy.AllowedIframeHosts);
        CollectionAssert.Contains(policy.AllowedIframeHosts!.ToArray(), "www.youtube-nocookie.com");
        CollectionAssert.Contains(policy.AllowedIframeHosts!.ToArray(), "player.vimeo.com");
    }

    [TestMethod]
    public void BitRichTextEditorShouldLeaveTheEmbedHostsToTheBridgeWhenAPolicyNamesNone()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("");

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.SanitizationPolicy, new BitRichTextEditorSanitizationPolicy
            {
                AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "p", "iframe" },
                AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase),
                AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https" }
            });
        });

        // A policy that lists the iframe tag without saying where from sends no host list, which is
        // what makes the bridge fall back to its built-in approved embed hosts rather than framing
        // anything the scheme allowlist happens to accept.
        var policy = SetupOption(LastSetupOptions(), "Policy")!;
        Assert.IsNull(SetupOption(policy, "AllowedIframeHosts"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldSendTheLowercasedEmbedHosts()
    {
        SetupJsInterop();
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("");

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.SanitizationPolicy, new BitRichTextEditorSanitizationPolicy
            {
                AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "p", "iframe" },
                AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase),
                AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https" },
                AllowedIframeHosts = ["Player.Vimeo.COM", "maps.example.com"]
            });
        });

        var policy = SetupOption(LastSetupOptions(), "Policy")!;
        CollectionAssert.AreEqual(new[] { "player.vimeo.com", "maps.example.com" },
            (string[])SetupOption(policy, "AllowedIframeHosts")!);
    }

    [TestMethod]
    public void BitRichTextEditorDefaultPolicyShouldBeAFreshInstanceEachTime()
    {
        var first = BitRichTextEditorSanitizationPolicy.Default;
        first.AllowedTags.Add("marquee");

        // Callers are told they may mutate the default; that must not leak into the next editor.
        Assert.IsFalse(BitRichTextEditorSanitizationPolicy.Default.AllowedTags.Contains("marquee"));
    }


    // ---------------------------------------------------------------- content facts

    [DataTestMethod]
    [DataRow(false, false, true)]
    [DataRow(true, false, false)]
    [DataRow(false, true, false)]
    [DataRow(true, true, false)]
    public void BitRichTextEditorContentFactsShouldClassifyEmptiness(bool hasText, bool hasEmbedded, bool expectedEmpty)
    {
        // An image-only or table-only document is not empty even though it has no text.
        var facts = new BitRichTextEditorContentFacts(hasText, hasEmbedded, 0, 0);

        Assert.AreEqual(expectedEmpty, facts.IsEmpty);
    }


    // ---------------------------------------------------------------- selection toolbar

    [TestMethod]
    public void BitRichTextEditorShouldNotShowTheQuickToolbarByDefault()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { HasSelection = true })).Wait();

        Assert.AreEqual(0, component.FindAll(".bit-rte-quick").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldShowTheQuickToolbarOnlyWhileSomethingIsSelected()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowQuickToolbar, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-rte-quick").Count);

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { HasSelection = true, SelectionTop = 120, SelectionLeft = 40, SelectionHeight = 18 })).Wait();

        var bar = component.Find(".bit-rte-quick");
        Assert.AreEqual("toolbar", bar.GetAttribute("role"));

        // It is anchored above the selection and formatted with an invariant decimal separator, so
        // the declaration is not dropped under a comma-decimal culture.
        StringAssert.Contains(bar.GetAttribute("style"), "top:76px");
        StringAssert.Contains(bar.GetAttribute("style"), "left:40px");

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState())).Wait();
        Assert.AreEqual(0, component.FindAll(".bit-rte-quick").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldFlipTheQuickToolbarBelowASelectionNearTheTop()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowQuickToolbar, true);
        });

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { HasSelection = true, SelectionTop = 4, SelectionHeight = 20 })).Wait();

        // Placing it above would put the bar off the top of the component, so it goes below instead.
        StringAssert.Contains(component.Find(".bit-rte-quick").GetAttribute("style"), "top:32px");
    }

    [TestMethod]
    public void BitRichTextEditorShouldHideTheQuickToolbarWhileReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowQuickToolbar, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { HasSelection = true })).Wait();

        Assert.AreEqual(0, component.FindAll(".bit-rte-quick").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorQuickToolbarShouldRunTheCommand()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowQuickToolbar, true);
            parameters.Add(p => p.ShowToolbar, false);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { HasSelection = true }));

        await component.Find(".bit-rte-quick button[aria-label='Bold']").ClickAsync(new());

        Assert.AreEqual("bold", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.exec").Arguments[1]);
    }


    // ---------------------------------------------------------------- image alignment

    [TestMethod]
    public void BitRichTextEditorShouldDisableImageAlignmentWithoutASelectedImage()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
        });

        Assert.IsTrue(ButtonByLabel(component, "Float image left").HasAttribute("disabled"));
        Assert.IsTrue(ButtonByLabel(component, "Center image").HasAttribute("disabled"));
        Assert.IsTrue(ButtonByLabel(component, "Float image right").HasAttribute("disabled"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldAlignTheSelectedImage()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { ImageSelected = true }));

        await ButtonByLabel(component, "Float image right").ClickAsync(new());

        Assert.AreEqual("right", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.alignImage").Arguments[1]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldClearTheAlignmentWhenTheActiveOneIsClickedAgain()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { ImageSelected = true, ImageAlign = "center" }));

        Assert.AreEqual("true", ButtonByLabel(component, "Center image").GetAttribute("aria-pressed"));

        await ButtonByLabel(component, "Center image").ClickAsync(new());

        // Clicking the active alignment puts the image back inline rather than reapplying it.
        Assert.AreEqual("none", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.alignImage").Arguments[1]);
    }


    // ---------------------------------------------------------------- mentions

    private static Func<string, Task<IReadOnlyList<BitRichTextEditorMention>>> People(params string[] names)
        => term => Task.FromResult<IReadOnlyList<BitRichTextEditorMention>>(
            names.Where(n => string.IsNullOrEmpty(term) || n.Contains(term, StringComparison.OrdinalIgnoreCase))
                 .Select((n, i) => new BitRichTextEditorMention(i.ToString(), n))
                 .ToArray());

    [TestMethod]
    public void BitRichTextEditorShouldOnlyWatchForMentionsWhenALookupIsSupplied()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>();
        Assert.AreEqual(false, SetupOption(LastSetupOptions(), "Mentions"));

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnMentionSearch, People("Ada"));
        });
        Assert.AreEqual(true, SetupOption(LastSetupOptions(), "Mentions"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldOpenTheMentionMenuAndFilterIt()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnMentionSearch, People("Ada Lovelace", "Alan Turing", "Grace Hopper"));
        });

        await component.InvokeAsync(() => component.Instance._OnMentionTrigger());

        Assert.AreEqual(3, component.FindAll(".bit-rte-mention-menu .bit-rte-slash-item").Count);

        await component.Find(".bit-rte-mention-menu input").InputAsync(new() { Value = "grace" });

        var items = component.FindAll(".bit-rte-mention-menu .bit-rte-slash-item");
        Assert.AreEqual(1, items.Count);
        StringAssert.Contains(items[0].TextContent, "Grace Hopper");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldInsertThePickedMention()
    {
        SetupJsInterop();

        BitRichTextEditorMention? picked = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnMentionSearch, People("Ada Lovelace"));
            parameters.Add(p => p.OnMentionSelected, EventCallback.Factory.Create<BitRichTextEditorMention>(this, m => picked = m));
        });

        await component.InvokeAsync(() => component.Instance._OnMentionTrigger());
        await component.Find(".bit-rte-mention-menu .bit-rte-slash-item").ClickAsync(new());

        var html = (string)Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.applyMention").Arguments[1]!;
        // The id has to reach the markup, or the host cannot resolve the mention when reading back.
        StringAssert.Contains(html, "data-mention-id=\"0\"");
        StringAssert.Contains(html, "@Ada Lovelace");
        Assert.IsNotNull(picked);
        Assert.AreEqual("Ada Lovelace", picked!.Display);
        // The menu closes once a mention is picked.
        Assert.AreEqual(0, component.FindAll(".bit-rte-mention-menu").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldEscapeMentionMarkup()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnMentionSearch, new Func<string, Task<IReadOnlyList<BitRichTextEditorMention>>>(
                _ => Task.FromResult<IReadOnlyList<BitRichTextEditorMention>>(
                    [new BitRichTextEditorMention("\" onerror=\"x", "<script>alert(1)</script>")])));
        });

        await component.InvokeAsync(() => component.Instance._OnMentionTrigger());
        await component.Find(".bit-rte-mention-menu .bit-rte-slash-item").ClickAsync(new());

        var html = (string)Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.applyMention").Arguments[1]!;
        // Host-supplied text is data, so neither the id nor the display name may break out of the
        // attribute or the element they are written into.
        Assert.IsFalse(html.Contains("<script>"));
        Assert.IsFalse(html.Contains("onerror=\"x"));
        StringAssert.Contains(html, "&lt;script&gt;");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNavigateTheMentionMenuWithTheKeyboard()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnMentionSearch, People("Ada", "Alan", "Grace"));
        });

        await component.InvokeAsync(() => component.Instance._OnMentionTrigger());

        await component.Find(".bit-rte-mention-menu input").KeyDownAsync(Key.Down);
        Assert.AreEqual("true", component.FindAll(".bit-rte-mention-menu .bit-rte-slash-item")[1].GetAttribute("aria-selected"));

        await component.Find(".bit-rte-mention-menu input").KeyDownAsync(Key.Enter);
        Assert.AreEqual(0, component.FindAll(".bit-rte-mention-menu").Count);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.applyMention");
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldCloseTheMentionMenuOnEscape()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnMentionSearch, People("Ada"));
        });

        await component.InvokeAsync(() => component.Instance._OnMentionTrigger());
        await component.Find(".bit-rte-mention-menu input").KeyDownAsync(Key.Escape);

        Assert.AreEqual(0, component.FindAll(".bit-rte-mention-menu").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldReportAFailingMentionLookup()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
            parameters.Add(p => p.OnMentionSearch, new Func<string, Task<IReadOnlyList<BitRichTextEditorMention>>>(
                _ => throw new InvalidOperationException("directory offline")));
        });

        await component.InvokeAsync(() => component.Instance._OnMentionTrigger());

        Assert.IsNotNull(error);
        Assert.AreEqual("mention-search-failed", error!.Code);
        Assert.IsFalse(error.Message.Contains("directory offline"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNotOpenTheMentionMenuWhileReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnMentionSearch, People("Ada"));
        });

        await component.InvokeAsync(() => component.Instance._OnMentionTrigger());

        Assert.AreEqual(0, component.FindAll(".bit-rte-mention-menu").Count);
    }

    [TestMethod]
    public void BitRichTextEditorDefaultPolicyShouldKeepAMentionIntact()
    {
        var policy = BitRichTextEditorSanitizationPolicy.Default;

        // A mention is a span carrying the host's id; dropping the attribute would turn a saved
        // mention back into plain text on reload.
        Assert.IsTrue(policy.AllowedAttributes["span"].Contains("data-mention-id"));
        Assert.IsTrue(policy.AllowedAttributes["*"].Contains("class"));
    }


    // ---------------------------------------------------------------- localization

    [TestMethod]
    public void BitRichTextEditorShouldUseTheLocalizerForLabels()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
            parameters.Add(p => p.Localizer, new TestLocalizer(new() { ["bold-label"] = "Gras", ["toolbar"] = "Mise en forme" }));
        });

        Assert.AreEqual("Mise en forme", component.Find(".bit-rte-tlb").GetAttribute("aria-label"));
        Assert.IsNotNull(component.Find("button[aria-label='Gras']"));
        // A key the localizer does not know falls back to the built-in English text.
        Assert.IsNotNull(component.Find("button[aria-label='Italic']"));
    }

    // ---------------------------------------------------------------- panels

    [TestMethod]
    public async Task BitRichTextEditorShouldPrefillWhetherAnExistingLinkOpensInANewTab()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { InLink = true, LinkHref = "https://example.com", LinkNewTab = true }));

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        // Opening the panel on a new-tab link must show it as such; otherwise pressing Apply after
        // editing the URL would silently drop the target the author chose.
        Assert.IsTrue(component.Find(".bit-rte-bar input[type=checkbox]").HasAttribute("checked"));

        await ButtonByText(component, ".bit-rte-bar button", "Apply").ClickAsync(new());
        Assert.AreEqual(true, Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.updateLink").Arguments[2]);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNotPrefillTheNewTabStateOfASameTabLink()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { InLink = true, LinkHref = "https://example.com" }));

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        Assert.IsFalse(component.Find(".bit-rte-bar input[type=checkbox]").HasAttribute("checked"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldCloseTheFindPanelWhenAnotherToolOpens()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find | BitRichTextEditorToolbar.Link);
        });

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=text]").InputAsync(new() { Value = "fox" });
        var clearedBefore = InvokeCount("BitBlazorUI.RichTextEditor.clearFind");

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        // The panels share one strip under the toolbar, so opening another tool closes find - and
        // closing it has to take its highlight markup out of the content with it.
        Assert.AreEqual(0, component.FindAll("input[aria-label='Replace with']").Count);
        Assert.IsNotNull(component.Find(".bit-rte-bar input[type=url]"));
        Assert.IsTrue(InvokeCount("BitBlazorUI.RichTextEditor.clearFind") > clearedBefore);
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldCloseTheOtherPanelsWhenFindOpens()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find | BitRichTextEditorToolbar.Image);
        });

        await ButtonByLabel(component, "Insert image").ClickAsync(new());
        await ButtonByLabel(component, "Find and replace").ClickAsync(new());

        Assert.AreEqual(0, component.FindAll("input[aria-label='Image URL']").Count);
        Assert.IsNotNull(component.Find("input[aria-label='Find']"));
    }


    [DataTestMethod]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void BitRichTextEditorShouldDisableTheSourceViewToggleWhenLocked(bool readOnly, bool isEnabled)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Source);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        // Both ways of locking the editor keep source view out of reach, so the button must look
        // the way it behaves rather than being enabled for a toggle that refuses to run.
        Assert.IsTrue(ButtonByLabel(component, "HTML source view").HasAttribute("disabled"));
    }


    [TestMethod]
    public async Task BitRichTextEditorShouldCloseTheOpenPanelsWhenSourceViewOpens()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find | BitRichTextEditorToolbar.Source);
        });

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.Find("input[aria-label='Find']").InputAsync(new() { Value = "fox" });

        await ButtonByLabel(component, "HTML source view").ClickAsync(new());
        Assert.IsNotNull(component.Find("textarea.bit-rte-src"));

        // Coming back must not restore a panel still holding the previous search and its
        // highlights: the panels belong to the rendered view, so entering source view closes them.
        await ButtonByLabel(component, "HTML source view").ClickAsync(new());
        Assert.AreEqual(0, component.FindAll("input[aria-label='Find']").Count);
        Assert.IsTrue(InvokeCount("BitBlazorUI.RichTextEditor.clearFind") > 0);
    }


    // ---------------------------------------------------------------- accessibility

    [TestMethod]
    public void BitRichTextEditorShouldGiveAReadOnlySurfaceATabStop()
    {
        SetupJsInterop();

        // An editable surface is focusable through contenteditable itself...
        var editable = RenderComponent<BitRichTextEditor>();
        Assert.IsFalse(editable.Find(".bit-rte-edt").HasAttribute("tabindex"));

        // ...but a read-only one is a plain div, so without a tab stop its content could not be
        // reached, scrolled or read out from the keyboard at all.
        var readOnly = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });
        Assert.AreEqual("0", readOnly.Find(".bit-rte-edt").GetAttribute("tabindex"));

        // A disabled component takes no focus at all.
        var disabled = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });
        Assert.IsFalse(disabled.Find(".bit-rte-edt").HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldExposeThePlaceholderToAssistiveTechnology()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Write something...");
        });

        // The placeholder is drawn by CSS from data-placeholder, which a screen reader never sees.
        var editor = component.Find(".bit-rte-edt");
        Assert.AreEqual("Write something...", editor.GetAttribute("data-placeholder"));
        Assert.AreEqual("Write something...", editor.GetAttribute("aria-placeholder"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldAnnounceTheShortcutOfAToolbarButton()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.History);
        });

        Assert.AreEqual("Control+B", ButtonByLabel(component, "Bold").GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("Control+Shift+X", ButtonByLabel(component, "Strikethrough").GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("Control+Z", ButtonByLabel(component, "Undo").GetAttribute("aria-keyshortcuts"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldAnnounceTheCustomShortcutInsteadOfTheDefault()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string>
            {
                ["ctrl+shift+b"] = "bold",
                ["ctrl+i"] = "underline"
            });
        });

        // What is announced has to be what actually runs the command...
        Assert.AreEqual("Control+Shift+B", ButtonByLabel(component, "Bold").GetAttribute("aria-keyshortcuts"));
        // ...and a default whose key the host has taken over for something else announces nothing.
        Assert.IsFalse(ButtonByLabel(component, "Italic").HasAttribute("aria-keyshortcuts"));
        Assert.AreEqual("Control+I", ButtonByLabel(component, "Underline").GetAttribute("aria-keyshortcuts"));
    }


    // ---------------------------------------------------------------- block-format shortcuts

    [TestMethod]
    public async Task BitRichTextEditorShouldRunABlockFormatShortcut()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string> { ["ctrl+alt+1"] = "h1" });
        });

        var handled = await component.InvokeAsync(() => component.Instance._OnShortcut("1", true, false, true));

        // A block name is a paragraph format, so it runs through the block path rather than being
        // rejected as an unknown editing command.
        Assert.IsTrue(handled);
        Assert.AreEqual("h1", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.execBlock").Arguments[1]);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.exec"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldToggleABlockFormatShortcutBackToAParagraph()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string> { ["ctrl+alt+1"] = "h1" });
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { Block = "h1" }));

        await component.InvokeAsync(() => component.Instance._OnShortcut("1", true, false, true));

        // Pressing the chord on the block it already produces goes back to a normal paragraph,
        // exactly as the toolbar's own block buttons do.
        Assert.AreEqual("p", Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.execBlock").Arguments[1]);
    }

    [TestMethod]
    public void BitRichTextEditorShouldAdvertiseABlockShortcutAsOwned()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string>
            {
                ["ctrl+alt+1"] = "h1",
                ["ctrl+alt+0"] = "p"
            });
        });

        var combos = (string[])SetupOption(LastSetupOptions(), "ShortcutKeys")!;

        CollectionAssert.Contains(combos, "ctrl+alt+1");
        CollectionAssert.Contains(combos, "ctrl+alt+0");
    }


    // ---------------------------------------------------------------- editing a selected image

    [TestMethod]
    public async Task BitRichTextEditorShouldEditTheSelectedImageInsteadOfInsertingANewOne()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState
        {
            ImageSelected = true,
            ImageSrc = "https://example.com/photo.png",
            ImageAlt = "A photo"
        }));

        await ButtonByLabel(component, "Edit image").ClickAsync(new());

        // The panel opens on the selected image, showing what it already carries - which is what
        // makes alternative text fixable after the image was inserted without any.
        Assert.AreEqual("https://example.com/photo.png", component.Find("input[aria-label='Image URL']").GetAttribute("value"));
        Assert.AreEqual("A photo", component.Find("input[aria-label='Alternative text']").GetAttribute("value"));

        await component.Find("input[aria-label='Alternative text']").InputAsync(new() { Value = "A better description" });
        await ButtonByText(component, ".bit-rte-bar button", "Update").ClickAsync(new());

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.updateImage");
        Assert.AreEqual("https://example.com/photo.png", invocation.Arguments[1]);
        Assert.AreEqual("A better description", invocation.Arguments[2]);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.insertImageUrl"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldStillInsertAnImageWithNothingSelected()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
        });

        await ButtonByLabel(component, "Insert image").ClickAsync(new());
        Assert.AreEqual("", component.Find("input[aria-label='Image URL']").GetAttribute("value"));

        await component.Find("input[aria-label='Image URL']").InputAsync(new() { Value = "https://example.com/new.png" });
        await ButtonByText(component, ".bit-rte-bar button", "Insert").ClickAsync(new());

        Assert.AreEqual("https://example.com/new.png",
            Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertImageUrl").Arguments[1]);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.updateImage"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldRejectAnInvalidUrlWhenEditingAnImage()
    {
        SetupJsInterop();

        BitRichTextEditorError? error = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<BitRichTextEditorError>(this, e => error = e));
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState
        {
            ImageSelected = true,
            ImageSrc = "https://example.com/photo.png",
            ImageAlt = ""
        }));

        await ButtonByLabel(component, "Edit image").ClickAsync(new());
        await component.Find("input[aria-label='Image URL']").InputAsync(new() { Value = "javascript:alert(1)" });
        await ButtonByText(component, ".bit-rte-bar button", "Update").ClickAsync(new());

        // Editing an image runs through the same URL validation as inserting one.
        Assert.IsNotNull(error);
        Assert.AreEqual("invalid-url", error!.Code);
        Assert.AreEqual(0, InvokeCount("BitBlazorUI.RichTextEditor.updateImage"));
    }


    // ---------------------------------------------------------------- color

    [DataTestMethod]
    [DataRow("Remove text color", "fore")]
    [DataRow("Remove highlight", "back")]
    public async Task BitRichTextEditorShouldClearTheColorOfTheSelection(string label, string kind)
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Color);
        });

        await ButtonByLabel(component, label).ClickAsync(new());

        // Taking a color back off is its own operation: "clear formatting" would remove the bold
        // and the links along with it.
        Assert.AreEqual(kind, Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.clearColor").Arguments[1]);
    }

    [TestMethod]
    public void BitRichTextEditorShouldDisableTheColorControlsWhileReadOnly()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Color);
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.IsTrue(ButtonByLabel(component, "Remove text color").HasAttribute("disabled"));
        Assert.IsTrue(ButtonByLabel(component, "Remove highlight").HasAttribute("disabled"));
        Assert.IsTrue(component.Find("input[aria-label='Text color']").HasAttribute("disabled"));
    }


    // ---------------------------------------------------------------- selection events

    [TestMethod]
    public async Task BitRichTextEditorShouldReportSelectionChangesToTheHost()
    {
        SetupJsInterop();

        BitRichTextEditorSelectionState? reported = null;
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.OnSelectionChange,
                EventCallback.Factory.Create<BitRichTextEditorSelectionState>(this, s => reported = s));
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(
            new BitRichTextEditorSelectionState { Bold = true, Block = "h2", InLink = true, LinkHref = "https://example.com" }));

        // The host gets the same snapshot the toolbar highlights itself from, so it can drive its
        // own chrome from the caret without reaching into the browser.
        Assert.IsNotNull(reported);
        Assert.IsTrue(reported!.Bold);
        Assert.AreEqual("h2", reported.Block);
        Assert.IsTrue(reported.InLink);
        Assert.AreEqual("https://example.com", reported.LinkHref);
    }


    // ---------------------------------------------------------------- content facts

    [TestMethod]
    public async Task BitRichTextEditorShouldExposeTheContentFactsAsProperties()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>();

        // Before anything is typed the editor reads as empty rather than as unknown.
        Assert.IsTrue(component.Instance.IsEmpty);
        Assert.AreEqual(0, component.Instance.WordCount);
        Assert.AreEqual(0, component.Instance.CharacterCount);

        await component.InvokeAsync(() => component.Instance._OnContentChanged(
            "<p>Two words</p>", new BitRichTextEditorContentFacts(true, false, 9, 2)));

        Assert.IsFalse(component.Instance.IsEmpty);
        Assert.AreEqual(2, component.Instance.WordCount);
        Assert.AreEqual(9, component.Instance.CharacterCount);

        // A programmatic set refreshes them without being treated as an edit.
        await component.InvokeAsync(() => component.Instance._OnFactsChanged(
            new BitRichTextEditorContentFacts(false, true, 0, 0)));

        // An image is content even with no text, so the editor is not empty.
        Assert.IsFalse(component.Instance.IsEmpty);
    }


    // ---------------------------------------------------------------- surface

    [TestMethod]
    public void BitRichTextEditorShouldOfferAResizeHandleWhenAsked()
    {
        SetupJsInterop();

        Assert.IsFalse(RenderComponent<BitRichTextEditor>().Find(".bit-rte-edt").ClassList.Contains("bit-rte-edt-rsz"));

        var resizable = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Resizable, true);
        });

        Assert.IsTrue(resizable.Find(".bit-rte-edt").ClassList.Contains("bit-rte-edt-rsz"));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldHideTheQuickToolbarWhileAPanelIsOpen()
    {
        SetupJsInterop();

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowQuickToolbar, true);
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState
        {
            HasSelection = true,
            SelectionTop = 120,
            SelectionLeft = 40,
            SelectionHeight = 20
        }));
        Assert.AreEqual(1, component.FindAll(".bit-rte-quick").Count);

        // The panel and the floating bar occupy the same corner of the editor, so the bar steps
        // aside rather than covering the fields the panel just opened.
        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());
        Assert.AreEqual(0, component.FindAll(".bit-rte-quick").Count);
    }


    // ---------------------------------------------------------------- typography

    [TestMethod]
    public void BitRichTextEditorShouldTellTheBridgeAboutSmartTypography()
    {
        SetupJsInterop();

        RenderComponent<BitRichTextEditor>();
        // The typographic input rules rewrite what was typed, so they stay off unless asked for.
        Assert.AreEqual(false, SetupOption(LastSetupOptions(), "SmartTypography"));

        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.SmartTypography, true);
        });
        Assert.AreEqual(true, SetupOption(LastSetupOptions(), "SmartTypography"));

        var pushed = InvokeCount("BitBlazorUI.RichTextEditor.updateOptions");
        component.Render(parameters => parameters.Add(p => p.SmartTypography, false));
        Assert.IsTrue(InvokeCount("BitBlazorUI.RichTextEditor.updateOptions") > pushed);
    }

    private sealed class TestLocalizer(Dictionary<string, string> labels) : IBitRichTextEditorLocalizer
    {
        public string? this[string key] => labels.TryGetValue(key, out var value) ? value : null;
    }
}
