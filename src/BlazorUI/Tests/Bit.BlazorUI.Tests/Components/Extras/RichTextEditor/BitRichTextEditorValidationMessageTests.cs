using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the bound field shown in the error
/// line under the editor and referenced by the editing surface (and the source view), giving way to the app's own
/// ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitRichTextEditorValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Body field is required.";

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitRichTextEditorShowValidationMessageShouldShowTheMessageOfTheFormUnderTheEditor()
    {
        var model = new FormModel();
        var validCount = 0;
        var invalidCount = 0;

        var component = RenderComponent<EditForm>(parameters =>
        {
            parameters.Add(p => p.Model, model);
            parameters.Add(p => p.OnValidSubmit, () => validCount++);
            parameters.Add(p => p.OnInvalidSubmit, () => invalidCount++);
            parameters.Add(p => p.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();

                builder.OpenComponent<BitRichTextEditor>(1);
                builder.AddAttribute(2, nameof(BitRichTextEditor.ShowValidationMessage), true);
                builder.AddAttribute(3, nameof(BitRichTextEditor.Value), model.Body);
                builder.AddAttribute(4, nameof(BitRichTextEditor.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Body = v));
                builder.AddAttribute(5, nameof(BitRichTextEditor.ValueExpression), (Expression<Func<string?>>)(() => model.Body));
                builder.CloseComponent();
            }));
        });

        // Nothing is shown before the form has anything to say.
        Assert.AreEqual(0, component.FindAll(".bit-rte-erm").Count);

        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var error = component.Find(".bit-rte-erm");
            var surface = component.Find(".bit-rte-edt");

            Assert.AreEqual(1, invalidCount);
            Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
            Assert.AreEqual($"{error.Id} {component.Find(".bit-rte-hint").Id}", surface.GetAttribute("aria-describedby"));
            Assert.AreEqual("true", surface.GetAttribute("aria-invalid"));
        });

        // An edit the form accepts takes the line, and the reference to it, away again.
        var editor = component.FindComponent<BitRichTextEditor>().Instance;
        component.InvokeAsync(() => editor._OnContentChanged("<p>typed</p>", new BitRichTextEditorContentFacts(true, false, 5, 1))).Wait();

        Assert.IsNotNull(model.Body);

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, component.FindAll(".bit-rte-erm").Count);
            Assert.AreEqual(component.Find(".bit-rte-hint").Id, component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));
        });

        component.Find("form").Submit();

        Assert.AreEqual(1, validCount);
    }

    [TestMethod]
    public void BitRichTextEditorShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(FormModel.Body));
        var store = new ValidationMessageStore(editContext);

        var component = RenderEditor(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "Keep it under a page.");
            editContext.NotifyValidationStateChanged();
        }).Wait();

        component.WaitForAssertion(() => Assert.AreEqual("Keep it under a page.", component.Find(".bit-rte-erm").TextContent.Trim()));

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "No links to other sites.");
            store.Add(field, "No images.");
            editContext.NotifyValidationStateChanged();
        }).Wait();

        component.WaitForAssertion(() => Assert.AreEqual("No links to other sites. No images.", component.Find(".bit-rte-erm").TextContent.Trim()));
    }

    [TestMethod]
    public void BitRichTextEditorWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderEditor(editContext, model);

        component.InvokeAsync(() => editContext.Validate()).Wait();

        // The editor is marked as before, and the message stays where the page puts it.
        component.WaitForAssertion(() => Assert.AreEqual("true", component.Find(".bit-rte-edt").GetAttribute("aria-invalid")));
        Assert.AreEqual(0, component.FindAll(".bit-rte-erm").Count);
        Assert.AreEqual(component.Find(".bit-rte-hint").Id, component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRichTextEditorErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderEditor(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "The server rejected this content.");
        });

        component.InvokeAsync(() => editContext.Validate()).Wait();

        Assert.AreEqual(1, component.FindAll(".bit-rte-erm").Count);
        Assert.AreEqual("The server rejected this content.", component.Find(".bit-rte-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RequiredMessage, component.Find(".bit-rte-erm").TextContent.Trim());
        Assert.AreEqual($"{component.Find(".bit-rte-erm").Id} {component.Find(".bit-rte-hint").Id}",
                        component.Find(".bit-rte-edt").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderEditor(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() => editContext.Validate()).Wait();

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        component.WaitForAssertion(() => Assert.AreEqual(RequiredMessage, component.Find(".bit-rte-erm").TextContent.Trim()));
        Assert.IsFalse(component.FindAll("[role=status]").Any(r => r.TextContent.Contains(RequiredMessage)));
    }

    [TestMethod]
    public void BitRichTextEditorShouldDescribeTheSurfaceWithTheMessageFirst()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderEditor(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Description, "Plain words work best.");
            parameters.Add(p => p.ShowCount, true);
        });

        component.InvokeAsync(() => editContext.Validate()).Wait();

        // What is wrong first, then the helper text, the count, and last the hint about the keyboard shortcuts.
        component.WaitForAssertion(() =>
        {
            var ids = component.Find(".bit-rte-edt").GetAttribute("aria-describedby")!.Split(' ');

            Assert.AreEqual(4, ids.Length);
            Assert.AreEqual(component.Find(".bit-rte-erm").Id, ids[0]);
            Assert.AreEqual(component.Find(".bit-rte-dsc").Id, ids[1]);
            Assert.AreEqual(component.Find(".bit-rte-cnt").Id, ids[2]);
            Assert.AreEqual(component.Find(".bit-rte-hint").Id, ids[3]);
        });
    }

    [TestMethod]
    public void BitRichTextEditorSourceViewShouldBeDescribedByTheMessageToo()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderEditor(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Toolbar, BitRichTextEditorToolbar.Source);
        });

        component.InvokeAsync(() => editContext.Validate()).Wait();

        component.Find("button[aria-label='HTML source view']").Click();

        // The source view holds the same field, so it says what is wrong with it the same way.
        component.WaitForAssertion(() =>
            Assert.AreEqual(component.Find(".bit-rte-erm").Id, component.Find(".bit-rte-src").GetAttribute("aria-describedby")));
    }

    [TestMethod]
    public void BitRichTextEditorBoundToNothingShouldShowNoMessage()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderEditor(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate()).Wait();

        // The model has a message, but the editor has no member of it the message could belong to.
        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.AreEqual(0, component.FindAll(".bit-rte-erm").Count);
    }

    [TestMethod]
    public void BitRichTextEditorShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every editor in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitRichTextEditorParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitRichTextEditor>(0);
                builder.AddAttribute(1, nameof(BitRichTextEditor.Value), model.Body);
                builder.AddAttribute(2, nameof(BitRichTextEditor.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Body = v));
                builder.AddAttribute(3, nameof(BitRichTextEditor.ValueExpression), (Expression<Func<string?>>)(() => model.Body));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate()).Wait();

        component.WaitForAssertion(() => Assert.AreEqual(RequiredMessage, component.Find(".bit-rte-erm").TextContent.Trim()));
    }

    private IRenderedComponent<BitRichTextEditor> RenderEditor(EditContext editContext, FormModel model,
                                                               Action<ComponentParameterCollectionBuilder<BitRichTextEditor>>? configure = null,
                                                               bool bound = true)
    {
        return RenderComponent<BitRichTextEditor>(parameters =>
        {
            parameters.AddCascadingValue(editContext);

            if (bound)
            {
                parameters.Add(p => p.Value, model.Body);
                parameters.Add(p => p.ValueChanged, (string? v) => model.Body = v);
                parameters.Add(p => p.ValueExpression, () => model.Body);
            }

            configure?.Invoke(parameters);
        });
    }

    private sealed class FormModel
    {
        [Required]
        public string? Body { get; set; }
    }
}
