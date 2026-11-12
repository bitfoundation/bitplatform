using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Slider;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field - the value, or the pair
/// of a ranged slider - shown in the error line under the slider and referenced by every thumb, giving way to the
/// app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitSliderValidationMessageTests : BunitTestContext
{
    private const string RangeMessage = "Pick at least 4";
    private const string BandMessage = "Pick a band at least 3 wide";

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitSliderShowValidationMessageShouldShowTheMessageOfTheFormUnderTheSlider()
    {
        var component = RenderComponent<BitSliderValidationTest>(parameters => parameters.Add(p => p.ShowValidationMessage, true));

        // Nothing is shown before the form has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-sld-erm"));
        Assert.IsNull(component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        var error = component.Find(".bit-sld-erm");
        var input = component.Find(".bit-sld-inp");

        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(RangeMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-sld").ClassList.Contains("bit-inv"));

        // A move the form accepts takes the line, and the reference to it, away again.
        component.Find(".bit-sld-inp").Input("5");

        Assert.AreEqual(5, component.Instance.TestModel.Days);
        Assert.IsEmpty(component.FindAll(".bit-sld-erm"));
        Assert.IsNull(component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitSliderShowValidationMessageShouldShowTheMessageOfARangeForBothThumbs()
    {
        var component = RenderComponent<BitSliderRangeValidationTest>(parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.Find("form").Submit();

        // The pair is one field, so it has one line, and both ends of it are described by that line.
        var error = component.Find(".bit-sld-erm");

        Assert.ContainsSingle(component.FindAll(".bit-sld-erm"));
        Assert.AreEqual(BandMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-sld-inp-lwr").GetAttribute("aria-describedby"));
        Assert.AreEqual(error.Id, component.Find(".bit-sld-inp-upr").GetAttribute("aria-describedby"));

        // Every move of a thumb is reported, so the line goes as soon as the pair is wide enough.
        component.Find(".bit-sld-inp-upr").Input("8");

        Assert.IsEmpty(component.FindAll(".bit-sld-erm"));
        Assert.IsNull(component.Find(".bit-sld-inp-lwr").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-sld-inp-upr").GetAttribute("aria-describedby"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitSliderShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitSliderTestModel.Days));
        var store = new ValidationMessageStore(editContext);

        var component = RenderSlider(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too few days.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too few days.", component.Find(".bit-sld-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "No weekends.");
            store.Add(field, "Ask your manager.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("No weekends. Ask your manager.", component.Find(".bit-sld-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitSliderWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSlider(editContext, model);

        component.InvokeAsync(() => editContext.Validate());

        // The slider is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-sld-inp").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-sld-erm"));
        Assert.IsNull(component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSliderErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSlider(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, "Your plan allows three days at most.");
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-sld-erm"));
        Assert.AreEqual("Your plan allows three days at most.", component.Find(".bit-sld-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        component.Render(parameters => parameters.Add(p => p.ErrorMessage, null));

        Assert.AreEqual(RangeMessage, component.Find(".bit-sld-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-sld-erm").Id, component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSliderErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSlider(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "The schedule service is unavailable.");
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Unavailable");
                builder.CloseElement();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the thumb is described by.
        var labelId = component.Find(".bit-sld-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Unavailable", component.Find(".bit-sld-erm b").TextContent);
        Assert.DoesNotContain(RangeMessage, component.Find(".bit-sld-erm").TextContent);
        Assert.AreEqual("The schedule service is unavailable.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitSliderShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One slider is wired for the live region by an ErrorMessage written as a null for now, the other is not.
        var wired = RenderSlider(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.ErrorMessage, null);
        });

        var plain = RenderSlider(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true));

        wired.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RangeMessage, wired.Find(".bit-sld-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, wired.Find("[role=status]").TextContent.Trim());

        // The messages do not wire a slider for a live region of its own either.
        Assert.AreEqual(RangeMessage, plain.Find(".bit-sld-erm").TextContent.Trim());
        Assert.IsEmpty(plain.FindAll("[role=status]"));
    }

    [TestMethod]
    public void BitSliderShouldDescribeTheThumbWithTheMessageAheadOfTheDescription()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSlider(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.AriaDescription, "Days you work from the office.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "page-hint" });
        });

        component.InvokeAsync(() => editContext.Validate());

        // What the page pointed the thumb at stays first, then what is wrong with the value, then the description.
        var ids = component.Find(".bit-sld-inp").GetAttribute("aria-describedby")!.Split(' ');

        Assert.HasCount(3, ids);
        Assert.AreEqual("page-hint", ids[0]);
        Assert.AreEqual(component.Find(".bit-sld-erm").Id, ids[1]);
        Assert.AreEqual("Days you work from the office.", component.Find($"#{ids[2]}").TextContent);
    }

    [TestMethod]
    public void BitSliderForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitSliderTestModel.Days));
        var store = new ValidationMessageStore(editContext);

        var component = RenderSlider(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.Invalid, true);
            parameters.Add(p => p.ErrorMessageAriaLabel, "Refused by the server.");
        });

        var labelId = component.Find(".bit-sld-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too few days.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the thumb; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-sld-erm").Id, component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find("[role=status]").TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSliderThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSlider(editContext, model, parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.NoValidate, false);
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-sld-erm"));

        component.Render(parameters => parameters.Add(p => p.NoValidate, true));

        Assert.IsEmpty(component.FindAll(".bit-sld-erm"));
        Assert.IsNull(component.Find(".bit-sld-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSliderBoundToNothingShouldShowNoMessage()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderSlider(editContext, model, parameters => parameters.Add(p => p.ShowValidationMessage, true), bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the slider has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-sld-erm"));
    }

    [TestMethod]
    public void BitSliderRangeBoundThroughItsEndsShouldShowNoMessage()
    {
        var model = new BitSliderRangeTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<BitSlider>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.IsRanged, true);
            parameters.Add(p => p.Max, 10);
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.LowerValue, model.Band!.Lower);
            parameters.Add(p => p.LowerValueChanged, (double v) => model.Band = new(v, model.Band!.Upper));
            parameters.Add(p => p.UpperValue, model.Band!.Upper);
            parameters.Add(p => p.UpperValueChanged, (double v) => model.Band = new(model.Band!.Lower, v));
        });

        component.InvokeAsync(() => editContext.Validate());

        // The two ends are two properties rather than one field, so the band's message belongs to neither of them.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-sld-erm"));
    }

    [TestMethod]
    public void BitSliderShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitSliderTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every slider in it.
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitSliderParams { ShowValidationMessage = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSlider>(0);
                builder.AddAttribute(1, nameof(BitSlider.Value), model.Days);
                builder.AddAttribute(2, nameof(BitSlider.ValueChanged), EventCallback.Factory.Create<double>(this, v => model.Days = v));
                builder.AddAttribute(3, nameof(BitSlider.ValueExpression), (Expression<Func<double>>)(() => model.Days));
                builder.CloseComponent();
            });
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RangeMessage, component.Find(".bit-sld-erm").TextContent.Trim());
    }

    private IRenderedComponent<BitSlider> RenderSlider(EditContext editContext, BitSliderTestModel model,
                                                       Action<ComponentParameterCollectionBuilder<BitSlider>>? configure = null,
                                                       bool bound = true)
    {
        return RenderComponent<BitSlider>(parameters =>
        {
            parameters.AddCascadingValue(editContext);
            parameters.Add(p => p.Max, 7);

            if (bound)
            {
                parameters.Add(p => p.Value, model.Days);
                parameters.Add(p => p.ValueChanged, (double v) => model.Days = v);
                parameters.Add(p => p.ValueExpression, () => model.Days);
            }

            configure?.Invoke(parameters);
        });
    }
}
