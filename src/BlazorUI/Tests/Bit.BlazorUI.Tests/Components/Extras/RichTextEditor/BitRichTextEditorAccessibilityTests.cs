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
        Assert.AreEqual(count.Id, component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));

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

        component.InvokeAsync(() => component.Instance._OnClientError("invalid-image", "Bad image.")).Wait();

        var error = component.Find(".bit-rte-err");
        Assert.AreEqual("alert", error.GetAttribute("role"));
        Assert.AreEqual(error.Id, component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));
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
        var component = RenderComponent<BitRichTextEditor>(parameters => parameters.Add(p => p.IsEnabled, false));

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

    private sealed class FormModel
    {
        public string? Body { get; set; }
    }
}
