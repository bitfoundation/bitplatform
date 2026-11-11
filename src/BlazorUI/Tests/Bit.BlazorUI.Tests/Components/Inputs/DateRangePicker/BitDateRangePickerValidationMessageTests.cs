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

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field - the one of a typed text
/// the DateRangePicker refuses among them - shown in the error line under the DateRangePicker and referenced by its
/// input, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitDateRangePickerValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Value field is required.";
    private const string LiveRegion = ".bit-dtrp > [role=status]";

    [TestMethod]
    public void BitDateRangePickerShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheField()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new() { [nameof(BitDateRangePicker.ShowValidationMessage)] = true });

        // Nothing is shown before the EditContext has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-dtrp-erm"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-dtrp-erm");
        var input = component.Find(".bit-dtrp-inp");

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-inv"));

        // A range the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-dtrp-wrp").Click();
        component.Find(".bit-dtrp-dtd").Click();

        Assert.IsNotNull(model.Value);
        Assert.IsEmpty(component.FindAll(".bit-dtrp-erm"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitDateRangePickerShowValidationMessageShouldShowTheMessagesOfATypedRangeInAnEditForm()
    {
        var component = RenderComponent<BitDateRangePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.ShowValidationMessage, true);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.MinDate, new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.MaxDate, new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.InvalidErrorMessage, "Not a range.");
            parameters.Add(p => p.OutOfRangeErrorMessage, "Outside the bookable dates.");
        });

        component.Find("form").Submit();

        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtrp-erm").TextContent.Trim());
        Assert.AreEqual(1, component.Instance.InvalidCount);

        // Each refused text shows its own reason; the range stays empty meanwhile, so "required" stands beside it.
        component.Find(".bit-dtrp-inp").Change("nonsense");

        Assert.Contains("Not a range.", component.Find(".bit-dtrp-erm").TextContent);
        Assert.Contains(RequiredMessage, component.Find(".bit-dtrp-erm").TextContent);

        component.Find(".bit-dtrp-inp").Change("2026-01-01 - 2026-01-05");

        Assert.Contains("Outside the bookable dates.", component.Find(".bit-dtrp-erm").TextContent);
        Assert.DoesNotContain("Not a range.", component.Find(".bit-dtrp-erm").TextContent);

        component.Find(".bit-dtrp-inp").Change("2026-01-12 - 2026-01-15");

        Assert.IsEmpty(component.FindAll(".bit-dtrp-erm"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitDateRangePickerShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitDateRangePickerTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, new() { [nameof(BitDateRangePicker.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too short a stay.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too short a stay.", component.Find(".bit-dtrp-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Starts on a holiday.");
            store.Add(field, "Fully booked.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Starts on a holiday. Fully booked.", component.Find(".bit-dtrp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-dtrp-inp").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-dtrp-erm"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitDateRangePicker.ShowValidationMessage)] = true,
            [nameof(BitDateRangePicker.ErrorMessage)] = "Those nights are booked.",
        };

        var component = RenderPicker(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-dtrp-erm"));
        Assert.AreEqual("Those nights are booked.", component.Find(".bit-dtrp-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitDateRangePicker.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtrp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitDateRangePicker.ShowValidationMessage)] = true,
            [nameof(BitDateRangePicker.ErrorMessageAriaLabel)] = "The booking service is unavailable.",
            [nameof(BitDateRangePicker.ErrorMessageTemplate)] = (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Unavailable");
                builder.CloseElement();
            }),
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the input is described by.
        var labelId = component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Unavailable", component.Find(".bit-dtrp-erm b").TextContent);
        Assert.DoesNotContain(RequiredMessage, component.Find(".bit-dtrp-erm").TextContent);
        Assert.AreEqual("The booking service is unavailable.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitDateRangePickerShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // A picker wired for messages of the app's own, so its live region is there to say something.
        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitDateRangePicker.ShowValidationMessage)] = true,
            [nameof(BitDateRangePicker.ErrorMessage)] = null,
        });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtrp-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(LiveRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerShouldDescribeTheFieldWithTheMessageBeforeThePagesOwnReference()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitDateRangePicker.ShowValidationMessage)] = true,
            [nameof(BitDateRangePicker.InputHtmlAttributes)] = new Dictionary<string, object> { ["aria-describedby"] = "page-hint" },
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual($"{component.Find(".bit-dtrp-erm").Id} page-hint", component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldDescribeTheCalendarWithTheMessage()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitDateRangePicker.ShowValidationMessage)] = true,
            [nameof(BitDateRangePicker.Standalone)] = true,
        });

        component.InvokeAsync(() => editContext.Validate());

        // There is no input to focus there, so the calendar itself is what is described.
        var error = component.Find(".bit-dtrp-erm");

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-dtrp-cac[role=group]").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitDateRangePickerTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderPicker(editContext, model, new()
        {
            [nameof(BitDateRangePicker.ShowValidationMessage)] = true,
            [nameof(BitDateRangePicker.Invalid)] = true,
            [nameof(BitDateRangePicker.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too short a stay.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the field; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-dtrp-erm").Id, component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(LiveRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitDateRangePicker.ShowValidationMessage)] = true,
            [nameof(BitDateRangePicker.NoValidate)] = false,
        };

        var component = RenderPicker(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-dtrp-erm"));

        attributes[nameof(BitDateRangePicker.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.IsEmpty(component.FindAll(".bit-dtrp-erm"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDateRangePickerBoundToNothingShouldShowNoMessage()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderPicker(editContext, model, new() { [nameof(BitDateRangePicker.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the DateRangePicker has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-dtrp-erm"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every DateRangePicker in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitDateRangePickerParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(picker => AddPicker(picker, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtrp-erm").TextContent.Trim());
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderPicker(EditContext editContext, BitDateRangePickerTestModel model,
                                                                         Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddPicker(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitDateRangePickerTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddPicker(builder, model, attributes, bound: true)));
        });
    }

    private void AddPicker(RenderTreeBuilder builder, BitDateRangePickerTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitDateRangePicker>(0);

        if (bound)
        {
            builder.AddAttribute(1, nameof(BitDateRangePicker.Value), model.Value);
            builder.AddAttribute(2, nameof(BitDateRangePicker.ValueChanged), EventCallback.Factory.Create<BitDateRangePickerValue?>(this, v => model.Value = v));
            builder.AddAttribute(3, nameof(BitDateRangePicker.ValueExpression), (Expression<Func<BitDateRangePickerValue?>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(4, name, value);
        }

        builder.CloseComponent();
    }
}
