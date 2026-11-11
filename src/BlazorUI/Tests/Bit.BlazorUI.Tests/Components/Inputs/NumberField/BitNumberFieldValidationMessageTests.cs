using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.NumberField;

/// <summary>
/// Covers ShowValidationMessage: the messages the cascading EditContext holds for the field - a text that is not a
/// number among them - shown in the error line under the number field and referenced by its input, giving way to the
/// app's own ErrorMessage, and never announced.
/// </summary>
[TestClass]
public class BitNumberFieldValidationMessageTests : BunitTestContext
{
    private const string RangeMessage = "The field Value must be between 6 and 18.";
    private const string LiveRegion = ".bit-nfl > [role=status]";

    [TestMethod]
    public void BitNumberFieldShowValidationMessageShouldShowTheMessageOfTheEditContextUnderTheField()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new() { [nameof(BitNumberField<int?>.ShowValidationMessage)] = true });

        // Nothing is shown before the EditContext has anything to say.
        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
        Assert.IsNull(component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-nfl-erm");
        var input = component.Find(".bit-nfl-inp");

        Assert.AreEqual(RangeMessage, error.TextContent.Trim());
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-inv"));

        // A value the EditContext accepts takes the line, and the reference to it, away again.
        component.Find(".bit-nfl-inp").Change("10");

        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
        Assert.IsNull(component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-nfl-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitNumberFieldShowValidationMessageShouldShowTheParsingErrorMessageOfATextThatIsNotANumber()
    {
        var model = new BitNumberFieldTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = true,
            [nameof(BitNumberField<int?>.DisplayName)] = "Weight",
            [nameof(BitNumberField<int?>.ParsingErrorMessage)] = "{0} must be a number.",
        });

        component.Find(".bit-nfl-inp").Change("abc");

        var error = component.Find(".bit-nfl-erm");

        Assert.AreEqual("Weight must be a number.", error.TextContent.Trim());
        Assert.AreEqual(error.Id, component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));

        // A number takes the parsing error away with the text that caused it.
        component.Find(".bit-nfl-inp").Change("10");

        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
        Assert.AreEqual(10, model.Value);
    }

    [TestMethod]
    public void BitNumberFieldShowValidationMessageShouldFollowTheMessagesWhileTheFieldStaysInvalid()
    {
        var model = new BitNumberFieldTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitNumberFieldTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderField(editContext, model, new() { [nameof(BitNumberField<int?>.ShowValidationMessage)] = true });

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too young.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too young.", component.Find(".bit-nfl-erm").TextContent.Trim());

        // Every message the field holds is shown, in the order they were added, as the ValidationMessage component
        // lists them.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Not a whole number.");
            store.Add(field, "Too old.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Not a whole number. Too old.", component.Find(".bit-nfl-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitNumberFieldWithoutShowValidationMessageShouldLeaveTheMessagesToTheValidationMessageComponent()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new() { [nameof(BitNumberField<int?>.LabelPlacement)] = BitPlacement.Start });

        component.InvokeAsync(() => editContext.Validate());

        // The field is marked as before, and the message stays where the page puts it.
        Assert.AreEqual("true", component.Find(".bit-nfl-inp").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
        Assert.IsNull(component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));
        Assert.IsFalse(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));
    }

    [TestMethod]
    public void BitNumberFieldErrorMessageShouldWinTheLineOverTheMessagesOfTheEditContext()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = true,
            [nameof(BitNumberField<int?>.ErrorMessage)] = "Only 4 seats are left for this show.",
        };

        var component = RenderField(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-nfl-erm"));
        Assert.AreEqual("Only 4 seats are left for this show.", component.Find(".bit-nfl-erm").TextContent.Trim());

        // Once the app takes its message back, the EditContext's takes the line.
        attributes[nameof(BitNumberField<int?>.ErrorMessage)] = null;
        Rerender(component, editContext, model, attributes);

        Assert.AreEqual(RangeMessage, component.Find(".bit-nfl-erm").TextContent.Trim());
    }

    [TestMethod]
    public void BitNumberFieldShouldNotAnnounceTheMessagesOfTheEditContext()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new() { [nameof(BitNumberField<int?>.ShowValidationMessage)] = true });

        component.InvokeAsync(() => editContext.Validate());

        // A submit validates every field at once; the field that takes the focus says what is wrong with it instead.
        Assert.AreEqual(RangeMessage, component.Find(".bit-nfl-erm").TextContent.Trim());
        Assert.AreEqual(string.Empty, component.Find(LiveRegion).TextContent.Trim());
    }

    [TestMethod]
    public void BitNumberFieldShouldDescribeTheFieldWithTheMessageBetweenThePagesOwnReferenceAndTheDescription()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = true,
            [nameof(BitNumberField<int?>.Description)] = "Between 6 and 18.",
            [nameof(BitNumberField<int?>.InputHtmlAttributes)] = new Dictionary<string, object> { ["aria-describedby"] = "page-hint" },
        });

        component.InvokeAsync(() => editContext.Validate());

        var error = component.Find(".bit-nfl-erm");
        var description = component.Find(".bit-nfl-des");

        Assert.AreEqual($"page-hint {error.Id} {description.Id}", component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitNumberFieldWithHideInputShouldDescribeTheGroupWithTheMessage()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = true,
            [nameof(BitNumberField<int?>.HideInput)] = true,
        });

        component.InvokeAsync(() => editContext.Validate());

        // The input is hidden from assistive technologies there, so the group around the buttons is what is described.
        Assert.AreEqual(component.Find(".bit-nfl-erm").Id, component.Find(".bit-nfl-cnt[role=group]").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitNumberFieldForcedInvalidLabelShouldGiveWayToAMessageOnScreen()
    {
        var model = new BitNumberFieldTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitNumberFieldTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = true,
            [nameof(BitNumberField<int?>.Invalid)] = true,
            [nameof(BitNumberField<int?>.ErrorMessageAriaLabel)] = "Refused by the server.",
        });

        var labelId = component.Find(".bit-nfl-inp").GetAttribute("aria-describedby");

        Assert.AreEqual("Refused by the server.", component.Find($"#{labelId}").TextContent);

        component.InvokeAsync(() =>
        {
            store.Add(field, "Too young.");
            editContext.NotifyValidationStateChanged();
        });

        // The text on screen describes the field; the app's own rejection is still what the live region says.
        Assert.AreEqual(component.Find(".bit-nfl-erm").Id, component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));
        Assert.AreEqual("Refused by the server.", component.Find(LiveRegion).TextContent.Trim());

        component.InvokeAsync(() =>
        {
            store.Clear();
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual(labelId, component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitNumberFieldWithARowLabelShouldWrapTheMessageOntoALineOfItsOwn()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitNumberField<int?>.Label)] = "Age",
            [nameof(BitNumberField<int?>.LabelPlacement)] = BitPlacement.Start,
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = false,
        };

        var component = RenderField(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        // An invalid field with no line to show keeps its row layout as it is.
        Assert.IsFalse(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));

        // The line arrives with the parameter, and the class that wraps it under the label and the field with it.
        attributes[nameof(BitNumberField<int?>.ShowValidationMessage)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.ContainsSingle(component.FindAll(".bit-nfl-erm"));
        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));

        // And both go once the value is accepted.
        component.Find(".bit-nfl-inp").Change("10");

        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
        Assert.IsFalse(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));
    }

    [TestMethod]
    public void BitNumberFieldRootClassShouldFollowAMessageThatGetsTextWhileTheFieldStaysInvalid()
    {
        var model = new BitNumberFieldTestModel();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(BitNumberFieldTestModel.Value));
        var store = new ValidationMessageStore(editContext);

        var component = RenderField(editContext, model, new()
        {
            [nameof(BitNumberField<int?>.Label)] = "Age",
            [nameof(BitNumberField<int?>.LabelPlacement)] = BitPlacement.End,
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = true,
        });

        // A blank message marks the field, but has nothing to show.
        component.InvokeAsync(() =>
        {
            store.Add(field, " ");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("true", component.Find(".bit-nfl-inp").GetAttribute("aria-invalid"));
        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
        Assert.IsFalse(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));

        // The field was invalid and stays so; the message alone is what changes.
        component.InvokeAsync(() =>
        {
            store.Clear(field);
            store.Add(field, "Too young.");
            editContext.NotifyValidationStateChanged();
        });

        Assert.AreEqual("Too young.", component.Find(".bit-nfl-erm").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-nfl").ClassList.Contains("bit-nfl-hds"));
    }

    [TestMethod]
    public void BitNumberFieldThatStopsValidatingShouldTakeTheMessageAway()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var attributes = new Dictionary<string, object?>
        {
            [nameof(BitNumberField<int?>.ShowValidationMessage)] = true,
            [nameof(BitNumberField<int?>.NoValidate)] = false,
        };

        var component = RenderField(editContext, model, attributes);

        component.InvokeAsync(() => editContext.Validate());

        Assert.ContainsSingle(component.FindAll(".bit-nfl-erm"));

        attributes[nameof(BitNumberField<int?>.NoValidate)] = true;
        Rerender(component, editContext, model, attributes);

        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
        Assert.IsNull(component.Find(".bit-nfl-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitNumberFieldBoundToNothingShouldShowNoMessage()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderField(editContext, model, new() { [nameof(BitNumberField<int?>.ShowValidationMessage)] = true }, bound: false);

        component.InvokeAsync(() => editContext.Validate());

        // The model has a message, but the field has no member of it the message could belong to.
        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));
    }

    [TestMethod]
    public void BitNumberFieldShowValidationMessageShouldReplaceTheValidationMessageComponentOfAnEditForm()
    {
        var component = RenderComponent<BitNumberFieldValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitNumberFieldTestModel { Value = 4 });
            parameters.Add(p => p.ShowValidationMessage, true);
        });

        component.Find("form").Submit();

        Assert.AreEqual(RangeMessage, component.Find(".bit-nfl-erm").TextContent.Trim());
        Assert.AreEqual(1, component.Instance.InvalidCount);

        // A text that is not a number leaves the value out of range, so both are wrong with the field.
        component.Find(".bit-nfl-inp").Change("abc");

        var message = component.Find(".bit-nfl-erm").TextContent.Trim();

        Assert.Contains("The Value field is not valid.", message);
        Assert.Contains(RangeMessage, message);

        component.Find(".bit-nfl-inp").Change("10");

        Assert.IsEmpty(component.FindAll(".bit-nfl-erm"));

        component.Find("form").Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
    }

    [TestMethod]
    public void BitNumberFieldShouldShowTheMessagesWhenACascadeAsksForThem()
    {
        var model = new BitNumberFieldTestModel { Value = 4 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        // One BitParams around a form turns the messages on for every number field in it.
        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitNumberFieldParams { ShowValidationMessage = true } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(field => AddField(field, model, new(), bound: true)));
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        Assert.AreEqual(RangeMessage, component.Find(".bit-nfl-erm").TextContent.Trim());
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderField(EditContext editContext, BitNumberFieldTestModel model,
                                                                        Dictionary<string, object?> attributes, bool bound = true)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddField(builder, model, attributes, bound)));
        });
    }

    // A CascadingValue takes its value - and its content - anew on every parameters pass, so both are handed to it again.
    private void Rerender(IRenderedComponent<CascadingValue<EditContext>> component, EditContext editContext, BitNumberFieldTestModel model,
                          Dictionary<string, object?> attributes)
    {
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder => AddField(builder, model, attributes, bound: true)));
        });
    }

    private void AddField(RenderTreeBuilder builder, BitNumberFieldTestModel model, Dictionary<string, object?> attributes, bool bound)
    {
        builder.OpenComponent<BitNumberField<int?>>(0);

        if (bound)
        {
            builder.AddAttribute(1, nameof(BitNumberField<int?>.Value), model.Value);
            builder.AddAttribute(2, nameof(BitNumberField<int?>.ValueChanged), EventCallback.Factory.Create<int?>(this, v => model.Value = v));
            builder.AddAttribute(3, nameof(BitNumberField<int?>.ValueExpression), (Expression<Func<int?>>)(() => model.Value));
        }

        foreach (var (name, value) in attributes)
        {
            builder.AddAttribute(4, name, value);
        }

        builder.CloseComponent();
    }
}
