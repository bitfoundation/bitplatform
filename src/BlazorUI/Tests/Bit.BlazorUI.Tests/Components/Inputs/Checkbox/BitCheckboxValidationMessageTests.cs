using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Checkbox;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field, shown in the error line
/// under the checkbox and referenced by its input, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitCheckboxValidationMessageTests : BunitTestContext
{
    // The message the data annotation of the test model gives a checked box, read off the annotation itself rather
    // than written out, so the tests do not pin how a range of booleans is put into words.
    private static readonly string RangeMessage = GetRangeMessage();

    [TestMethod]
    public void BitCheckboxShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheCheckbox()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderCheckbox(editContext, model, new() { [nameof(BitCheckbox.ShowValidationMessage)] = true });

        // Nothing is shown before the EditContext has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-chb-erm"));
        Assert.IsNull(component.Find(".bit-chb-inp").GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-chb-erm");
        var input = component.Find(".bit-chb-inp");

        Assert.AreEqual(RangeMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-chb").ClassList.Contains("bit-inv"));

        // A state the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-chb-inp").Click();

        Assert.IsFalse(model.Value);
        Assert.IsEmpty(component.FindAll(".bit-chb-erm"));
        Assert.IsNull(component.Find(".bit-chb-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-chb-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitCheckboxShowValidationMessageShouldFollowTheMessagesWhileTheCheckboxStaysInvalid()
    {
        var model = new BitCheckboxTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitCheckboxTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderCheckbox(editContext, model, new() { [nameof(BitCheckbox.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Accept the terms.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Accept the terms.", component.Find(".bit-chb-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Accept the terms.");
            store.Add(field, "They changed since your last visit.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Accept the terms. They changed since your last visit.", component.Find(".bit-chb-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitCheckboxWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderCheckbox(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The checkbox is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-chb-inp").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-chb-erm"));
        Assert.IsNull(component.Find(".bit-chb-inp").GetAttribute("aria-describedby"));
        Assert.IsFalse(component.Find(".bit-chb").ClassList.Contains("bit-chb-her"));
    }

    [TestMethod]
    public void BitCheckboxErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitCheckbox.ShowValidationMessage)] = true,
            [nameof(BitCheckbox.ErrorMessage)] = "Paper invoices are not available on the free plan.",
        };

        var component = RenderCheckbox(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-chb-erm"));
        Assert.AreEqual("Paper invoices are not available on the free plan.", component.Find(".bit-chb-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitCheckbox.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RangeMessage, component.Find(".bit-chb-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitCheckboxShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderCheckbox(editContext, model, new() { [nameof(BitCheckbox.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the checkbox that takes the focus says what is wrong with it
        // instead. The messages do not even wire a live region of their own.
        Assert.AreEqual(RangeMessage, component.Find(".bit-chb-erm").TextContent.Trim());
        Assert.IsEmpty(component.FindAll("[role=status]"));
    }

    [TestMethod]
    public void BitCheckboxShouldNotAnnounceTheMessagesOfTheEditContextThroughALiveRegionItIsWiredFor()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // An ErrorMessage written as a null wires the live region before there is anything to say through it.
        var component = RenderCheckbox(editContext, model, new()
        {
            [nameof(BitCheckbox.ShowValidationMessage)] = true,
            [nameof(BitCheckbox.ErrorMessage)] = null,
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RangeMessage, component.Find(".bit-chb-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitCheckboxShouldDescribeTheInputWithTheMessageBetweenThePagesOwnReferenceAndTheDescription()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderCheckbox(editContext, model, new()
        {
            [nameof(BitCheckbox.ShowValidationMessage)] = true,
            [nameof(BitCheckbox.Description)] = "You can change it later in the settings.",
            [nameof(BitCheckbox.AriaDescribedby)] = "terms-hint",
            [nameof(BitCheckbox.InputHtmlAttributes)] = new Dictionary<string, object> { ["aria-describedby"] = "page-hint" },
        });

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-chb-erm");
        var description = component.Find(".bit-chb-des");

        Assert.AreEqual($"page-hint {error.Id} {description.Id} terms-hint", component.Find(".bit-chb-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitCheckboxForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitCheckboxTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitCheckboxTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderCheckbox(editContext, model, new()
        {
            [nameof(BitCheckbox.ShowValidationMessage)] = true,
            [nameof(BitCheckbox.Invalid)] = true,
            [nameof(BitCheckbox.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(".bit-chb-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Accept the terms.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the checkbox; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-chb-erm").Id, component.Find(".bit-chb-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find("[role=status]").TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-chb-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitCheckboxRootClassShouldFollowTheLineTheMessagesTake()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitCheckbox.Label)] = "Send paper invoices",
            [nameof(BitCheckbox.ShowValidationMessage)] = false,
        };

        var component = RenderCheckbox(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        // An invalid checkbox with no line to show keeps its label row as it is.
        Assert.IsFalse(component.Find(".bit-chb").ClassList.Contains("bit-chb-her"));

        // The line arrives with the parameter, and the class that lets the label row go of the full width with it.
        attributes[nameof(BitCheckbox.ShowValidationMessage)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.ContainsSingle(component.FindAll(".bit-chb-erm"));
        Assert.IsTrue(component.Find(".bit-chb").ClassList.Contains("bit-chb-her"));

        // And both go once the state is accepted.
        component.Find(".bit-chb-inp").Click();

        Assert.IsEmpty(component.FindAll(".bit-chb-erm"));
        Assert.IsFalse(component.Find(".bit-chb").ClassList.Contains("bit-chb-her"));
    }

    [TestMethod]
    public void BitCheckboxThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitCheckbox.ShowValidationMessage)] = true,
            [nameof(BitCheckbox.NoValidate)] = false,
        };

        var component = RenderCheckbox(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-chb-erm"));

        attributes[nameof(BitCheckbox.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.IsEmpty(component.FindAll(".bit-chb-erm"));
        Assert.IsNull(component.Find(".bit-chb-inp").GetAttribute("aria-describedby"));
        Assert.IsFalse(component.Find(".bit-chb").ClassList.Contains("bit-chb-her"));
    }

    [TestMethod]
    public void BitCheckboxBoundToNothingShouldShowNoMessage()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderCheckbox(editContext, model, new() { [nameof(BitCheckbox.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the checkbox has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-chb-erm"));
    }

    [TestMethod]
    public void BitCheckboxShowValidationMessageShouldReplaceTheValidationMessageComponentOfAnEditForm()
    {
        var component = RenderComponent<BitCheckboxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitCheckboxTestModel { Value = true });
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        Assert.AreEqual(RangeMessage, component.Find(".bit-chb-erm").TextContent.Trim());
        Assert.AreEqual(1, component.Instance.InvalidCount);

        component.Find(".bit-chb-inp").Click();

        Assert.IsEmpty(component.FindAll(".bit-chb-erm"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitCheckboxShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every checkbox in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitCheckboxParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(checkbox => AddCheckbox(checkbox, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RangeMessage, component.Find(".bit-chb-erm").TextContent.Trim());
    }

    private static string GetRangeMessage()
    {
        var model = new BitCheckboxTestModel { Value = true };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results.Single().ErrorMessage!;
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderCheckbox(EditContext editContext, BitCheckboxTestModel model,
                                                                           Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddCheckbox(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitCheckboxTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddCheckbox(builder, model, attributes, bound: true)));
        });
    }

    private void AddCheckbox(RenderTreeBuilder builder, BitCheckboxTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitCheckbox>(0);

        if (bound)
        {
            builder.AddAttribute(1, nameof(BitCheckbox.Value), model.Value);
            builder.AddAttribute(2, nameof(BitCheckbox.ValueChanged), EventCallback.Factory.Create<bool>(this, v => model.Value = v));
            builder.AddAttribute(3, nameof(BitCheckbox.ValueExpression), (Expression<Func<bool>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(4, name, value);
        }

        builder.CloseComponent();
    }
}
