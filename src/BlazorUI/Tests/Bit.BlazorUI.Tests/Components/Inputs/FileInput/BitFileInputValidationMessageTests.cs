using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.FileInput;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field shown in the error line
/// under the browse button and referenced by it, giving way to the app's own ErrorMessage, apart from the files the
/// file input rejects on its own, and never announced.
/// </summary>
[TestClass]
public class BitFileInputValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Value field is required.";

    // The live region of the error, told apart from the one the selection is announced out of.
    private const string ErrorRegion = ".bit-fin > [role=status].bit-fin-vhd";

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitFileInputShowValidationMessageShouldShowTheMessageOfTheFormUnderTheBrowseButton()
    {
        var component = RenderComponent<BitFileInputValidationTest>(parameters => parameters.Add(p => p.ShowValidationMessage, true));

        // Nothing is shown before the form has anything to say.
        Assert.AreEqual(0, component.FindAll(".bit-fin-erm").Count);
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        var button = component.Find(".bit-fin-lbl");
        var error = component.Find(".bit-fin-erm");

        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(1, component.FindAll(".bit-fin-lbl + .bit-fin-erm").Count);
        Assert.AreEqual(error.Id, button.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", button.GetAttribute("aria-invalid"));

        // A file the input rejects on its own says why in the list; the selection still holds no valid file, so the
        // line keeps saying what the form thinks of it.
        SetupFiles([new() { Name = "big.txt", Size = 100, FileId = "1" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual(1, component.FindAll(".bit-fin-msg").Count);
        Assert.AreEqual(RequiredMessage, component.Find(".bit-fin-erm").TextContent.Trim());

        // A valid file takes the line, and the reference to it, away again.
        SetupFiles([new() { Name = "a.txt", Size = 10, FileId = "2" }]);
        component.Find(".bit-fin-fi").Change(string.Empty);

        Assert.AreEqual("a.txt", component.Instance.TestModel.Value);
        Assert.AreEqual(0, component.FindAll(".bit-fin-erm").Count);
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitFileInputShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitFileInputTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderFileInput(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "Attach your resume.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Attach your resume.", component.Find(".bit-fin-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "One file only.");
            store.Add(field, "Name it after yourself.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("One file only. Name it after yourself.", component.Find(".bit-fin-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitFileInputWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderFileInput(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The browse button is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-fin-lbl").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-fin-erm").Count);
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderFileInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "Uploads are paused.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-fin-erm").Count);
        Assert.AreEqual("Uploads are paused.", component.Find(".bit-fin-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, (string?)null));

        Assert.AreEqual(RequiredMessage, component.Find(".bit-fin-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-fin-erm").Id, component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderFileInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Your storage is full.");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Free up space");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the browse button is described by.
        var labelId = component.Find(".bit-fin-lbl").GetAttribute("aria-describedby");

        Assert.AreEqual("Free up space", component.Find(".bit-fin-erm b").TextContent);
        Assert.IsFalse(component.Find(".bit-fin-erm").TextContent.Contains(RequiredMessage));
        Assert.AreEqual("Your storage is full.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitFileInputShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One file input is wired for the live region of its errors by an ErrorMessage written as a null for now, the
        // other is not.
        var wired = RenderFileInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, (string?)null);
        });

        var plain = RenderFileInput(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        wired.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        // The region the selection is announced out of says nothing of it either.
        Assert.AreEqual(RequiredMessage, wired.Find(".bit-fin-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, wired.Find(ErrorRegion).TextContent.Trim());
        Assert.IsFalse(wired.FindAll("[aria-live]").Any(r => r.TextContent.Contains(RequiredMessage)));

        // The messages do not wire a file input for a live region of its errors either.
        Assert.AreEqual(RequiredMessage, plain.Find(".bit-fin-erm").TextContent.Trim());
        Assert.AreEqual(0, plain.FindAll(ErrorRegion).Count);
        Assert.IsFalse(plain.FindAll("[aria-live]").Any(r => r.TextContent.Contains(RequiredMessage)));
    }

    [TestMethod]
    public void BitFileInputShouldDescribeTheBrowseButtonWithTheMessageAheadOfTheHint()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderFileInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Description, "PDF only, up to 5 MB.");
        });

        component.InvokeAsync(() => editContext.Validate());

        // In the order the two sit under the button: what is wrong with the selection first, then the hint.
        Assert.AreEqual(1, component.FindAll(".bit-fin-lbl + .bit-fin-erm + .bit-fin-dsc").Count);
        Assert.AreEqual($"{component.Find(".bit-fin-erm").Id} {component.Find(".bit-fin-dsc").Id}",
                        component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitFileInputTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderFileInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Refused by the server.");
        });

        var labelId = component.Find(".bit-fin-lbl").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Attach your resume.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the button; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-fin-erm").Id, component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(ErrorRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderFileInput(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoValidate, false);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-fin-erm").Count);

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.AreEqual(0, component.FindAll(".bit-fin-erm").Count);
        Assert.IsNull(component.Find(".bit-fin-lbl").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitFileInputBoundToNothingShouldShowNoMessage()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderFileInput(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the file input has no member of it the message could belong to.
        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.AreEqual(0, component.FindAll(".bit-fin-erm").Count);
    }

    [TestMethod]
    public void BitFileInputShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitFileInputTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every file input in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitFileInputParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFileInput>(0);
                builder.AddAttribute(1, nameof(BitFileInput.Value), model.Value);
                builder.AddAttribute(2, nameof(BitFileInput.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Value = v));
                builder.AddAttribute(3, nameof(BitFileInput.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-fin-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitFileInput> RenderFileInput(EditContext editContext, BitFileInputTestModel model,
                                                             Action<ComponentParameterCollectionBuilder<BitFileInput>>? configure = null,
                                                             bool bound = true)
    {
        return RenderComponent<BitFileInput>(parameters =>
        {
            parameters.AddCascadingValue(editContext);

            if (bound)
            {
                parameters.Add(p => p.Value, model.Value);
                parameters.Add(p => p.ValueChanged, (string? v) => model.Value = v);
                parameters.Add(p => p.ValueExpression, () => model.Value);
            }

            configure?.Invoke(parameters);
        });
    }

    private void SetupFiles(BitFileInputInfo[] files)
    {
        Context.JSInterop.Setup<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", _ => true).SetResult(files);
    }
}
