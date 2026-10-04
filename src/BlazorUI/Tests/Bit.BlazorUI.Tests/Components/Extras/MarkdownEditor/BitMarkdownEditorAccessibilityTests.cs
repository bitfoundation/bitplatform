using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
        Assert.IsTrue(component.Find(".bit-mde-lbl").ClassList.Contains("bit-mde-req"));
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
        var component = RenderComponent<BitMarkdownEditor>(parameters => parameters.Add(p => p.IsEnabled, false));

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
}
