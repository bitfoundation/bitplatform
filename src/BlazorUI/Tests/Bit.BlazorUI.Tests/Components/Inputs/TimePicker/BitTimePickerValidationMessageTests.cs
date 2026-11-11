using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field - the one of a typed time
/// the picker refuses among them - shown in the error line under the picker and referenced by its input, giving way
/// to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitTimePickerValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Time field is required.";
    private const string LiveRegion = ".bit-tpc > [role=status]";

    [TestMethod]
    public void BitTimePickerShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheField()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitTimePicker.ShowValidationMessage)] = true,
            [nameof(BitTimePicker.AllowTextInput)] = true,
            [nameof(BitTimePicker.Culture)] = CultureInfo.InvariantCulture,
        });

        // Nothing is shown before the EditContext has anything to say.
        Assert.AreEqual(0, component.FindAll(".bit-tpc-erm").Count);
        Assert.IsNull(component.Find(".bit-tpc-inp").GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-tpc-erm");
        var input = component.Find(".bit-tpc-inp");

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-inv"));

        // A time the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-tpc-inp").Input("14:30");

        Assert.AreEqual(new TimeSpan(14, 30, 0), model.Time);
        Assert.AreEqual(0, component.FindAll(".bit-tpc-erm").Count);
        Assert.IsNull(component.Find(".bit-tpc-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-tpc-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitTimePickerShowValidationMessageShouldShowTheMessagesOfATypedTimeInAnEditForm()
    {
        var component = RenderComponent<BitTimePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.MinTime, new TimeSpan(8, 0, 0));
            parameters.Add(p => p.MaxTime, new TimeSpan(18, 0, 0));
            parameters.Add(p => p.AllowedMinutes, m => m % 15 == 0);
            parameters.Add(p => p.InvalidErrorMessage, "Not a time.");
            parameters.Add(p => p.OutOfRangeErrorMessage, "Between 08:00 and 18:00 only.");
            parameters.Add(p => p.DisallowedTimeErrorMessage, "Quarter hours only.");
        });

        component.Find("form").Submit();

        Assert.AreEqual(RequiredMessage, component.Find(".bit-tpc-erm").TextContent.Trim());
        Assert.AreEqual(1, component.Instance.InvalidCount);

        // Each refused text shows its own reason; the time stays empty meanwhile, so "required" stands beside it.
        component.Find(".bit-tpc-inp").Input("not a time");

        StringAssert.Contains(component.Find(".bit-tpc-erm").TextContent, "Not a time.");
        StringAssert.Contains(component.Find(".bit-tpc-erm").TextContent, RequiredMessage);

        component.Find(".bit-tpc-inp").Input("19:00");

        StringAssert.Contains(component.Find(".bit-tpc-erm").TextContent, "Between 08:00 and 18:00 only.");
        Assert.IsFalse(component.Find(".bit-tpc-erm").TextContent.Contains("Not a time."));

        component.Find(".bit-tpc-inp").Input("10:20");

        StringAssert.Contains(component.Find(".bit-tpc-erm").TextContent, "Quarter hours only.");

        component.Find(".bit-tpc-inp").Input("10:15");

        Assert.AreEqual(0, component.FindAll(".bit-tpc-erm").Count);

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitTimePickerShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitTimePickerTestModel.Time));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, new() { [nameof(BitTimePicker.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too early.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too early.", component.Find(".bit-tpc-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Lunch break.");
            store.Add(field, "Fully booked.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Lunch break. Fully booked.", component.Find(".bit-tpc-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimePickerWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-tpc-inp").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-tpc-erm").Count);
        Assert.IsNull(component.Find(".bit-tpc-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimePickerErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitTimePicker.ShowValidationMessage)] = true,
            [nameof(BitTimePicker.ErrorMessage)] = "That slot was taken while you were choosing.",
        };

        var component = RenderPicker(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-tpc-erm").Count);
        Assert.AreEqual("That slot was taken while you were choosing.", component.Find(".bit-tpc-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitTimePicker.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RequiredMessage, component.Find(".bit-tpc-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimePickerErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitTimePicker.ShowValidationMessage)] = true,
            [nameof(BitTimePicker.ErrorMessageAriaLabel)] = "The booking service is unavailable.",
            [nameof(BitTimePicker.ErrorMessageTemplate)] = (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Unavailable");
                builder.CloseElement();
            }),
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the input is described by.
        var labelId = component.Find(".bit-tpc-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Unavailable", component.Find(".bit-tpc-erm b").TextContent);
        Assert.IsFalse(component.Find(".bit-tpc-erm").TextContent.Contains(RequiredMessage));
        Assert.AreEqual("The booking service is unavailable.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitTimePickerShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new() { [nameof(BitTimePicker.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-tpc-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(LiveRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitTimePickerShouldDescribeTheFieldWithTheMessageFirst()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitTimePicker.ShowValidationMessage)] = true,
            [nameof(BitTimePicker.Description)] = "When the meeting starts.",
            [nameof(BitTimePicker.AriaDescription)] = "Times are in your local time zone.",
            [nameof(BitTimePicker.Suffix)] = "UTC",
        });

        component.InvokeAsync(() => editContext.Validate());

        // What is wrong with the value first, then the helper texts, and last the text beside the field.
        var ids = component.Find(".bit-tpc-inp").GetAttribute("aria-describedby")!.Split(' ');

        Assert.AreEqual(4, ids.Length);
        Assert.AreEqual(component.Find(".bit-tpc-erm").Id, ids[0]);
        Assert.AreEqual(component.Find(".bit-tpc-des").Id, ids[1]);
        Assert.AreEqual("Times are in your local time zone.", component.Find($"#{ids[2]}").TextContent);
        Assert.AreEqual(component.Find(".bit-tpc-suf").Id, ids[3]);
    }

    [TestMethod]
    public void BitTimePickerStandaloneShouldDescribeTheHourInputWithTheMessage()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitTimePicker.ShowValidationMessage)] = true,
            [nameof(BitTimePicker.Standalone)] = true,
        });

        component.InvokeAsync(() => editContext.Validate());

        // There is no field there, so the first input of the picker is what is described.
        var error = component.Find(".bit-tpc-erm");
        var described = component.FindAll(".bit-tpc-tin[aria-describedby]");

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(1, described.Count);
        Assert.AreEqual(error.Id, described[0].GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimePickerForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitTimePickerTestModel.Time));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitTimePicker.ShowValidationMessage)] = true,
            [nameof(BitTimePicker.Invalid)] = true,
            [nameof(BitTimePicker.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(".bit-tpc-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too early.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the field; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-tpc-erm").Id, component.Find(".bit-tpc-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(LiveRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-tpc-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimePickerThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitTimePicker.ShowValidationMessage)] = true,
            [nameof(BitTimePicker.NoValidate)] = false,
        };

        var component = RenderPicker(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-tpc-erm").Count);

        attributes[nameof(BitTimePicker.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(0, component.FindAll(".bit-tpc-erm").Count);
        Assert.IsNull(component.Find(".bit-tpc-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTimePickerBoundToNothingShouldShowNoMessage()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new() { [nameof(BitTimePicker.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the picker has no member of it the message could belong to.
        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.AreEqual(0, component.FindAll(".bit-tpc-erm").Count);
    }

    [TestMethod]
    public void BitTimePickerShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitTimePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every picker in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitTimePickerParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(picker => AddPicker(picker, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-tpc-erm").TextContent.Trim());
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderPicker(EditContext editContext, BitTimePickerTestModel model,
                                                                         Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddPicker(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitTimePickerTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddPicker(builder, model, attributes, bound: true)));
        });
    }

    private void AddPicker(RenderTreeBuilder builder, BitTimePickerTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitTimePicker>(0);

        if (bound)
        {
            builder.AddAttribute(1, nameof(BitTimePicker.Value), model.Time);
            builder.AddAttribute(2, nameof(BitTimePicker.ValueChanged), EventCallback.Factory.Create<TimeSpan?>(this, v => model.Time = v));
            builder.AddAttribute(3, nameof(BitTimePicker.ValueExpression), (Expression<Func<TimeSpan?>>)(() => model.Time));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(4, name, value);
        }

        builder.CloseComponent();
    }
}
