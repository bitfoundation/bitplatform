using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field shown in the error line at
/// the foot of the picker and referenced by every control that holds the color, giving way to the app's own
/// ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitColorPickerValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Value field is required.";
    private const string ErrorRegion = ".bit-clp > [role=status].bit-clp-vhd";

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitColorPickerShowValidationMessageShouldShowTheMessageOfTheFormAtTheFootOfThePanel()
    {
        var component = RenderComponent<BitColorPickerValidationTest>(parameters => parameters.Add(p => p.ShowValidationMessage, true));

        // Nothing is shown before the form has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-clp-erm"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        var error = component.Find(".bit-clp-erm");

        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-clp").ClassList.Contains("bit-inv"));

        // Every control that holds the color says it is invalid, so every one of them is described by the reason: the
        // gradient, the sliders and the text fields. The swatches of the palette hold no value of their own.
        Assert.AreEqual(error.Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.IsTrue(component.FindAll(".bit-clp-inp").All(i => i.GetAttribute("aria-describedby") == error.Id));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-describedby") == error.Id));
        Assert.IsTrue(component.FindAll(".bit-clp-prt").All(b => b.HasAttribute("aria-describedby") is false));

        // The line is the last row of the panel, under the palette too.
        Assert.ContainsSingle(component.FindAll(".bit-clp-prs + .bit-clp-erm"));

        // A pick the form accepts takes the line, and the references to it, away again.
        component.FindAll(".bit-clp-prt")[0].Click();

        Assert.IsNotNull(component.Instance.TestModel.Value);
        Assert.IsEmpty(component.FindAll(".bit-clp-erm"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.HasAttribute("aria-describedby") is false));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitColorPickerShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitColorPickerTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "Not in the brand palette.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Not in the brand palette.", component.Find(".bit-clp-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Too close to the background.");
            store.Add(field, "Taken by another team.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too close to the background. Taken by another team.", component.Find(".bit-clp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitColorPickerWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The picker is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-clp-erm"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitColorPickerErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "A logo color has to be fully opaque.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-clp-erm"));
        Assert.AreEqual("A logo color has to be fully opaque.", component.Find(".bit-clp-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RequiredMessage, component.Find(".bit-clp-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-clp-erm").Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitColorPickerErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Your plan does not include custom theme colors.");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Upgrade");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the controls are described by.
        var labelId = component.Find(".bit-clp-rec").GetAttribute("aria-describedby");

        Assert.AreEqual("Upgrade", component.Find(".bit-clp-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-clp-erm").TextContent);
        Assert.AreEqual("Your plan does not include custom theme colors.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitColorPickerShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One picker is wired for the live region by an ErrorMessage written as a null for now, the other is not.
        var wired = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, null);
        });

        var plain = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        wired.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the control that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, wired.Find(".bit-clp-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, wired.Find(ErrorRegion).TextContent.Trim());

        // The messages do not wire a picker for a live region of its own either.
        Assert.AreEqual(RequiredMessage, plain.Find(".bit-clp-erm").TextContent.Trim());
        Assert.IsEmpty(plain.FindAll("[role=status]"));
    }

    [TestMethod]
    public void BitColorPickerShouldDescribeTheGradientWithTheMessageAheadOfTheContrast()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ShowContrast, true);
        });

        component.InvokeAsync(() => editContext.Validate());

        // What is wrong with the color is the more urgent thing to hear, then the verdict of the readout - which only
        // the first control is described by.
        var error = component.Find(".bit-clp-erm");
        var ids = component.Find(".bit-clp-rec").GetAttribute("aria-describedby")!.Split(' ');

        Assert.HasCount(2, ids);
        Assert.AreEqual(error.Id, ids[0]);
        Assert.AreEqual(component.Find(".bit-clp-cnr").Id, ids[1]);
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-describedby") == error.Id));
    }

    [TestMethod]
    public void BitColorPickerForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitColorPickerTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Refused by the server.");
        });

        var labelId = component.Find(".bit-clp-rec").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Not in the brand palette.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the controls; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-clp-erm").Id, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(ErrorRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitColorPickerThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoValidate, false);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-clp-erm"));

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.IsEmpty(component.FindAll(".bit-clp-erm"));
        Assert.IsNull(component.Find(".bit-clp-rec").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitColorPickerBoundToNothingShouldShowNoMessage()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the picker has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-clp-erm"));
    }

    [TestMethod]
    public void BitColorPickerShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitColorPickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every picker in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitColorPickerParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitColorPicker>(0);
                builder.AddAttribute(1, nameof(BitColorPicker.Value), model.Value);
                builder.AddAttribute(2, nameof(BitColorPicker.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Value = v));
                builder.AddAttribute(3, nameof(BitColorPicker.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-clp-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitColorPicker> RenderPicker(EditContext editContext, BitColorPickerTestModel model,
                                                            Action<ComponentParameterCollectionBuilder<BitColorPicker>>? configure = null,
                                                            bool bound = true)
    {
        return RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.ShowInputs, true);

            if (bound)
            {
                parameters.Add(p => p.Value, model.Value);
                parameters.Add(p => p.ValueChanged, (string? v) => model.Value = v);
                parameters.Add(p => p.ValueExpression, () => model.Value);
            }

            configure?.Invoke(parameters);
        });
    }
}
