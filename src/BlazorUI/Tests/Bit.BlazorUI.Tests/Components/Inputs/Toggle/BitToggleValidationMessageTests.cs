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

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field, shown in the error line
/// under the toggle and referenced by its switch, giving way to the app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitToggleValidationMessageTests : BunitTestContext
{
    // The message the data annotation of the test model gives a toggle that is on, read off the annotation itself
    // rather than written out, so the tests do not pin how a range of booleans is put into words.
    private static readonly string RangeMessage = GetRangeMessage();

    [TestMethod]
    public void BitToggleShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheToggle()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderToggle(editContext, model, new() { [nameof(BitToggle.ShowValidationMessage)] = true });

        // Nothing is shown before the EditContext has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-tgl-erm"));
        Assert.IsNull(component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));

        // A validation of the whole form re-renders the toggle without a parameter or its value changing, which is
        // all the switch has to read the reference to the message from.
        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-tgl-erm");
        var button = component.Find(".bit-tgl-btn");

        Assert.AreEqual(RangeMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, button.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", button.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-tgl").ClassList.Contains("bit-inv"));

        // A state the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-tgl-btn").Click();

        Assert.IsFalse(model.Value);
        Assert.IsEmpty(component.FindAll(".bit-tgl-erm"));
        Assert.IsNull(component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-tgl-btn").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitToggleShowValidationMessageShouldFollowTheMessagesWhileTheToggleStaysInvalid()
    {
        var model = new BitToggleTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitToggleTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderToggle(editContext, model, new() { [nameof(BitToggle.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Accept the terms.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Accept the terms.", component.Find(".bit-tgl-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Accept the terms.");
            store.Add(field, "They changed since your last visit.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Accept the terms. They changed since your last visit.", component.Find(".bit-tgl-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitToggleWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderToggle(editContext, model, new());

        component.InvokeAsync(() => editContext.Validate());

        // The toggle is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-tgl-btn").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-tgl-erm"));
        Assert.IsNull(component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));
        Assert.IsFalse(component.Find(".bit-tgl").ClassList.Contains("bit-tgl-her"));
    }

    [TestMethod]
    public void BitToggleErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitToggle.ShowValidationMessage)] = true,
            [nameof(BitToggle.ErrorMessage)] = "Offline mode is not available on the free plan.",
        };

        var component = RenderToggle(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-tgl-erm"));
        Assert.AreEqual("Offline mode is not available on the free plan.", component.Find(".bit-tgl-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitToggle.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RangeMessage, component.Find(".bit-tgl-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitToggleShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderToggle(editContext, model, new() { [nameof(BitToggle.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the switch that takes the focus says what is wrong with it
        // instead. The messages do not even wire a live region of their own.
        Assert.AreEqual(RangeMessage, component.Find(".bit-tgl-erm").TextContent.Trim());
        Assert.IsEmpty(component.FindAll("[role=status]"));
    }

    [TestMethod]
    public void BitToggleShouldNotAnnounceTheMessagesOfTheEditContextThroughALiveRegionItIsWiredFor()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // An ErrorMessage written as a null wires the live region before there is anything to say through it.
        var component = RenderToggle(editContext, model, new()
        {
            [nameof(BitToggle.ShowValidationMessage)] = true,
            [nameof(BitToggle.ErrorMessage)] = null,
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RangeMessage, component.Find(".bit-tgl-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitToggleShouldDescribeTheSwitchWithTheMessageBetweenTheStateTextAndTheDescription()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderToggle(editContext, model, new()
        {
            [nameof(BitToggle.ShowValidationMessage)] = true,
            [nameof(BitToggle.Label)] = "Terms and conditions",
            [nameof(BitToggle.Text)] = "I agree.",
            [nameof(BitToggle.Description)] = "You can change it later in the settings.",
            [nameof(BitToggle.AriaDescribedby)] = "terms-hint",
        });

        component.InvokeAsync(() => editContext.Validate());

        var stateText = component.Find(".bit-tgl-stx");
        var error = component.Find(".bit-tgl-erm");
        var description = component.Find(".bit-tgl-des");

        Assert.AreEqual($"{stateText.Id} {error.Id} {description.Id} terms-hint", component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitToggleForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitToggleTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitToggleTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderToggle(editContext, model, new()
        {
            [nameof(BitToggle.ShowValidationMessage)] = true,
            [nameof(BitToggle.Invalid)] = true,
            [nameof(BitToggle.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(".bit-tgl-btn").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Accept the terms.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the switch; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-tgl-erm").Id, component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find("[role=status]").TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitToggleRootClassShouldFollowTheLineTheMessagesTake()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitToggle.Label)] = "Offline mode",
            [nameof(BitToggle.Inline)] = true,
            [nameof(BitToggle.ShowValidationMessage)] = false,
        };

        var component = RenderToggle(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        // An invalid toggle with no line to show keeps its row as it is.
        Assert.IsFalse(component.Find(".bit-tgl").ClassList.Contains("bit-tgl-her"));

        // The line arrives with the parameter, and the class that wraps it onto a line of its own with it.
        attributes[nameof(BitToggle.ShowValidationMessage)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.ContainsSingle(component.FindAll(".bit-tgl-erm"));
        Assert.IsTrue(component.Find(".bit-tgl").ClassList.Contains("bit-tgl-her"));

        // And both go once the state is accepted.
        component.Find(".bit-tgl-btn").Click();

        Assert.IsEmpty(component.FindAll(".bit-tgl-erm"));
        Assert.IsFalse(component.Find(".bit-tgl").ClassList.Contains("bit-tgl-her"));
    }

    [TestMethod]
    public void BitToggleThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitToggle.ShowValidationMessage)] = true,
            [nameof(BitToggle.NoValidate)] = false,
        };

        var component = RenderToggle(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-tgl-erm"));

        attributes[nameof(BitToggle.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.IsEmpty(component.FindAll(".bit-tgl-erm"));
        Assert.IsNull(component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));
        Assert.IsFalse(component.Find(".bit-tgl").ClassList.Contains("bit-tgl-her"));
    }

    [TestMethod]
    public void BitToggleBoundToNothingShouldShowNoMessage()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderToggle(editContext, model, new() { [nameof(BitToggle.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the toggle has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-tgl-erm"));
    }

    [TestMethod]
    public void BitToggleShowValidationMessageShouldReplaceTheValidationMessageComponentOfAnEditForm()
    {
        var component = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = true });
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        Assert.AreEqual(RangeMessage, component.Find(".bit-tgl-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-tgl-erm").Id, component.Find(".bit-tgl-btn").GetAttribute("aria-describedby"));
        Assert.AreEqual(1, component.Instance.InvalidCount);

        component.Find(".bit-tgl-btn").Click();

        Assert.IsEmpty(component.FindAll(".bit-tgl-erm"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitToggleShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitToggleTestModel { Value = true };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every toggle in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitToggleParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(toggle => AddToggle(toggle, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RangeMessage, component.Find(".bit-tgl-erm").TextContent.Trim());
    }

    private static string GetRangeMessage()
    {
        var model = new BitToggleTestModel { Value = true };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results.Single().ErrorMessage!;
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderToggle(EditContext editContext, BitToggleTestModel model,
                                                                         Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddToggle(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitToggleTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddToggle(builder, model, attributes, bound: true)));
        });
    }

    private void AddToggle(RenderTreeBuilder builder, BitToggleTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitToggle>(0);

        if (bound)
        {
            builder.AddAttribute(1, nameof(BitToggle.Value), model.Value);
            builder.AddAttribute(2, nameof(BitToggle.ValueChanged), EventCallback.Factory.Create<bool>(this, v => model.Value = v));
            builder.AddAttribute(3, nameof(BitToggle.ValueExpression), (Expression<Func<bool>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(4, name, value);
        }

        builder.CloseComponent();
    }
}
