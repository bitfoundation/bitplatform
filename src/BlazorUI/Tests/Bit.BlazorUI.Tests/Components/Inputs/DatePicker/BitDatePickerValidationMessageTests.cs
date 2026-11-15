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

namespace Bit.BlazorUI.Tests.Components.Inputs.DatePicker;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field - the one of a typed text
/// the DatePicker refuses among them - shown in the error line under the DatePicker and referenced by its input,
/// giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitDatePickerValidationMessageTests : BunitTestContext
{
    private const string RequiredMessage = "The Value field is required.";
    private const string LiveRegion = ".bit-dtp > [role=status]";

    [TestMethod]
    public void BitDatePickerShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheField()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new() { [nameof(BitDatePicker.ShowValidationMessage)] = true });

        // Nothing is shown before the EditContext has anything to say.
        Assert.AreEqual(0, component.FindAll(".bit-dtp-erm").Count);
        Assert.IsNull(component.Find(".bit-dtp-inp").GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-dtp-erm");
        var input = component.Find(".bit-dtp-inp");

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-dtp").ClassList.Contains("bit-inv"));

        // A date the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-dtp-wrp").Click();
        component.Find(".bit-dtp-dtd").Click();

        Assert.IsNotNull(model.Value);
        Assert.AreEqual(0, component.FindAll(".bit-dtp-erm").Count);
        Assert.IsNull(component.Find(".bit-dtp-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-dtp-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitDatePickerShowValidationMessageShouldShowTheMessageOfATypedTextThatIsNotADate()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new()
        {
            [nameof(BitDatePicker.ShowValidationMessage)] = true,
            [nameof(BitDatePicker.AllowTextInput)] = true,
            [nameof(BitDatePicker.Culture)] = CultureInfo.InvariantCulture,
            [nameof(BitDatePicker.DateFormat)] = "dd/MM/yyyy",
            [nameof(BitDatePicker.InvalidErrorMessage)] = "Type the date as dd/MM/yyyy.",
        });

        component.Find(".bit-dtp-inp").Input("next friday");

        // The text leaves the value empty, so the field is refused for both.
        var error = component.Find(".bit-dtp-erm");

        StringAssert.Contains(error.TextContent, "Type the date as dd/MM/yyyy.");
        StringAssert.Contains(error.TextContent, RequiredMessage);

        // What is wrong with the text is read before the pattern it should have been typed in.
        var ids = component.Find(".bit-dtp-inp").GetAttribute("aria-describedby")!.Split(' ');

        Assert.AreEqual(2, ids.Length);
        Assert.AreEqual(error.Id, ids[0]);
        StringAssert.Contains(component.Find($"#{ids[1]}").TextContent, "dd/MM/yyyy");

        // A date takes the parsing error away with the text that caused it.
        component.Find(".bit-dtp-inp").Input("20/01/2026");

        Assert.AreEqual(0, component.FindAll(".bit-dtp-erm").Count);
        Assert.AreEqual(new DateTime(2026, 1, 20), model.Value!.Value.Date);
    }

    [TestMethod]
    public void BitDatePickerShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitDatePickerTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderDatePicker(editContext, model, new() { [nameof(BitDatePicker.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too early.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too early.", component.Find(".bit-dtp-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Not a weekday.");
            store.Add(field, "Fully booked.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Not a weekday. Fully booked.", component.Find(".bit-dtp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-dtp-inp").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-dtp-erm").Count);
        Assert.IsNull(component.Find(".bit-dtp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDatePickerErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitDatePicker.ShowValidationMessage)] = true,
            [nameof(BitDatePicker.ErrorMessage)] = "The clinic is closed that week.",
        };

        var component = RenderDatePicker(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-dtp-erm").Count);
        Assert.AreEqual("The clinic is closed that week.", component.Find(".bit-dtp-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitDatePicker.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtp-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerErrorMessageTemplateShouldKeepItsLabelOverTheMessagesOfTheEditContext()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new()
        {
            [nameof(BitDatePicker.ShowValidationMessage)] = true,
            [nameof(BitDatePicker.ErrorMessageAriaLabel)] = "The booking service is unavailable.",
            [nameof(BitDatePicker.ErrorMessageTemplate)] = (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddContent(1, "Unavailable");
                builder.CloseElement();
            }),
        });

        component.InvokeAsync(() => editContext.Validate());

        // The template is what is on screen, so its label is what the input is described by.
        var labelId = component.Find(".bit-dtp-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Unavailable", component.Find(".bit-dtp-erm b").TextContent);
        Assert.IsFalse(component.Find(".bit-dtp-erm").TextContent.Contains(RequiredMessage));
        Assert.AreEqual("The booking service is unavailable.", component.Find($"#{labelId}").TextContent);
    }

    [TestMethod]
    public void BitDatePickerShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new() { [nameof(BitDatePicker.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtp-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(LiveRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerShouldDescribeTheFieldWithTheMessageFirst()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new()
        {
            [nameof(BitDatePicker.ShowValidationMessage)] = true,
            [nameof(BitDatePicker.Description)] = "A weekday of this month.",
            [nameof(BitDatePicker.AriaDescription)] = "Dates are stored in UTC.",
            [nameof(BitDatePicker.InputHtmlAttributes)] = new Dictionary<string, object> { ["aria-describedby"] = "page-hint" },
        });

        component.InvokeAsync(() => editContext.Validate());

        // What is wrong with the value first, then the helper texts, and last the description the page wrote itself.
        var ids = component.Find(".bit-dtp-inp").GetAttribute("aria-describedby")!.Split(' ');

        Assert.AreEqual(4, ids.Length);
        Assert.AreEqual(component.Find(".bit-dtp-erm").Id, ids[0]);
        Assert.AreEqual(component.Find(".bit-dtp-des").Id, ids[1]);
        Assert.AreEqual("Dates are stored in UTC.", component.Find($"#{ids[2]}").TextContent);
        Assert.AreEqual("page-hint", ids[3]);
    }

    [TestMethod]
    public void BitDatePickerStandaloneShouldDescribeTheCalendarWithTheMessage()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new()
        {
            [nameof(BitDatePicker.ShowValidationMessage)] = true,
            [nameof(BitDatePicker.Standalone)] = true,
        });

        component.InvokeAsync(() => editContext.Validate());

        // There is no input to focus there, so the calendar itself is what is described.
        var error = component.Find(".bit-dtp-erm");

        Assert.AreEqual(RequiredMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-dtp-cac[role=group]").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDatePickerForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitDatePickerTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderDatePicker(editContext, model, new()
        {
            [nameof(BitDatePicker.ShowValidationMessage)] = true,
            [nameof(BitDatePicker.Invalid)] = true,
            [nameof(BitDatePicker.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(".bit-dtp-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too early.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the field; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-dtp-erm").Id, component.Find(".bit-dtp-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(LiveRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-dtp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDatePickerThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitDatePicker.ShowValidationMessage)] = true,
            [nameof(BitDatePicker.NoValidate)] = false,
        };

        var component = RenderDatePicker(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(1, component.FindAll(".bit-dtp-erm").Count);

        attributes[nameof(BitDatePicker.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(0, component.FindAll(".bit-dtp-erm").Count);
        Assert.IsNull(component.Find(".bit-dtp-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitDatePickerBoundToNothingShouldShowNoMessage()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderDatePicker(editContext, model, new() { [nameof(BitDatePicker.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the DatePicker has no member of it the message could belong to.
        Assert.IsTrue(editContext.GetValidationMessages().Any());
        Assert.AreEqual(0, component.FindAll(".bit-dtp-erm").Count);
    }

    [TestMethod]
    public void BitDatePickerShowValidationMessageShouldReplaceTheValidationMessageComponentOfAnEditForm()
    {
        var component = RenderComponent<BitDatePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitDatePickerTestModel());
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtp-erm").TextContent.Trim());
        Assert.AreEqual(1, component.Instance.InvalidCount);

        component.Find(".bit-dtp-wrp").Click();
        component.Find(".bit-dtp-dtd").Click();

        Assert.AreEqual(0, component.FindAll(".bit-dtp-erm").Count);

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitDatePickerShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitDatePickerTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every DatePicker in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitDatePickerParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(datePicker => AddDatePicker(datePicker, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RequiredMessage, component.Find(".bit-dtp-erm").TextContent.Trim());
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderDatePicker(EditContext editContext, BitDatePickerTestModel model,
                                                                             Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddDatePicker(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitDatePickerTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddDatePicker(builder, model, attributes, bound: true)));
        });
    }

    private void AddDatePicker(RenderTreeBuilder builder, BitDatePickerTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitDatePicker>(0);

        if (bound)
        {
            builder.AddAttribute(1, nameof(BitDatePicker.Value), model.Value);
            builder.AddAttribute(2, nameof(BitDatePicker.ValueChanged), EventCallback.Factory.Create<DateTimeOffset?>(this, v => model.Value = v));
            builder.AddAttribute(3, nameof(BitDatePicker.ValueExpression), (Expression<Func<DateTimeOffset?>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(4, name, value);
        }

        builder.CloseComponent();
    }
}
