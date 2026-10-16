using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

/// <summary>
/// Covers what the MarkdownEditor tells assistive tech and forms: the required, disabled and invalid states of its
/// textarea, the posted name, the menus its triggers control, the labelled help dialog, the announced mode, and the
/// upload button that is the keyboard's way to the upload handler.
/// </summary>
[TestClass]
public class BitMarkdownEditorAccessibilityTests : BunitTestContext
{
    private sealed class Model
    {
        public string? Notes { get; set; }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldMarkARequiredField()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Label, "Notes");
            parameters.Add(p => p.Required, true);
        });

        Assert.IsTrue(component.Find(".bit-mde-txa").HasAttribute("required"));
        Assert.AreEqual("true", component.Find(".bit-mde-txa").GetAttribute("aria-required"));
        Assert.IsTrue(component.Find(".bit-mde-lbl").ClassList.Contains("bit-mde-req"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNotBlockASubmitOverItsHiddenTextArea()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Mode, BitMarkdownEditorMode.Preview);
        });

        // A browser will not submit a form over a required control it cannot focus, and says nothing about why.
        var textArea = component.Find(".bit-mde-txa");
        Assert.IsFalse(textArea.HasAttribute("required"));
        Assert.AreEqual("true", textArea.GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldDescribeItsTextAreaWithTheDescription()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Description, "Markdown is supported.");
            parameters.Add(p => p.MaxLength, 100);
            parameters.Add(p => p.Classes, new() { Description = "custom-description" });
        });

        var description = component.Find(".bit-mde-des");
        Assert.AreEqual("Markdown is supported.", description.TextContent.Trim());
        Assert.IsTrue(description.ClassList.Contains("custom-description"));

        // Both the description and the counter are read out with the field, the description first.
        var describedBy = component.Find(".bit-mde-txa").GetAttribute("aria-describedby")!.Split(' ');
        Assert.AreEqual(2, describedBy.Length);
        Assert.AreEqual(description.Id, describedBy[0]);
        Assert.AreEqual("0 / 100 chars", component.Find($"[id='{describedBy[1]}']").TextContent);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRenderTheDescriptionTemplate()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Description, "Ignored");
            parameters.Add(p => p.DescriptionTemplate, builder => builder.AddMarkupContent(0, "<a href=\"/help\">Markdown help</a>"));
        });

        var description = component.Find(".bit-mde-des");
        Assert.AreEqual(1, description.QuerySelectorAll("a").Length);
        Assert.IsFalse(description.TextContent.Contains("Ignored"));
        Assert.AreEqual(description.Id, component.Find(".bit-mde-txa").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRenderNoDescriptionWithoutOne()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        Assert.AreEqual(0, component.FindAll(".bit-mde-des").Count);
        Assert.IsFalse(component.Find(".bit-mde").ClassList.Contains("bit-mde-stk"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldPostUnderItsName()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters => parameters.Add(p => p.Name, "notes"));

        Assert.AreEqual("notes", component.Find(".bit-mde-txa").GetAttribute("name"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldDisableItsTextAreaWhenDisabled()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters => parameters.Add(p => p.Disabled, true));

        Assert.IsTrue(component.Find(".bit-mde-txa").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldReflectTheValidationStateOfItsField()
    {
        var model = new Model();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        Expression<Func<string?>> expression = () => model.Notes;

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitMarkdownEditor>(0);
                builder.AddAttribute(1, nameof(BitMarkdownEditor.ValueExpression), expression);
                builder.CloseComponent();
            }));
        });

        var textArea = component.Find(".bit-mde-txa");
        Assert.IsFalse(textArea.HasAttribute("aria-invalid"));
        Assert.IsFalse(component.Find(".bit-mde").ClassList.Contains("bit-inv"));

        component.InvokeAsync(() =>
        {
            messages.Add(editContext.Field(nameof(Model.Notes)), "Notes are required.");
            editContext.NotifyValidationStateChanged();
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", component.Find(".bit-mde-txa").GetAttribute("aria-invalid"));
            Assert.IsTrue(component.Find(".bit-mde").ClassList.Contains("bit-inv"));
        });

        component.InvokeAsync(() =>
        {
            messages.Clear();
            editContext.NotifyValidationStateChanged();
        });

        component.WaitForAssertion(() => Assert.IsFalse(component.Find(".bit-mde-txa").HasAttribute("aria-invalid")));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldNotifyTheFormOfAnEdit()
    {
        var model = new Model();
        var editContext = new EditContext(model);
        Expression<Func<string?>> expression = () => model.Notes;
        FieldIdentifier? changed = null;
        editContext.OnFieldChanged += (_, e) => changed = e.FieldIdentifier;

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitMarkdownEditor>(0);
                builder.AddAttribute(1, nameof(BitMarkdownEditor.ValueExpression), expression);
                builder.CloseComponent();
            }));
        });

        var editor = component.FindComponent<BitMarkdownEditor>().Instance;

        await component.InvokeAsync(() => editor._OnChange("# Notes"));

        Assert.AreEqual(nameof(Model.Notes), changed?.FieldName);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldTieEachMenuTriggerToItsMenu()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        var trigger = component.Find("[data-cmd=heading]");
        var menuId = trigger.GetAttribute("aria-controls");

        Assert.IsFalse(string.IsNullOrEmpty(menuId));
        Assert.AreEqual("menu", component.Find($"#{menuId}").GetAttribute("role"));
    }

    [TestMethod]
    public void BitMarkdownEditorHelpDialogShouldBeLabelledByItsHeading()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=help]").Click();

        var dialog = component.Find("[role=dialog]");
        var heading = component.Find($"#{dialog.GetAttribute("aria-labelledby")}");

        Assert.AreEqual("H2", heading.TagName);
        Assert.AreEqual("Keyboard shortcuts", heading.TextContent);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldAnnounceTheModeItCyclesTo()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        var region = component.Find(".bit-mde-ann");
        Assert.AreEqual("polite", region.GetAttribute("aria-live"));
        Assert.AreEqual(string.Empty, region.TextContent);

        component.Find("[data-cmd=preview]").Click();

        Assert.AreEqual("Preview mode", component.Find(".bit-mde-ann").TextContent);
        Assert.IsTrue(component.Find(".bit-mde-bdy").ClassList.Contains("bit-mde-preview"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldMoveTheFocusOffAPaneTheModeHides()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=preview]").Click();

        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.syncFocus"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldOfferTheUploadButtonOnlyWithAHandler()
    {
        var without = RenderComponent<BitMarkdownEditor>();
        Assert.AreEqual(0, without.FindAll("[data-cmd=upload]").Count);

        var with = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnImageUpload, _ => Task.FromResult<string?>(null));
        });

        var upload = with.Find("[data-cmd=upload]");
        Assert.IsTrue(upload.HasAttribute("data-bit-mde-upload"));
        Assert.AreEqual("Upload image", upload.GetAttribute("aria-label"));
        Assert.IsFalse(upload.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMarkdownEditorUploadButtonShouldBeDisabledWhileReadOnly()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnImageUpload, _ => Task.FromResult<string?>(null));
        });

        Assert.IsTrue(component.Find("[data-cmd=upload]").HasAttribute("disabled"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorHelpShortcutShouldToggleTheDialog()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        await component.InvokeAsync(() => component.Instance._OnShortcut("help"));

        Assert.AreEqual(1, component.FindAll("[role=dialog]").Count);
        StringAssert.Contains(component.Find("[role=dialog] dl").TextContent, "Ctrl/Cmd + /");
        Assert.AreEqual("Control+/", component.Find("[data-cmd=help]").GetAttribute("aria-keyshortcuts"));

        await component.InvokeAsync(() => component.Instance._OnShortcut("help"));

        Assert.AreEqual(0, component.FindAll("[role=dialog]").Count);
    }

    [TestMethod]
    public void BitMarkdownEditorAutoHeightShouldMarkTheRootAndReachTheScript()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters => parameters.Add(p => p.AutoHeight, true));

        Assert.IsTrue(component.Find(".bit-mde").ClassList.Contains("bit-mde-ahg"));

        var init = Context.JSInterop.VerifyInvoke("BitBlazorUI.MarkdownEditor.init");
        StringAssert.Contains(System.Text.Json.JsonSerializer.Serialize(init.Arguments[^1]), "\"AutoHeight\":true");
    }

    [TestMethod]
    public void BitMarkdownEditorFindInputsShouldNotBeAutocompletedOrSpellChecked()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=find]").Click();

        foreach (var input in component.FindAll(".bit-mde-fni"))
        {
            Assert.AreEqual("off", input.GetAttribute("autocomplete"));
            Assert.AreEqual("false", input.GetAttribute("spellcheck"));
        }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldPairAriaDisabledWithTheDisabledOfItsTextArea()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters => parameters.Add(p => p.Disabled, true));

        var textArea = component.Find(".bit-mde-txa");

        Assert.IsTrue(textArea.HasAttribute("disabled"));
        Assert.AreEqual("true", textArea.GetAttribute("aria-disabled"));
        // A disabled editor is read-only too, so nothing types into it however it is reached.
        Assert.IsTrue(textArea.HasAttribute("readonly"));
        Assert.IsTrue(component.Find(".bit-mde").ClassList.Contains("bit-dis"));

        component.Render(parameters => parameters.Add(p => p.Disabled, false));

        textArea = component.Find(".bit-mde-txa");

        Assert.IsFalse(textArea.HasAttribute("disabled"));
        Assert.IsFalse(textArea.HasAttribute("aria-disabled"));
        Assert.IsFalse(textArea.HasAttribute("readonly"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldKeepTheSplattedAriaDisabledOfItsTextAreaWhileEnabled()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-disabled"] = "true", ["data-field"] = "notes" });
        });

        var textArea = component.Find(".bit-mde-txa");

        // The page's own value stays while the editor is enabled, and the rest of the splat reaches the textarea.
        Assert.AreEqual("true", textArea.GetAttribute("aria-disabled"));
        Assert.IsFalse(textArea.HasAttribute("disabled"));
        Assert.AreEqual("notes", textArea.GetAttribute("data-field"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldPairAriaDisabledWithTheDisabledOfEveryToolbarButton()
    {
        var disabled = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnImageUpload, _ => Task.FromResult<string?>(null));
        });

        var buttons = disabled.FindAll(".bit-mde-btn");

        Assert.IsTrue(buttons.Count > 10);

        foreach (var button in buttons)
        {
            Assert.IsTrue(button.HasAttribute("disabled"), button.GetAttribute("data-cmd"));
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"), button.GetAttribute("data-cmd"));
        }

        var enabled = RenderComponent<BitMarkdownEditor>();

        // With no history yet, undo and redo are the only buttons out of reach, and they say so both ways.
        foreach (var name in new[] { "undo", "redo" })
        {
            var button = enabled.Find($"[data-cmd={name}]");

            Assert.IsTrue(button.HasAttribute("disabled"), name);
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"), name);
        }

        foreach (var button in enabled.FindAll(".bit-mde-btn:not([data-cmd=undo]):not([data-cmd=redo])"))
        {
            Assert.IsFalse(button.HasAttribute("disabled"), button.GetAttribute("data-cmd"));
            Assert.IsFalse(button.HasAttribute("aria-disabled"), button.GetAttribute("data-cmd"));
        }

        enabled.Instance._OnHistoryChanged(true, true);

        enabled.WaitForAssertion(() =>
        {
            Assert.IsFalse(enabled.Find("[data-cmd=undo]").HasAttribute("disabled"));
            Assert.IsFalse(enabled.Find("[data-cmd=undo]").HasAttribute("aria-disabled"));
            Assert.IsFalse(enabled.Find("[data-cmd=redo]").HasAttribute("disabled"));
        });
    }

    [TestMethod]
    public void BitMarkdownEditorReadOnlyShouldPairAriaDisabledOnTheEditingButtonsAlone()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters => parameters.Add(p => p.ReadOnly, true));

        var bold = component.Find("[data-cmd=bold]");
        Assert.IsTrue(bold.HasAttribute("disabled"));
        Assert.AreEqual("true", bold.GetAttribute("aria-disabled"));

        // The heading menu holds nothing but commands, so the trigger is out of reach too.
        var heading = component.Find("[data-cmd=heading]");
        Assert.IsTrue(heading.HasAttribute("disabled"));
        Assert.AreEqual("true", heading.GetAttribute("aria-disabled"));

        var preview = component.Find("[data-cmd=preview]");
        Assert.IsFalse(preview.HasAttribute("disabled"));
        Assert.IsFalse(preview.HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitMarkdownEditorReadOnlyShouldPairAriaDisabledOnTheReplaceButtons()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters => parameters.Add(p => p.ReadOnly, true));

        component.Find("[data-cmd=find]").Click();

        var buttons = component.FindAll(".bit-mde-fnb");

        // Replace and Replace all are the last two: nothing may change a read-only text, while the search still works.
        foreach (var button in buttons.TakeLast(2))
        {
            Assert.IsTrue(button.HasAttribute("disabled"));
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        }

        foreach (var button in buttons.Take(buttons.Count - 2))
        {
            Assert.IsFalse(button.HasAttribute("disabled"));
            Assert.IsFalse(button.HasAttribute("aria-disabled"));
        }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldRenderTheBaseParametersOnItsRoot()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Id, "the-editor");
            parameters.Add(p => p.Class, "own-class");
            parameters.Add(p => p.Style, "margin:1px");
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        var root = component.Find(".bit-mde");

        Assert.AreEqual("the-editor", root.Id);
        Assert.IsTrue(root.ClassList.Contains("own-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        // Every id of a part is derived from the root's, so a page can point at them.
        Assert.AreEqual("the-editor-txt", component.Find(".bit-mde-txa").Id);
        Assert.AreEqual("the-editor-txt", component.Find(".bit-mde-tlb").GetAttribute("aria-controls"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldSplatTheAttributesItWasGivenOnItsRootAndDropTheActivationHandlersWhileDisabled()
    {
        var clicked = 0;

        IRenderedComponent<BitMarkdownEditor> Render(bool disabled) => Context.Render(builder =>
        {
            builder.OpenComponent<BitMarkdownEditor>(0);
            builder.AddAttribute(1, "data-test", "editor");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked++));
            builder.AddAttribute(3, nameof(BitMarkdownEditor.Disabled), disabled);
            builder.CloseComponent();
        }).FindComponent<BitMarkdownEditor>();

        var disabled = Render(true);
        var root = disabled.Find(".bit-mde");

        Assert.AreEqual("editor", root.GetAttribute("data-test"));

        // A disabled editor runs none of the page's activation handlers, the way a disabled native control does.
        Assert.ThrowsExactly<MissingEventHandlerException>(() => root.Click());
        Assert.AreEqual(0, clicked);

        var enabled = Render(false);

        enabled.Find(".bit-mde").Click();

        Assert.AreEqual(1, clicked);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldApplyItsClassesAndStylesToTheLabelTheButtonsAndTheDescription()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Label, "Notes");
            parameters.Add(p => p.Description, "Markdown.");
            parameters.Add(p => p.Classes, new BitMarkdownEditorClassStyles { Label = "c-label", ToolbarButton = "c-button" });
            parameters.Add(p => p.Styles, new BitMarkdownEditorClassStyles
            {
                Root = "color:red",
                Label = "color:orange",
                Toolbar = "color:yellow",
                ToolbarButton = "color:green",
                TextArea = "color:blue",
                Preview = "color:indigo",
                Description = "color:violet",
            });
        });

        StringAssert.Contains(component.Find(".bit-mde").GetAttribute("style"), "color:red");

        var label = component.Find(".bit-mde-lbl");
        Assert.IsTrue(label.ClassList.Contains("c-label"));
        StringAssert.Contains(label.GetAttribute("style"), "color:orange");

        StringAssert.Contains(component.Find(".bit-mde-tlb").GetAttribute("style"), "color:yellow");

        // Every top-level button, the menu triggers included; the items inside a menu are the menu's own.
        foreach (var button in component.FindAll(".bit-mde-tlb > .bit-mde-btn, .bit-mde-dd > .bit-mde-btn"))
        {
            Assert.IsTrue(button.ClassList.Contains("c-button"), button.GetAttribute("data-cmd"));
            StringAssert.Contains(button.GetAttribute("style"), "color:green", button.GetAttribute("data-cmd"));
        }

        StringAssert.Contains(component.Find(".bit-mde-txa").GetAttribute("style"), "color:blue");
        StringAssert.Contains(component.Find(".bit-mde-ppn").GetAttribute("style"), "color:indigo");
        StringAssert.Contains(component.Find(".bit-mde-des").GetAttribute("style"), "color:violet");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldSeedItsTextAreaAndItsCountsFromTheDefaultValue()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "one two three");
        });

        // What a static page shows before the script seeds the textarea, and what the status bar counts from the start.
        Assert.AreEqual("one two three", component.Find(".bit-mde-txa").GetAttribute("value"));
        StringAssert.Contains(component.Find(".bit-mde-sbr").TextContent, "3 words");
        StringAssert.Contains(component.Find(".bit-mde-sbr").TextContent, "13 chars");
        Assert.AreEqual("one two three", component.Instance.Value);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldUploadAnImageThroughItsHandler()
    {
        BitMarkdownEditorImageUploadInfo? uploaded = null;

        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnImageUpload, info =>
            {
                uploaded = info;
                return Task.FromResult<string?>("https://cdn.example.com/a.png");
            });
        });

        var url = await component.Instance._UploadImage("a.png", Convert.ToBase64String(new byte[] { 1, 2, 3 }), "image/png");

        Assert.AreEqual("https://cdn.example.com/a.png", url);
        Assert.AreEqual("a.png", uploaded!.Value.FileName);
        Assert.AreEqual("image/png", uploaded.Value.ContentType);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, uploaded.Value.Data);
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldRefuseAnUploadItCannotHandOver()
    {
        var calls = 0;

        var without = RenderComponent<BitMarkdownEditor>();
        Assert.IsNull(await without.Instance._UploadImage("a.png", Convert.ToBase64String(new byte[] { 1 }), "image/png"));

        var readOnly = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnImageUpload, _ => { calls++; return Task.FromResult<string?>("x"); });
        });
        Assert.IsNull(await readOnly.Instance._UploadImage("a.png", Convert.ToBase64String(new byte[] { 1 }), "image/png"));

        var garbled = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.OnImageUpload, _ => { calls++; return Task.FromResult<string?>("x"); });
        });
        Assert.IsNull(await garbled.Instance._UploadImage("a.png", "not base64!", "image/png"));

        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldSendTheRestOfItsConfigToTheScript()
    {
        RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.SyncScroll, false);
            parameters.Add(p => p.ChangeDebounceTime, 250);
            parameters.Add(p => p.AutoSaveId, "draft-1");
            parameters.Add(p => p.OnImageUpload, _ => Task.FromResult<string?>(null));
        });

        var config = Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.init"].Last().Arguments[5]!;

        object? Read(string property) => config.GetType().GetProperty(property)!.GetValue(config);

        Assert.AreEqual(false, Read("SyncScroll"));
        Assert.AreEqual(250, Read("ChangeDebounceMs"));
        Assert.AreEqual("draft-1", Read("AutoSaveKey"));
        Assert.AreEqual(true, Read("ImageUpload"));
        // The default texts travel with it, so the script never hard-codes a word of its own.
        Assert.AreEqual(new BitMarkdownEditorTexts().UploadingText, Read("UploadingText"));
    }

    [TestMethod]
    public void BitMarkdownEditorShouldHoldThePageStillWhenFullScreenIsSetFromOutside()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);

        // A FullScreen the page sets after the first render takes the hold the toggle button would have.
        component.Render(parameters => parameters.Add(p => p.FullScreen, true));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
        Assert.IsTrue(component.Find(".bit-mde").ClassList.Contains("bit-mde-fsc"));

        component.Render(parameters => parameters.Add(p => p.FullScreen, false));

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
        Assert.IsFalse(component.Find(".bit-mde").ClassList.Contains("bit-mde-fsc"));
    }

    [TestMethod]
    public async Task BitMarkdownEditorShouldAnswerAnEmptySearchWithoutAskingTheScript()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        Assert.AreEqual(0, await component.Instance.Replace(string.Empty, "x"));
        Assert.AreEqual(BitMarkdownEditorFindResult.None, await component.Instance.FindNext(string.Empty));
        Assert.AreEqual(BitMarkdownEditorFindResult.None, await component.Instance.FindPrevious(string.Empty));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.replaceAll"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.find"].Count);
    }

    [TestMethod]
    public async Task BitMarkdownEditorFindPanelShouldWalkBackwardsOnShiftEnter()
    {
        Context.JSInterop.Setup<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.find", _ => true).SetResult(new(3, 2));

        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.Find(".bit-mde-fni").Input("md");
        await component.Find(".bit-mde-fni").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        // The direction is the fourth argument: id, search, matchCase, backwards, focusEditor.
        var invocation = Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.find"].Single();

        Assert.AreEqual(true, invocation.Arguments[3]);
        Assert.AreEqual(false, invocation.Arguments[2]);
        Assert.AreEqual("2 of 3", component.Find(".bit-mde-fns").TextContent);

        // The previous-match button walks the same way.
        component.FindAll(".bit-mde-fnb").Single(b => b.GetAttribute("aria-label") == "Previous match").Click();

        Assert.AreEqual(true, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.find"].Last().Arguments[3]);
    }

    [TestMethod]
    public async Task BitMarkdownEditorFindPanelShouldSearchCaseSensitivelyOnceMatchCaseIsOn()
    {
        Context.JSInterop.Setup<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.find", _ => true).SetResult(new(1, 1));

        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.Find(".bit-mde-fni").Input("Md");

        // Turning the toggle on with a term in the box searches again at once.
        component.Find(".bit-mde-fnc").Click();

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.find"].Single();

        Assert.AreEqual(true, invocation.Arguments[2]);
        Assert.AreEqual("1 of 1", component.Find(".bit-mde-fns").TextContent);
    }

    [TestMethod]
    public async Task BitMarkdownEditorReplaceFieldShouldReplaceEverythingOnAltEnterToo()
    {
        Context.JSInterop.Setup<int>("BitBlazorUI.MarkdownEditor.replaceAll", _ => true).SetResult(2);
        Context.JSInterop.Setup<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.find", _ => true).SetResult(BitMarkdownEditorFindResult.None);

        var component = RenderComponent<BitMarkdownEditor>();
        await component.Instance._OnShortcut("find");

        component.FindAll(".bit-mde-fni")[0].Input("cat");
        component.FindAll(".bit-mde-fni")[1].Input("dog");

        await component.FindAll(".bit-mde-fni")[1].KeyDownAsync(new KeyboardEventArgs { Key = "Enter", AltKey = true });

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.replaceAll"].Count);
        // What is left is reported: a replacement containing the search term still has matches.
        Assert.AreEqual("No results", component.Find(".bit-mde-fns").TextContent);
    }

    [TestMethod]
    public async Task BitMarkdownEditorHelpDialogShouldCloseOnItsOwnShortcutAndLeaveAModifiedEscapeAlone()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=help]").Click();

        // An Escape with a modifier is the surrounding surface's.
        await component.Find(".bit-mde-hcr").KeyDownAsync(new KeyboardEventArgs { Key = "Escape", ShiftKey = true });
        Assert.AreEqual(1, component.FindAll(".bit-mde-hcr").Count);

        // The shortcut that opened the dialog closes it, keyed by the physical key the way the script keys it.
        await component.Find(".bit-mde-hcr").KeyDownAsync(new KeyboardEventArgs { Key = "?", Code = "Slash", CtrlKey = true });
        Assert.AreEqual(0, component.FindAll(".bit-mde-hcr").Count);
    }

    [TestMethod]
    public void BitMarkdownEditorHelpDialogShouldCloseOnAClickOnItsBackdrop()
    {
        var component = RenderComponent<BitMarkdownEditor>();

        component.Find("[data-cmd=help]").Click();
        Assert.AreEqual(1, component.FindAll(".bit-mde-hcr").Count);

        component.Find(".bit-mde-hlp").Click();

        Assert.AreEqual(0, component.FindAll(".bit-mde-hcr").Count);
        // The dialog held the focus, so it is handed back to the editor.
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.MarkdownEditor.focus"].Count);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNotRenderAgainForACaretMoveNothingShows()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, false);
        });

        var renders = component.RenderCount;

        // Neither a format to light up nor a position to print: the move costs no render.
        component.Instance._OnSelectionChanged(2, 2, "plain", 3, 2, 0);
        component.Instance._OnHistoryChanged(false, false);

        Assert.AreEqual(renders, component.RenderCount);

        // Turning the status bar's readout on shows where the caret actually is, not where it was last shown.
        component.Render(parameters => parameters.Add(p => p.ShowCursorPosition, true));

        Assert.AreEqual("Ln 3, Col 2", component.Find(".bit-mde-pos").TextContent);
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNameACustomItemByItsTitleOverItsText()
    {
        var component = RenderComponent<BitMarkdownEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, [new() { Name = "save", Title = "Save the draft", Text = "Save", Shortcut = "Ctrl+S", Type = BitMarkdownEditorToolbarItemType.Custom, OnClick = _ => Task.CompletedTask }]);
        });

        var button = component.Find("[data-cmd=save]");

        Assert.AreEqual("Save the draft", button.GetAttribute("aria-label"));
        Assert.AreEqual("Save the draft (Ctrl+S)", button.GetAttribute("title"));
        Assert.AreEqual("Control+S", button.GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("Save", button.QuerySelector(".bit-mde-mit")!.TextContent);
    }
}
