using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

/// <summary>
/// What a screen reader and a keyboard get from the editor: how the surface is named, described and marked required
/// or invalid, what is announced and what is not, where the focus goes when a panel or a menu closes, and which
/// controls are decorative.
/// </summary>
[TestClass]
public class BitRichTextEditorAccessibilityTests : BunitTestContext
{
    private const string RestoreFocus = "BitBlazorUI.RichTextEditor.restoreFocus";

    private static AngleSharp.Dom.IElement ButtonByLabel(IRenderedComponent<BitRichTextEditor> component, string ariaLabel)
        => component.Find($"button[aria-label='{ariaLabel}']");

    [TestMethod]
    public void BitRichTextEditorLabelShouldNameTheSurface()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Label, "Description");
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.AriaLabel, "Ignored while a label is shown");
        });

        var label = component.Find(".bit-rte-lbl");
        var surface = component.Find(".bit-rte-edt");

        Assert.AreEqual("Description", label.TextContent);
        Assert.IsTrue(label.ClassList.Contains("bit-rte-req"));
        Assert.AreEqual(label.Id, surface.GetAttribute("aria-labelledby"));
        Assert.IsFalse(surface.HasAttribute("aria-label"), "The surface carries two names.");
        Assert.AreEqual("true", surface.GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitRichTextEditorLabelClickShouldFocusTheText()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Label, "Description"));

        component.Find(".bit-rte-lbl").Click();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.focus");
    }

    [TestMethod]
    public void BitRichTextEditorShouldNotBeRequiredOrLabelledByDefault()
    {
        var component = RenderComponent<BitRichTextEditor>();

        var surface = component.Find(".bit-rte-edt");

        Assert.AreEqual(0, component.FindAll(".bit-rte-lbl").Count);
        Assert.IsFalse(surface.HasAttribute("aria-required"));
        Assert.IsFalse(surface.HasAttribute("aria-invalid"));
        Assert.IsFalse(surface.HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldReportAnInvalidFieldOfItsEditForm()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.AddChildContent<BitRichTextEditor>(editor =>
            {
                editor.Add(p => p.Value, model.Body);
                editor.Add(p => p.ValueExpression, () => model.Body);
            });
        });

        Assert.IsFalse(component.Find(".bit-rte").ClassList.Contains("bit-inv"));

        component.InvokeAsync(() =>
        {
            messages.Add(FieldIdentifier.Create(() => model.Body), "The body is required.");
            editContext.NotifyValidationStateChanged();
        }).Wait();

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Find(".bit-rte").ClassList.Contains("bit-inv"));
            Assert.AreEqual("true", component.Find(".bit-rte-edt").GetAttribute("aria-invalid"));
        });

        component.InvokeAsync(() =>
        {
            messages.Clear();
            editContext.NotifyValidationStateChanged();
        }).Wait();

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(component.Find(".bit-rte").ClassList.Contains("bit-inv"));
            Assert.IsFalse(component.Find(".bit-rte-edt").HasAttribute("aria-invalid"));
        });
    }

    [TestMethod]
    public void BitRichTextEditorCountShouldDescribeTheSurfaceWithoutReadingItselfOut()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.ShowCount, true);
            parameters.Add(p => p.MaxLength, 5);
        });

        var count = component.Find(".bit-rte-cnt");

        // A live counter would be read out after every pause in typing.
        Assert.IsFalse(count.HasAttribute("aria-live"));
        Assert.IsFalse(count.HasAttribute("role"));
        Assert.AreEqual($"{count.Id} {component.Find(".bit-rte-hint").Id}", component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));

        // Only reaching the limit is announced.
        var limit = component.Find(".bit-rte-cnt-over");
        Assert.AreEqual("status", limit.GetAttribute("role"));
        Assert.AreEqual("", limit.TextContent);

        component.InvokeAsync(() => component.Instance._OnFactsChanged(new BitRichTextEditorContentFacts(true, false, 5, 1))).Wait();

        Assert.AreEqual("limit reached", component.Find(".bit-rte-cnt-over").TextContent);
    }

    [TestMethod]
    public void BitRichTextEditorShouldDescribeTheSurfaceByItsError()
    {
        var component = RenderComponent<BitRichTextEditor>();

        component.InvokeAsync(() => component.Instance._OnClientError("invalid-image", "image-unsupported-type", "Bad image.", null)).Wait();

        var error = component.Find(".bit-rte-err");
        Assert.AreEqual("alert", error.GetAttribute("role"));
        Assert.AreEqual($"{error.Id} {component.Find(".bit-rte-hint").Id}", component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRichTextEditorDescriptionShouldDescribeTheSurface()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Description, "What changed and why.");
            parameters.Add(p => p.ShowCount, true);
        });

        var description = component.Find(".bit-rte-dsc");

        Assert.AreEqual("What changed and why.", description.TextContent);
        Assert.AreEqual($"{description.Id} {component.Find(".bit-rte-cnt").Id} {component.Find(".bit-rte-hint").Id}", component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));
        Assert.IsFalse(component.Find(".bit-rte-edt").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitRichTextEditorErrorMessageShouldMarkTheEditorInvalidAndBeAnnouncedOnce()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Description, "Help."));

        Assert.AreEqual(0, component.FindAll(".bit-rte-erm").Count);
        Assert.AreEqual("", component.Find(".bit-rte-ann").TextContent);

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "Refused."));

        var message = component.Find(".bit-rte-erm");
        var surface = component.Find(".bit-rte-edt");

        Assert.AreEqual("Refused.", message.TextContent);
        Assert.IsFalse(message.HasAttribute("role"), "The message would be read twice: by its own role and by the status region.");
        Assert.IsTrue(component.Find(".bit-rte").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", surface.GetAttribute("aria-invalid"));
        // What is wrong is read before the helper text.
        Assert.AreEqual($"{message.Id} {component.Find(".bit-rte-dsc").Id} {component.Find(".bit-rte-hint").Id}", surface.GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused.", component.Find(".bit-rte-ann").TextContent);

        // A render that keeps the same message does not say it again.
        component.Render(parameters => parameters.Add(p => p.Placeholder, "Write..."));
        Assert.AreEqual("Refused.", component.Find(".bit-rte-ann").TextContent);

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.AreEqual(0, component.FindAll(".bit-rte-erm").Count);
        Assert.IsFalse(component.Find(".bit-rte").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-rte-edt").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitRichTextEditorInvalidShouldMarkTheEditorWithoutAMessage()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Invalid, true));

        Assert.IsTrue(component.Find(".bit-rte").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-rte-edt").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-rte-erm").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorSourceToggleByPointerShouldMoveTheFocusIntoTheNewView()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.RichTextEditor.validateHtml", _ => true).SetResult(true);
        Context.JSInterop.Setup<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", _ => true).SetResult("<p>edited</p>");

        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Source));

        // A pointer click (detail 1): the surface that held the caret is hidden, so the source view takes the focus.
        await ButtonByLabel(component, "HTML source view").ClickAsync(new MouseEventArgs { Detail = 1 });
        Assert.AreEqual(1, component.FindAll(".bit-rte-src").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus"));

        // And back: the text takes it again.
        await ButtonByLabel(component, "HTML source view").ClickAsync(new MouseEventArgs { Detail = 1 });
        Assert.AreEqual(0, component.FindAll(".bit-rte-src").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke(RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorSourceToggleByKeyboardShouldKeepTheFocusOnTheButton()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Source));

        // A click raised by Enter or Space reports no click count.
        await ButtonByLabel(component, "HTML source view").ClickAsync(new MouseEventArgs { Detail = 0 });

        Assert.AreEqual(1, component.FindAll(".bit-rte-src").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitRichTextEditorWithoutAPaletteShouldKeepTheBrowsersColorPicker()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Color));

        Assert.AreEqual(2, component.FindAll(".bit-rte-tlb input[type=color]").Count);
        Assert.AreEqual(0, component.FindAll("button[aria-label='Text color']").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorColorPaletteShouldBeANamedGridOfSwatches()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Color);
            parameters.Add(p => p.ColorPalette, [new("#2563eb", "Blue"), new("#DC2626")]);
        });

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState { ForeColor = "#dc2626" }));

        var toggle = ButtonByLabel(component, "Text color");
        Assert.AreEqual(0, component.FindAll(".bit-rte-tlb input[type=color]").Count);
        Assert.AreEqual("false", toggle.GetAttribute("aria-expanded"));

        await toggle.ClickAsync(new());

        var panel = component.Find(".bit-rte-bar");
        Assert.AreEqual("group", panel.GetAttribute("role"));
        Assert.AreEqual("Text color", panel.GetAttribute("aria-label"));
        Assert.AreEqual(panel.Id, ButtonByLabel(component, "Text color").GetAttribute("aria-controls"));
        Assert.AreEqual("true", ButtonByLabel(component, "Text color").GetAttribute("aria-expanded"));

        var swatches = component.FindAll(".bit-rte-swatch");
        Assert.AreEqual(2, swatches.Count);
        // Named by its name, or by its value when it has none; the color under the caret is the pressed one.
        Assert.AreEqual("Blue", swatches[0].GetAttribute("aria-label"));
        Assert.AreEqual("false", swatches[0].GetAttribute("aria-pressed"));
        Assert.AreEqual("#DC2626", swatches[1].GetAttribute("aria-label"));
        Assert.AreEqual("true", swatches[1].GetAttribute("aria-pressed"));

        // The swatches are one tab stop, and the focus moves onto them.
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.enableGridRoving"));
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus"));

        await component.FindAll(".bit-rte-swatch")[0].ClickAsync(new());

        var apply = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.applyColor");
        Assert.AreEqual("fore", apply.Arguments[1]);
        Assert.AreEqual("#2563eb", apply.Arguments[2]);
        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke(RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorColorPanelShouldFollowTheButtonThatOpenedItAndCloseOnEscape()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Color);
            parameters.Add(p => p.ColorPalette, [new("#fff3a3", "Soft yellow")]);
        });

        await ButtonByLabel(component, "Text color").ClickAsync(new());
        await ButtonByLabel(component, "Highlight color").ClickAsync(new());

        Assert.AreEqual("Highlight color", component.Find(".bit-rte-bar").GetAttribute("aria-label"));
        Assert.AreEqual("false", ButtonByLabel(component, "Text color").GetAttribute("aria-expanded"));

        await component.Find(".bit-rte-swatch").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
    }

    [TestMethod]
    [DataRow(BitRichTextEditorToolbar.Image, 1, "Image inserted. Select an image and use the image button to describe it.")]
    [DataRow(BitRichTextEditorToolbar.Inline, 3, "3 images inserted.")]
    public void BitRichTextEditorDroppedImagesShouldBeAnnouncedWithWhereToDescribeThem(BitRichTextEditorToolbar toolbar, int count, string expected)
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, toolbar));

        component.InvokeAsync(() => component.Instance._OnImagesInserted(count)).Wait();

        Assert.AreEqual(expected, component.Find(".bit-rte-ann").TextContent);
    }

    [TestMethod]
    public void BitRichTextEditorShouldAnnounceATickedTask()
    {
        var component = RenderComponent<BitRichTextEditor>();

        var announcer = component.Find(".bit-rte-ann");
        Assert.AreEqual("status", announcer.GetAttribute("role"));

        component.InvokeAsync(() => component.Instance._OnTaskToggled(true)).Wait();
        Assert.AreEqual("Task checked", component.Find(".bit-rte-ann").TextContent);

        component.InvokeAsync(() => component.Instance._OnTaskToggled(false)).Wait();
        Assert.AreEqual("Task unchecked", component.Find(".bit-rte-ann").TextContent);

        // The same message twice in a row still changes the live region, so it is spoken again.
        component.InvokeAsync(() => component.Instance._OnTaskToggled(false)).Wait();
        StringAssert.StartsWith(component.Find(".bit-rte-ann").TextContent, "Task unchecked");
        Assert.AreNotEqual("Task unchecked", component.Find(".bit-rte-ann").TextContent);
    }

    [TestMethod]
    public void BitRichTextEditorRemoveLinkShouldOnlyBeEnabledInsideALink()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link));

        Assert.IsTrue(ButtonByLabel(component, "Remove link").HasAttribute("disabled"));

        component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState { InLink = true })).Wait();

        Assert.IsFalse(ButtonByLabel(component, "Remove link").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitRichTextEditorToolbarGlyphsShouldBeDecorative()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.AllExtended));

        var buttons = component.FindAll(".bit-rte-tlb button");
        Assert.IsTrue(buttons.Count > 30);

        foreach (var button in buttons)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(button.GetAttribute("aria-label")), $"A toolbar button has no name: {button.OuterHtml}");
        }

        foreach (var svg in component.FindAll(".bit-rte-tlb svg"))
        {
            Assert.AreEqual("true", svg.GetAttribute("aria-hidden"));
            Assert.AreEqual("false", svg.GetAttribute("focusable"));
        }
    }

    [TestMethod]
    [DataRow("Insert or edit link", "Link URL")]
    [DataRow("Insert image", "Image URL")]
    [DataRow("Embed media", "Media URL")]
    [DataRow("Insert table", null)]
    public async Task BitRichTextEditorPanelShouldBeANamedGroupThatGivesTheFocusBackOnEscape(string toggle, string? input)
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.AllExtended));

        await ButtonByLabel(component, toggle).ClickAsync(new());

        var panel = component.Find(".bit-rte-bar");
        Assert.AreEqual("group", panel.GetAttribute("role"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(panel.GetAttribute("aria-label")));

        var field = input is null ? component.Find(".bit-rte-bar input[type=number]") : component.Find($".bit-rte-bar input[aria-label='{input}']");
        await field.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke(RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorPanelShouldCloseOnEscapeFromAnyOfItsControls()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link));

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        // Not a text field: the checkbox has no key handler of its own, the panel answers for it.
        await component.Find(".bit-rte-bar input[type=checkbox]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
    }

    [TestMethod]
    public async Task BitRichTextEditorLinkPanelShouldOfferToOpenTheLinkUnderTheCaret()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link));

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState
        {
            InLink = true,
            LinkHref = "https://example.com/docs"
        }));
        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        var open = component.Find(".bit-rte-bar a");
        Assert.AreEqual("https://example.com/docs", open.GetAttribute("href"));
        Assert.AreEqual("_blank", open.GetAttribute("target"));
        Assert.AreEqual("noopener noreferrer", open.GetAttribute("rel"));
    }

    [TestMethod]
    public async Task BitRichTextEditorLinkPanelShouldNotOfferToOpenAnUnsafeLink()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link));

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState
        {
            InLink = true,
            LinkHref = "javascript:alert(1)"
        }));
        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        Assert.AreEqual(0, component.FindAll(".bit-rte-bar a").Count);
    }

    [TestMethod]
    [DataRow(false, "BitBlazorUI.RichTextEditor.replaceCurrent")]
    [DataRow(true, "BitBlazorUI.RichTextEditor.replaceAll")]
    public async Task BitRichTextEditorReplaceBoxShouldReplaceOnEnter(bool ctrl, string expected)
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find));

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());
        await component.Find("input[aria-label='Find']").InputAsync(new() { Value = "fox" });
        var replace = component.Find("input[aria-label='Replace with']");
        await replace.InputAsync(new() { Value = "cat" });
        await component.Find("input[aria-label='Replace with']").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", CtrlKey = ctrl });

        Context.JSInterop.VerifyInvoke(expected);
    }

    [TestMethod]
    public async Task BitRichTextEditorFindPanelShouldBeASearchLandmarkThatClosesBackToTheText()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Find));

        await ButtonByLabel(component, "Find and replace").ClickAsync(new());

        var panel = component.Find(".bit-rte-bar");
        Assert.AreEqual("search", panel.GetAttribute("role"));

        await component.FindAll(".bit-rte-bar button").First(b => b.TextContent.Trim() == "Close").ClickAsync(new());

        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke(RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorShouldNotPullTheFocusIntoTheTextWhenAPanelIsOnlyOpened()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link));

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorEmojiPickerShouldBeOneTabStopThatEscapeCloses()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Emoji));

        await ButtonByLabel(component, "Insert emoji").ClickAsync(new());

        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.enableGridRoving"));

        var panel = component.Find(".bit-rte-emoji-panel");
        Assert.AreEqual("group", panel.GetAttribute("role"));

        await panel.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-rte-emoji-panel").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke(RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorSlashMenuEscapeShouldGiveTheFocusBack()
    {
        var component = RenderComponent<BitRichTextEditor>();

        await component.InvokeAsync(() => component.Instance._OnSlashTrigger());

        var filter = component.Find(".bit-rte-slash input");
        // The options are reached through aria-activedescendant, so they are not tab stops of their own.
        Assert.IsTrue(component.FindAll(".bit-rte-slash-item").All(o => o.GetAttribute("tabindex") == "-1"));

        await filter.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-rte-slash").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke(RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorImageWidthShouldBeTheKeyboardWayToResize()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image));

        await ButtonByLabel(component, "Insert image").ClickAsync(new());
        await component.Find(".bit-rte-bar input[type=url]").InputAsync(new() { Value = "https://example.com/a.png" });
        await component.Find(".bit-rte-bar input[type=number]").InputAsync(new() { Value = "240" });
        await component.FindAll(".bit-rte-bar button").First(b => b.TextContent.Trim() == "Insert").ClickAsync(new());

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.insertImageUrl");
        Assert.AreEqual(240, invocation.Arguments[3]);
    }

    [TestMethod]
    public async Task BitRichTextEditorImagePanelShouldOpenOnTheSelectedImageWidth()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Image));

        await component.InvokeAsync(() => component.Instance._OnSelectionChanged(new BitRichTextEditorSelectionState
        {
            ImageSelected = true,
            ImageSrc = "https://example.com/a.png",
            ImageAlt = "A",
            ImageWidth = 320
        }));

        await ButtonByLabel(component, "Edit image").ClickAsync(new());

        Assert.AreEqual("320", component.Find(".bit-rte-bar input[type=number]").GetAttribute("value"));

        await component.Find(".bit-rte-bar input[type=number]").InputAsync(new() { Value = "" });
        await component.FindAll(".bit-rte-bar button").First(b => b.TextContent.Trim() == "Update").ClickAsync(new());

        // An emptied width gives the image back its natural size.
        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.RichTextEditor.updateImage");
        Assert.IsNull(invocation.Arguments[3]);
    }

    [TestMethod]
    public void BitRichTextEditorDisabledSurfaceShouldSayItIsDisabled()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.Disabled, true));

        var surface = component.Find(".bit-rte-edt");

        Assert.AreEqual("true", surface.GetAttribute("aria-disabled"));
        Assert.IsFalse(surface.HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitRichTextEditorStickyToolbarShouldMarkTheRoot()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.StickyToolbar, true));

        Assert.IsTrue(component.Find(".bit-rte").ClassList.Contains("bit-rte-stk"));
    }

    [TestMethod]
    public async Task BitRichTextEditorAltZeroShouldListTheEffectiveShortcutsInAFocusedRegion()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Inline);
            // A custom chord is listed, and a default the host took over (ctrl+b) no longer reads "Bold".
            parameters.Add(p => p.KeyboardShortcuts, new Dictionary<string, string> { ["ctrl+shift+1"] = "h1", ["ctrl+b"] = "strikeThrough" });
        });

        Assert.AreEqual(0, component.FindAll(".bit-rte-help").Count);

        await component.InvokeAsync(() => component.Instance._OnHelpRequested());

        var region = component.Find(".bit-rte-help-scroll");
        Assert.AreEqual("region", region.GetAttribute("role"));
        Assert.AreEqual("0", region.GetAttribute("tabindex"));
        Assert.AreEqual(component.Find(".bit-rte-help-ttl").Id, region.GetAttribute("aria-labelledby"));

        var rows = component.FindAll(".bit-rte-help-tbl tbody tr")
                            .ToDictionary(r => r.QuerySelector("th")!.TextContent, r => string.Join(" ", r.QuerySelectorAll("kbd").Select(k => k.TextContent)));

        Assert.AreEqual("Ctrl+Shift+1", rows["Heading 1"]);
        Assert.AreEqual("Ctrl+B Ctrl+Shift+X", rows["Strikethrough"]);
        Assert.IsFalse(rows.ContainsKey("Bold"));
        Assert.AreEqual("Ctrl+Y Ctrl+Shift+Z", rows["Redo"]);
        // The link group is off, so its chord is the browser's.
        Assert.IsFalse(rows.ContainsKey("Insert or edit link"));
        Assert.IsFalse(rows.ContainsKey("Mention someone"));

        await component.Find(".bit-rte-help-scroll").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-rte-help").Count);
        component.WaitForAssertion(() => Context.JSInterop.VerifyInvoke(RestoreFocus));
    }

    [TestMethod]
    public async Task BitRichTextEditorHelpShouldListOnlyNavigationWhileReadOnly()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.ReadOnly, true));

        await component.InvokeAsync(() => component.Instance._OnHelpRequested());

        var actions = component.FindAll(".bit-rte-help-tbl tbody th").Select(th => th.TextContent).ToList();
        CollectionAssert.Contains(actions, "Move to the toolbar");
        CollectionAssert.DoesNotContain(actions, "Bold");
        CollectionAssert.DoesNotContain(actions, "Paste as plain text");
    }

    [TestMethod]
    public async Task BitRichTextEditorHelpGroupShouldToggleThePanelAndCloseTheOthers()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Link | BitRichTextEditorToolbar.Help));

        await ButtonByLabel(component, "Insert or edit link").ClickAsync(new());
        Assert.AreEqual(1, component.FindAll(".bit-rte-bar").Count);

        var help = ButtonByLabel(component, "Keyboard shortcuts");
        Assert.AreEqual("Alt+0", help.GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("false", help.GetAttribute("aria-expanded"));

        await help.ClickAsync(new());

        Assert.AreEqual(0, component.FindAll(".bit-rte-bar").Count);
        help = ButtonByLabel(component, "Keyboard shortcuts");
        Assert.AreEqual("true", help.GetAttribute("aria-expanded"));
        Assert.AreEqual(component.Find(".bit-rte-help").Id, help.GetAttribute("aria-controls"));

        await help.ClickAsync(new());
        Assert.AreEqual(0, component.FindAll(".bit-rte-help").Count);
    }

    [TestMethod]
    public void BitRichTextEditorSourceViewShouldNotBeDescribedByTheHelpHint()
    {
        var component = RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Source);
            parameters.Add(p => p.Description, "Help.");
        });

        ButtonByLabel(component, "HTML source view").Click();

        Assert.AreEqual(component.Find(".bit-rte-dsc").Id, component.Find(".bit-rte-src").GetAttribute("aria-describedby"));
    }

    private sealed class FormModel
    {
        public string? Body { get; set; }
    }
}
